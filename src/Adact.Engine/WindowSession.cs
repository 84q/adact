using System.Diagnostics;

using Adact.Engine.Elements;
using Adact.Engine.Exceptions;
using Adact.Engine.Filters;
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
    private readonly FilterStrategyRegistry _filters;
    private readonly ILogger<WindowSession> _logger;
    private readonly bool _ownsAutomation;
    private readonly int _processId;
    private readonly string _processName;
    private readonly string _title;
    private bool _disposed;

    internal WindowSession(
        AutomationBase automation,
        Window window,
        int sessionId,
        WindowInfo info,
        FilterStrategyRegistry filters,
        ILogger<WindowSession>? logger = null,
        bool ownsAutomation = false)
    {
        _automation = automation;
        _window = window;
        _rootElement = new FlaUiElement(window);
        _registry = new RefRegistry(sessionId);
        _filters = filters;
        _logger = logger ?? NullLogger<WindowSession>.Instance;
        _ownsAutomation = ownsAutomation;
        _processId = info.ProcessId;
        _processName = info.ProcessName;
        _title = info.Title;
    }

    public int SessionId => _registry.SessionId;
    public string ProcessName => _processName;
    public int ProcessId => _processId;
    public string Title => _title;

    public Task<SnapshotResult> SnapshotAsync(SnapshotOptions? options = null, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        options ??= new SnapshotOptions();
        var filter = _filters.Get(options.FilterName);

        var modals = DetectModalElements();
        var now = DateTimeOffset.UtcNow;
        var input = new SnapshotBuildInput(
            _rootElement, modals, filter, options,
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
                Generation: built.Generation,
                FilterName: filter.Name,
                WindowTitle: Title,
                ProcessName: ProcessName,
                ProcessId: ProcessId,
                GeneratedAt: now));
        }
        catch (Exception ex) when (ex is not AdactException)
        {
            throw new SnapshotException("Snapshot construction failed.", ex);
        }
    }

    public Task ClickAsync(string refId, ClickOptions? options = null, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
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
        return AutoWaitAfterInteractionAsync(ct);
    }

    public Task FillAsync(string refId, string text, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
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
        return AutoWaitAfterInteractionAsync(ct);
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
        if (_disposed) throw new ObjectDisposedException(nameof(WindowSession));
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

            NativeMethods.GetWindowThreadProcessId(hWnd, out var pid);
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
                _logger.LogDebug(ex, "Failed to wrap modal window handle {Hwnd}", hwnd);
            }
        }
        return result;
    }
}
