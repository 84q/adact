using System.Diagnostics;

using Adact.Engine.Exceptions;

using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Adact.Engine;

/// <summary>
/// ADACT Engine のエントリポイント。ウィンドウ列挙とアタッチを担当する。
/// 内部に <see cref="UIA3Automation"/> を 1 個だけ保持し、Session 間で共有する。
/// </summary>
public sealed class UiaEngine : IDisposable
{
    private readonly AutomationBase _automation;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<UiaEngine> _logger;
    // UIA はマシン全体で前面ウィンドウを取り合うため、Engine と Engine が払い出す
    // WindowSession の UIA 操作はマシン内で 1 本に直列化する。Engine と全 Session で
    // 同じインスタンスを共有する。詳細は 006_Phase4_設計.md §5 参照。
    private readonly SemaphoreSlim _gate = new(1, 1);
    private int _nextSessionId;
    private bool _disposed;

    public UiaEngine(ILoggerFactory? loggerFactory = null)
        : this(new UIA3Automation(), loggerFactory)
    {
    }

    /// <summary>
    /// テスト容易性および将来の UIA2 fallback のため、外部から <see cref="AutomationBase"/> を注入可能。
    /// 渡された automation は本インスタンスが Dispose する。
    /// </summary>
    internal UiaEngine(AutomationBase automation, ILoggerFactory? loggerFactory = null)
    {
        _automation = automation;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<UiaEngine>();
    }

    public Task<IReadOnlyList<WindowInfo>> ListWindowsAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync<IReadOnlyList<WindowInfo>>(c =>
        {
            c.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<WindowInfo>>(ListWindowsCore());
        }, ct);
    }

    private List<WindowInfo> ListWindowsCore()
    {
        var desktop = _automation.GetDesktop();
        var windows = desktop.FindAllChildren(cf => cf.ByControlType(ControlType.Window));
        var list = new List<WindowInfo>(windows.Length);
        var seenHwnds = new HashSet<nint>();
        foreach (var w in windows)
        {
            try
            {
                var hwnd = w.Properties.NativeWindowHandle.ValueOrDefault;
                // 可視 & オンスクリーンのみを採用 (UWP の隠れた CoreWindow を除外)
                if (hwnd == IntPtr.Zero) continue;
                if (!NativeMethods.IsWindowVisible(hwnd)) continue;
                if (w.Properties.IsOffscreen.ValueOrDefault) continue;
                if (!seenHwnds.Add(hwnd)) continue;

                var pid = w.Properties.ProcessId.ValueOrDefault;
                var procName = "?";
                try { procName = Process.GetProcessById(pid).ProcessName; } catch { }
                var title = w.Properties.Name.ValueOrDefault ?? "";
                var ctrl = SafeControlType(w);
                var className = w.Properties.ClassName.ValueOrDefault;
                list.Add(new WindowInfo(pid, procName, title, ctrl, NullIfEmpty(className), hwnd));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to read a window during ListWindowsAsync; skipping.");
            }
        }
        return list;
    }

    /// <summary>
    /// HWND 直指定で attach する。AttachQuery によるマッチングを経ず、HWND 一致で 1 件確定する。
    /// 該当 HWND が現在の列挙に含まれない、もしくは <c>FromHandle</c> が失敗した場合は
    /// <see cref="WindowNotFoundException"/> を throw する。
    /// </summary>
    public Task<WindowSession> AttachByHandleAsync(nint hwnd, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();
            var all = ListWindowsCore();
            var target = all.FirstOrDefault(w => w.NativeWindowHandle == hwnd);
            if (target is null)
                throw new WindowNotFoundException(new AttachQuery(ProcessId: null));

            AutomationElement? raw;
            try
            {
                raw = _automation.FromHandle(target.NativeWindowHandle);
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(ex, "FromHandle failed for hwnd {Hwnd}", target.NativeWindowHandle);
                }
                throw new WindowNotFoundException(new AttachQuery(ProcessId: null));
            }
            if (raw is null)
                throw new WindowNotFoundException(new AttachQuery(ProcessId: null));

            var sessionId = Interlocked.Increment(ref _nextSessionId);
            var session = new WindowSession(
                _automation,
                raw.AsWindow(),
                sessionId,
                target,
                _gate,
                _loggerFactory.CreateLogger<WindowSession>(),
                ownsAutomation: false);
            return Task.FromResult(session);
        }, ct);
    }

    /// <summary>
    /// 指定 <see cref="AttachQuery"/> にマッチする現在の top-level window 一覧を返す。
    /// attach は行わない。WindowsTools 側で attach 前に WindowKey を確定するために使用する。
    /// </summary>
    public Task<IReadOnlyList<WindowInfo>> FindMatchesAsync(AttachQuery query, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();
            var all = ListWindowsCore();
            IReadOnlyList<WindowInfo> matches = all.Where(w => Matches(w, query)).ToList();
            return Task.FromResult(matches);
        }, ct);
    }

    public Task<WindowSession> AttachAsync(AttachQuery query, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();
            // 同 gate 内のため self-deadlock 回避目的に ListWindowsAsync ではなく Core を直接呼ぶ
            var all = ListWindowsCore();
            var matches = all.Where(w => Matches(w, query)).ToList();

            if (matches.Count == 0)
                throw new WindowNotFoundException(query);
            if (matches.Count > 1)
                throw new AmbiguousAttachException(query, matches);

            var target = matches[0];
            AutomationElement? raw;
            try
            {
                raw = _automation.FromHandle(target.NativeWindowHandle);
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(ex, "FromHandle failed for hwnd {Hwnd}", target.NativeWindowHandle);
                }
                throw new WindowNotFoundException(query);
            }
            if (raw is null)
                throw new WindowNotFoundException(query);

            var sessionId = Interlocked.Increment(ref _nextSessionId);
            var session = new WindowSession(
                _automation,
                raw.AsWindow(),
                sessionId,
                target,
                _gate,
                _loggerFactory.CreateLogger<WindowSession>(),
                ownsAutomation: false);
            return Task.FromResult(session);
        }, ct);
    }

    /// <summary>
    /// UIA 操作を直列化して実行する。Engine と Engine から払い出された全 WindowSession の
    /// UIA 操作はこの gate を共有する。
    /// </summary>
    internal async Task<T> RunSerializedAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
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

    /// <summary>
    /// 戻り値なし版の <see cref="RunSerializedAsync{T}"/>。
    /// </summary>
    internal async Task RunSerializedAsync(Func<CancellationToken, Task> action, CancellationToken ct)
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

    internal static bool Matches(WindowInfo w, AttachQuery q)
    {
        if (q.ProcessId is not null && w.ProcessId != q.ProcessId.Value) return false;
        if (q.ProcessName is not null
            && !string.Equals(w.ProcessName, q.ProcessName, StringComparison.OrdinalIgnoreCase)) return false;
        if (q.WindowTitle is not null
            && !string.Equals(w.Title, q.WindowTitle, StringComparison.OrdinalIgnoreCase)) return false;
        if (q.ClassName is not null
            && !string.Equals(w.ClassName, q.ClassName, StringComparison.OrdinalIgnoreCase)) return false;

        // 全フィールド null の AttachQuery はマッチさせない (誤用検知)
        if (q.ProcessId is null && q.ProcessName is null && q.WindowTitle is null && q.ClassName is null) return false;
        return true;
    }

    private static string SafeControlType(AutomationElement el)
    {
        try { return el.ControlType.ToString(); } catch { return "Unknown"; }
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrEmpty(s) ? null : s;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _automation.Dispose(); } catch { }
        try { _gate.Dispose(); } catch { }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
