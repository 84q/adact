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
/// Provides UI automation access to top-level windows and sessions.
/// </summary>
public sealed partial class UiaEngine : IDisposable
{
    private readonly AutomationBase _automation;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<UiaEngine> _logger;
    /// <summary>
    /// </summary>
    private readonly SemaphoreSlim _gate = new(1, 1);
    private int _nextSessionId;
    private int _disposed;

    /// <summary>
    /// Creates a new UIA engine.
    /// </summary>
    public UiaEngine(ILoggerFactory? loggerFactory = null)
        : this(new UIA3Automation(), loggerFactory)
    {
    }

    /// <summary>
    /// Creates a new UIA engine using the specified automation instance.
    /// </summary>
    internal UiaEngine(AutomationBase automation, ILoggerFactory? loggerFactory = null)
    {
        _automation = automation;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<UiaEngine>();
    }

    /// <summary>
    /// Lists all top-level windows visible to UIA.
    /// </summary>
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

    /// <summary>
    /// Builds the current top-level window list.
    /// </summary>
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
                if (hwnd == IntPtr.Zero) continue;
                if (!NativeMethods.IsWindowVisible(hwnd)) continue;
                if (w.Properties.IsOffscreen.ValueOrDefault) continue;
                if (!seenHwnds.Add(hwnd)) continue;

                var pid = w.Properties.ProcessId.ValueOrDefault;
                var procName = "?";
                DateTimeOffset? processStartTimeUtc = null;
                try
                {
                    using var process = Process.GetProcessById(pid);
                    procName = process.ProcessName;
                    try
                    {
                        processStartTimeUtc = process.StartTime.ToUniversalTime();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to read process start time for pid {Pid}", pid);
                    }
                }
                catch (Exception ex) { _logger.LogTrace(ex, "Failed to get process info for pid {Pid}", pid); }
                var title = w.Properties.Name.ValueOrDefault ?? "";
                var ctrl = SafeControlType(w);
                var className = w.Properties.ClassName.ValueOrDefault;
                list.Add(new WindowInfo(pid, procName, title, ctrl, NullIfEmpty(className), hwnd, processStartTimeUtc));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to read a window during ListWindowsAsync; skipping.");
            }
        }
        return list;
    }

    /// <summary>
    /// Attaches to a window by native handle.
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
                throw new WindowNotFoundException(hwnd);

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
                throw new WindowNotFoundException(hwnd);
            }
            if (raw is null)
                throw new WindowNotFoundException(hwnd);

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
    /// Runs an asynchronous action under the engine gate.
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
    /// Runs an asynchronous action under the engine gate.
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

    /// <summary>
    /// Gets the control type string for an element.
    /// </summary>
    private string SafeControlType(AutomationElement el)
    {
        try { return el.ControlType.ToString(); } catch (Exception ex) { _logger.LogTrace(ex, "Failed to get ControlType"); return "Unknown"; }
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrEmpty(s) ? null : s;

    /// <summary>
    /// Disposes the engine and its resources.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
        {
            return;
        }
        try { _automation.Dispose(); } catch (Exception ex) { _logger.LogTrace(ex, "Dispose failed for automation"); }
        try { _gate.Dispose(); } catch (Exception ex) { _logger.LogTrace(ex, "Dispose failed for gate"); }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);
    }
}
