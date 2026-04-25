using System.Diagnostics;
using Adact.Engine.Exceptions;
using Adact.Engine.Filters;
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
    private readonly FilterStrategyRegistry _filters;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<UiaEngine> _logger;
    private int _nextSessionId;
    private bool _disposed;

    public UiaEngine(ILoggerFactory? loggerFactory = null, FilterStrategyRegistry? filters = null)
        : this(new UIA3Automation(), loggerFactory, filters)
    {
    }

    /// <summary>
    /// テスト容易性および将来の UIA2 fallback のため、外部から <see cref="AutomationBase"/> を注入可能。
    /// 渡された automation は本インスタンスが Dispose する。
    /// </summary>
    internal UiaEngine(AutomationBase automation, ILoggerFactory? loggerFactory = null, FilterStrategyRegistry? filters = null)
    {
        _automation = automation;
        _filters = filters ?? new FilterStrategyRegistry();
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<UiaEngine>();
    }

    public FilterStrategyRegistry Filters => _filters;

    public Task<IReadOnlyList<WindowInfo>> ListWindowsAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();

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
        return Task.FromResult<IReadOnlyList<WindowInfo>>(list);
    }

    public async Task<WindowSession> AttachAsync(AttachQuery query, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();

        var all = await ListWindowsAsync(ct).ConfigureAwait(false);
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
            _logger.LogDebug(ex, "FromHandle failed for hwnd {Hwnd}", target.NativeWindowHandle);
            throw new WindowNotFoundException(query);
        }
        if (raw is null)
            throw new WindowNotFoundException(query);

        var sessionId = Interlocked.Increment(ref _nextSessionId);
        return new WindowSession(
            _automation,
            raw.AsWindow(),
            sessionId,
            target,
            _filters,
            _loggerFactory.CreateLogger<WindowSession>(),
            ownsAutomation: false);
    }

    private static bool Matches(WindowInfo w, AttachQuery q)
    {
        if (q.ProcessId is not null && w.ProcessId != q.ProcessId.Value) return false;
        if (q.ProcessName is not null
            && !string.Equals(w.ProcessName, q.ProcessName, StringComparison.OrdinalIgnoreCase)) return false;
        if (q.WindowTitle is not null
            && !string.Equals(w.Title, q.WindowTitle, StringComparison.OrdinalIgnoreCase)) return false;

        // 全フィールド null の AttachQuery はマッチさせない (誤用検知)
        if (q.ProcessId is null && q.ProcessName is null && q.WindowTitle is null) return false;
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
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(UiaEngine));
    }
}
