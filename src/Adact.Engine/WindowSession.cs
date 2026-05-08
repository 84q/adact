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
/// Represents an attached window session.
/// </summary>
public sealed partial class WindowSession : IWindowSession
{
    private readonly AutomationBase _automation;
    private readonly Window _window;
    private readonly IElement _rootElement;
    private readonly RefRegistry _registry;
    private readonly SemaphoreSlim _gate;
    private readonly ILogger<WindowSession> _logger;
    private readonly IWindowInteractionDriver _interaction;
    private readonly bool _ownsAutomation;
    private readonly bool _ownsGate;
    private readonly int _processId;
    private readonly DateTimeOffset? _processStartTimeUtc;
    private readonly string _processName;
    private readonly string _title;
    private readonly nint _nativeWindowHandle;
    private readonly int _sessionId;
    private int _disposed;

    /// <summary>
    /// Creates a new attached window session.
    /// </summary>
    internal WindowSession(
        AutomationBase automation,
        Window window,
        int sessionId,
        WindowInfo info,
        SemaphoreSlim gate,
        ILogger<WindowSession>? logger = null,
        bool ownsAutomation = false,
        bool ownsGate = false,
        IElement? rootElement = null,
        IWindowInteractionDriver? interaction = null)
    {
        _automation = automation;
        _window = window;
        _logger = logger ?? NullLogger<WindowSession>.Instance;
        _rootElement = rootElement ?? new FlaUiElement(window, _logger);
        _registry = new RefRegistry(sessionId);
        _gate = gate;
        _interaction = interaction ?? new FlaUiWindowInteractionDriver(window, info.ProcessId, _logger);
        _ownsAutomation = ownsAutomation;
        _ownsGate = ownsGate;
        _processId = info.ProcessId;
        _processStartTimeUtc = info.ProcessStartTimeUtc;
        _processName = info.ProcessName;
        _title = info.Title;
        _nativeWindowHandle = info.NativeWindowHandle;
        _sessionId = Process.GetCurrentProcess().SessionId;
    }

    /// <summary>
    /// Gets the session ID.
    /// </summary>
    public int SessionId => _registry.SessionId;

    /// <summary>
    /// Gets the owning process name.
    /// </summary>
    public string ProcessName => _processName;

    /// <summary>
    /// Gets the owning process ID.
    /// </summary>
    public int ProcessId => _processId;

    /// <summary>
    /// Gets the current window title.
    /// </summary>
    public string Title => _title;

    /// <summary>
    /// Gets the native window handle.
    /// </summary>
    public nint NativeWindowHandle => _nativeWindowHandle;

    /// <summary>
    /// Creates a test session without a live UIA attachment.
    /// </summary>
    internal static WindowSession CreateForTest(int sessionId, WindowInfo info)
        => new(
            automation: null!,
            window: null!,
            sessionId: sessionId,
            info: info,
            gate: new SemaphoreSlim(1, 1),
            logger: null,
            ownsAutomation: false,
            ownsGate: true);

    /// <summary>
    /// Creates a test session backed by a fake root element.
    /// </summary>
    internal static WindowSession CreateForTest(
        int sessionId,
        WindowInfo info,
        IElement rootElement,
        IWindowInteractionDriver? interaction = null)
        => new(
            automation: null!,
            window: null!,
            sessionId: sessionId,
            info: info,
            gate: new SemaphoreSlim(1, 1),
            logger: null,
            ownsAutomation: false,
            ownsGate: true,
            rootElement: rootElement,
            interaction: interaction ?? new NoopWindowInteractionDriver());

    /// <summary>
    /// Takes a snapshot of the attached window.
    /// </summary>
    public Task<SnapshotResult> SnapshotAsync(SnapshotOptions? options = null, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();
            var opt = options ?? new SnapshotOptions();

            var modals = DetectModalElements();
            var popups = DetectPopupElements(modals);
            _rootElement.ClearChildrenCache();
            var now = DateTimeOffset.UtcNow;
            var input = new SnapshotBuildInput(
                _rootElement, modals, popups, opt,
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

    /// <summary>
    /// Clicks an element identified by ref.
    /// </summary>
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
                try { _window.Focus(); } catch (Exception ex) { _logger.LogTrace(ex, "Focus attempt failed"); }
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

    /// <summary>
    /// Fills text into an element identified by ref.
    /// </summary>
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
    /// </summary>
    private async Task<T> RunSerializedAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await action(ct).ConfigureAwait(false);
        }
        catch (OperationBlockedException)
        {
            throw;
        }
        catch (AdactException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var blocked = OperationBlockerDetector.Detect(_sessionId, _nativeWindowHandle);
            if (blocked.IsBlocked)
                throw new OperationBlockedException(blocked.Reason!, ex);
            throw;
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
        catch (OperationBlockedException)
        {
            throw;
        }
        catch (AdactException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var blocked = OperationBlockerDetector.Detect(_sessionId, _nativeWindowHandle);
            if (blocked.IsBlocked)
                throw new OperationBlockedException(blocked.Reason!, ex);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task AutoWaitAfterInteractionAsync(CancellationToken ct)
        => await _interaction.WaitAfterInteractionAsync(ct).ConfigureAwait(false);

    /// <summary>
    /// Detaches the session and disposes it.
    /// </summary>
    public void Detach() => Dispose();

    /// <summary>
    /// Closes the attached window.
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
    /// Kills the attached process.
    /// </summary>
    public Task<KillMethod> KillAsync(bool force = false, int timeoutMs = 5000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(async c =>
        {
            c.ThrowIfCancellationRequested();
            try
            {
                if (_processStartTimeUtc is null)
                {
                    throw new KillFailedException(
                        $"Refusing to kill process {_processId} because the original process start time is unknown.");
                }

                using var p = Process.GetProcessById(_processId);
                DateTimeOffset currentProcessStartTimeUtc;
                try
                {
                    currentProcessStartTimeUtc = p.StartTime.ToUniversalTime();
                }
                catch (Exception ex)
                {
                    throw new KillFailedException(
                        $"Refusing to kill process {_processId} because its current start time could not be read.", ex);
                }

                if (currentProcessStartTimeUtc != _processStartTimeUtc.Value)
                {
                    throw new KillFailedException(
                        $"Refusing to kill process {_processId} because the PID now belongs to a different process.");
                }

                if (force)
                {
                    p.Kill(entireProcessTree: true);
                    return KillMethod.Forced;
                }

                var pid = (uint)_processId;
                var hwnds = new List<IntPtr>();
                NativeMethods.EnumWindows((hwnd, lParam) =>
                {
#pragma warning disable CA1806 // GetWindowThreadProcessId: we only need the out param (processId)
                    NativeMethods.GetWindowThreadProcessId(hwnd, out var windowPid);
#pragma warning restore CA1806
                    if (windowPid == pid)
                        hwnds.Add(hwnd);
                    return true;
                }, IntPtr.Zero);

                foreach (var hwnd in hwnds)
                {
                    NativeMethods.PostMessage(hwnd, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(c);
                timeoutCts.CancelAfter(timeoutMs);
                try
                {
                    await p.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
                    return KillMethod.Graceful;
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !c.IsCancellationRequested)
                {
                    p.Kill(entireProcessTree: true);
                    return KillMethod.ForcedAfterTimeout;
                }
            }
            catch (ArgumentException ex)
            {
                throw new KillFailedException($"Process {_processId} is no longer running.", ex);
            }
            catch (KillFailedException)
            {
                throw;
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
        }, ct);
    }

    /// <summary>
    /// Disposes the session and releases owned resources.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0) return;
        if (_ownsAutomation)
        {
            try { _automation.Dispose(); } catch (Exception ex) { _logger.LogTrace(ex, "Dispose failed for automation"); }
        }
        if (_ownsGate)
        {
            try { _gate.Dispose(); } catch (Exception ex) { _logger.LogTrace(ex, "Dispose failed for gate"); }
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);
    }

    /// <summary>
    /// </summary>
    private IReadOnlyList<IElement> DetectModalElements()
    {
        IntPtr ownerHwnd;
        try { ownerHwnd = _window.Properties.NativeWindowHandle.ValueOrDefault; }
        catch (Exception ex) { _logger.LogTrace(ex, "Failed to get NativeWindowHandle for modal detection"); return Array.Empty<IElement>(); }

        if (ownerHwnd == IntPtr.Zero) return Array.Empty<IElement>();

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
                if (el is not null) result.Add(new FlaUiElement(el, _logger));
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

    /// <summary>
    /// </summary>
    private IReadOnlyList<IElement> DetectPopupElements(IReadOnlyList<IElement> modalElements)
    {
        var ownerHwnd = _nativeWindowHandle;
        if (ownerHwnd == nint.Zero)
        {
            try { ownerHwnd = _window.Properties.NativeWindowHandle.ValueOrDefault; }
            catch (Exception ex) { _logger.LogTrace(ex, "Failed to get NativeWindowHandle for popup detection"); return Array.Empty<IElement>(); }
        }

        if (ownerHwnd == nint.Zero) return Array.Empty<IElement>();

        var modalHwnds = new HashSet<nint>();
        foreach (var modal in modalElements)
        {
            if (modal is FlaUiElement fe)
            {
                try
                {
                    var hwnd = fe.Inner.Properties.NativeWindowHandle.ValueOrDefault;
                    if (hwnd != nint.Zero) modalHwnds.Add(hwnd);
                }
                catch (Exception ex)
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug(ex, "Failed to get window handle for modal element");
                    }
                }
            }
        }

        var ownerPid = (uint)ProcessId;
        var popupHwnds = new List<nint>();
        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (hWnd == ownerHwnd) return true;
            if (!NativeMethods.IsWindowVisible(hWnd)) return true;

            _ = (nint)NativeMethods.GetWindowThreadProcessId(hWnd, out var pid);
            if (pid != ownerPid) return true;

            if (modalHwnds.Contains(hWnd)) return true;

            popupHwnds.Add(hWnd);
            return true;
        }, IntPtr.Zero);

        if (popupHwnds.Count == 0) return Array.Empty<IElement>();

        var result = new List<IElement>(popupHwnds.Count);
        foreach (var hwnd in popupHwnds)
        {
            try
            {
                var el = _automation.FromHandle(hwnd);
                if (el is not null) result.Add(new FlaUiElement(el, _logger));
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(ex, "Failed to wrap popup window handle {Hwnd}", hwnd);
                }
            }
        }
        return result;
    }
}
