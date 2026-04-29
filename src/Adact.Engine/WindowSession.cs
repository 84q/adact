using System.Diagnostics;

using Adact.Engine.Elements;
using Adact.Engine.Exceptions;
using Adact.Engine.Snapshot;

using FlaUI.Core;
using FlaUI.Core.AutomationElements;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Adact.Engine;

/// <summary>
/// 1 ウィンドウへの操作セッション。Snapshot / Click / Fill を提供する。
/// Session ID は <see cref="UiaEngine"/> が採番し、Detach 後も再利用しない。
/// </summary>
public sealed class WindowSession : IDisposable
{
    private readonly AutomationBase _automation;
    private readonly Window _window;
    private readonly IElement _rootElement;
    private readonly RefRegistry _registry;
    private readonly SemaphoreSlim _gate;
    private readonly ILogger<WindowSession> _logger;
    private readonly bool _ownsAutomation;
    private readonly int _processId;
    private readonly string _processName;
    private readonly string _title;
    private readonly nint _nativeWindowHandle;
    private bool _disposed;

    internal WindowSession(
        AutomationBase automation,
        Window window,
        int sessionId,
        WindowInfo info,
        SemaphoreSlim gate,
        ILogger<WindowSession>? logger = null,
        bool ownsAutomation = false)
    {
        _automation = automation;
        _window = window;
        _rootElement = new FlaUiElement(window);
        _registry = new RefRegistry(sessionId);
        _gate = gate;
        _logger = logger ?? NullLogger<WindowSession>.Instance;
        _ownsAutomation = ownsAutomation;
        _processId = info.ProcessId;
        _processName = info.ProcessName;
        _title = info.Title;
        _nativeWindowHandle = info.NativeWindowHandle;
    }

    public int SessionId => _registry.SessionId;
    public string ProcessName => _processName;
    public int ProcessId => _processId;
    public string Title => _title;
    public nint NativeWindowHandle => _nativeWindowHandle;

    /// <summary>
    /// テスト専用: FlaUI 依存を持たない最小限の <see cref="WindowSession"/> を生成する。
    /// Snapshot / Click / Fill / Close / Kill 等の操作は呼び出してはならない (NRE になる)。
    /// </summary>
    internal static WindowSession CreateForTest(int sessionId, WindowInfo info)
        => new(
            automation: null!,
            window: null!,
            sessionId: sessionId,
            info: info,
            gate: new SemaphoreSlim(1, 1),
            logger: null,
            ownsAutomation: false);

    public Task<SnapshotResult> SnapshotAsync(SnapshotOptions? options = null, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();
            var opt = options ?? new SnapshotOptions();

            var modals = DetectModalElements();
            var now = DateTimeOffset.UtcNow;
            var input = new SnapshotBuildInput(
                _rootElement, modals, opt,
                WindowTitle: Title,
                ProcessName: ProcessName,
                ProcessId: ProcessId,
                GeneratedAt: now);

            try
            {
                var builder = new SnapshotBuilder(_registry);
                var built = builder.Build(input);
                return Task.FromResult(new SnapshotResult(
                    Json: built.Json,
                    SessionId: built.SessionId,
                    WindowTitle: Title,
                    ProcessName: ProcessName,
                    ProcessId: ProcessId,
                    GeneratedAt: now));
            }
            catch (Exception ex) when (ex is not AdactException)
            {
                throw new SnapshotException("Snapshot construction failed.", ex);
            }
        }, ct);
    }

    public Task ClickAsync(string refId, ClickOptions? options = null, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(async c =>
        {
            c.ThrowIfCancellationRequested();
            var el = _registry.Resolve(refId);
            try
            {
                try { _window.Focus(); } catch { /* best effort */ }
                el.Click();
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(refId, "click", ex.Message, ex);
            }
            await AutoWaitAfterInteractionAsync(c).ConfigureAwait(false);
        }, ct);
    }

    public Task FillAsync(string refId, string text, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(async c =>
        {
            c.ThrowIfCancellationRequested();
            var el = _registry.Resolve(refId);
            try
            {
                el.Fill(text);
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(refId, "fill", ex.Message, ex);
            }
            await AutoWaitAfterInteractionAsync(c).ConfigureAwait(false);
        }, ct);
    }

    /// <summary>
    /// UIA 操作を Engine と共有の gate で直列化して実行する。
    /// </summary>
    private async Task<T> RunSerializedAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await action(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task RunSerializedAsync(Func<CancellationToken, Task> action, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await action(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// γ 案 auto-wait: UIA WaitWhileBusy 相当として <see cref="Process.WaitForInputIdle(int)"/> を
    /// 上限 1000ms で呼び、続けて 50ms スリープする。busy 解除待機の例外は無視 (best-effort)。
    /// 詳細は 002_アーキテクチャ設計.md §6.6 参照。
    /// </summary>
    private async Task AutoWaitAfterInteractionAsync(CancellationToken ct)
    {
        try
        {
            using var p = Process.GetProcessById(_processId);
            try { p.WaitForInputIdle(1000); }
            catch (Exception ex) { _logger.LogDebug(ex, "WaitForInputIdle failed (ignored, best effort)"); }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "GetProcessById failed during auto-wait (ignored)");
        }
        await Task.Delay(50, ct).ConfigureAwait(false);
    }

    public void Detach() => Dispose();

    /// <summary>
    /// UIA <c>WindowPattern.Close()</c> でウィンドウを閉じる。Pattern が取れなければ
    /// WM_CLOSE PostMessage にフォールバックする。失敗時は <see cref="CloseFailedException"/>。
    /// 成功・失敗に関わらず本メソッドはセッションの Dispose は行わない (呼び出し側が管理する)。
    /// </summary>
    public Task CloseAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();
            try
            {
                var windowPattern = _window.Patterns.Window.PatternOrDefault;
                if (windowPattern is not null)
                {
                    windowPattern.Close();
                    return Task.CompletedTask;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "WindowPattern.Close() failed; falling back to WM_CLOSE");
            }

            var hwnd = _nativeWindowHandle;
            if (hwnd == IntPtr.Zero)
                throw new CloseFailedException("Window does not expose a native handle; WindowPattern was unavailable.");

            try
            {
                if (!NativeMethods.PostMessage(hwnd, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero))
                {
                    var err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    throw new CloseFailedException($"PostMessage(WM_CLOSE) failed with Win32 error {err}.");
                }
            }
            catch (CloseFailedException) { throw; }
            catch (Exception ex)
            {
                throw new CloseFailedException("Window close failed.", ex);
            }
            return Task.CompletedTask;
        }, ct);
    }

    /// <summary>
    /// 対応プロセスを <see cref="Process.Kill(bool)"/> (entireProcessTree:true) で強制終了する。
    /// 失敗時は <see cref="KillFailedException"/>。本メソッドはセッションの Dispose は行わない。
    /// </summary>
    // TODO(post-Phase5): PID 再利用対策として ProcessStartTime での同一性検証を追加する余地あり。
    public Task KillAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();
            try
            {
                using var p = Process.GetProcessById(_processId);
                p.Kill(entireProcessTree: true);
            }
            // プロセスが既に終了している場合も KILL_FAILED として返す (auto-detach はしない)。
            // 判断は設計 §4.5 を参照。
            catch (ArgumentException ex)
            {
                throw new KillFailedException($"Process {_processId} is no longer running.", ex);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                throw new KillFailedException($"Process.Kill failed (Win32): {ex.Message}", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new KillFailedException($"Process.Kill failed (invalid state): {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new KillFailedException("Process kill failed.", ex);
            }
            return Task.CompletedTask;
        }, ct);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_ownsAutomation)
        {
            try { _automation.Dispose(); } catch { }
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// このセッションのプロセスに属し、Owner がメインウィンドウでメインが WS_DISABLED な
    /// 別ウィンドウをモーダルダイアログとして検出する。
    /// </summary>
    private IReadOnlyList<IElement> DetectModalElements()
    {
        IntPtr ownerHwnd;
        try { ownerHwnd = _window.Properties.NativeWindowHandle.ValueOrDefault; }
        catch { return Array.Empty<IElement>(); }

        if (ownerHwnd == IntPtr.Zero) return Array.Empty<IElement>();

        // メインウィンドウが enabled なら通常ケース (モーダルがないので速攻リターンしてもよいが、
        // 念のため owner 紐付けはチェックする — ファイルダイアログ等は親が disabled)。
        bool ownerDisabled = !NativeMethods.IsWindowEnabled(ownerHwnd);

        var ownerPid = (uint)ProcessId;
        var modalHwnds = new List<IntPtr>();
        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (hWnd == ownerHwnd) return true;
            if (!NativeMethods.IsWindowVisible(hWnd)) return true;

            _ = (nint)NativeMethods.GetWindowThreadProcessId(hWnd, out var pid);
            if (pid != ownerPid) return true;

            var owner = NativeMethods.GetWindow(hWnd, NativeMethods.GW_OWNER);
            if (owner != ownerHwnd) return true;

            // Owner が disabled = 真のモーダル。enabled な場合はフローティングペインなので除外。
            if (!ownerDisabled) return true;

            modalHwnds.Add(hWnd);
            return true;
        }, IntPtr.Zero);

        if (modalHwnds.Count == 0) return Array.Empty<IElement>();

        var result = new List<IElement>(modalHwnds.Count);
        foreach (var hwnd in modalHwnds)
        {
            try
            {
                var el = _automation.FromHandle(hwnd);
                if (el is not null) result.Add(new FlaUiElement(el));
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(ex, "Failed to wrap modal window handle {Hwnd}", hwnd);
                }
            }
        }
        return result;
    }
}
