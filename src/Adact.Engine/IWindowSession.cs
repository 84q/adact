using Adact.Engine.Snapshot;

namespace Adact.Engine;

/// <summary>
/// Represents an attached window session.
/// </summary>
public interface IWindowSession : IDisposable
{
    /// <summary>
    /// Gets the session ID.
    /// </summary>
    int SessionId { get; }

    /// <summary>
    /// Gets the owning process name.
    /// </summary>
    string ProcessName { get; }

    /// <summary>
    /// Gets the owning process ID.
    /// </summary>
    int ProcessId { get; }

    /// <summary>
    /// Gets the current window title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the native window handle.
    /// </summary>
    nint NativeWindowHandle { get; }

    /// <summary>
    /// Takes a snapshot of the window.
    /// </summary>
    Task<SnapshotResult> SnapshotAsync(SnapshotOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Clicks an element identified by ref.
    /// </summary>
    Task ClickAsync(string refId, ClickOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Clicks an element with explicit mouse options.
    /// </summary>
    Task ClickWithOptionsAsync(string refId, ClickOptions options, CancellationToken ct = default);

    /// <summary>
    /// Double-clicks an element identified by ref.
    /// </summary>
    Task DoubleClickAsync(string refId, ClickOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Fills text into an element identified by ref.
    /// </summary>
    Task FillAsync(string refId, string text, CancellationToken ct = default);

    /// <summary>
    /// Sends a key press to the window or a specific element.
    /// </summary>
    Task PressAsync(string key, string? refId = null, CancellationToken ct = default);

    /// <summary>
    /// Sends a key-down event.
    /// </summary>
    Task KeyDownAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Sends a key-up event.
    /// </summary>
    Task KeyUpAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Types text into the window or a specific element.
    /// </summary>
    Task TypeAsync(string? refId, string text, int delayMs = 0, CancellationToken ct = default);

    /// <summary>
    /// Moves the mouse over an element.
    /// </summary>
    Task HoverAsync(string refId, IReadOnlyList<string>? modifiers = null, int? positionX = null, int? positionY = null, CancellationToken ct = default);

    /// <summary>
    /// Moves the mouse pointer to a point or element.
    /// </summary>
    Task MouseMoveAsync(MouseTarget target, CancellationToken ct = default);

    /// <summary>
    /// Presses a mouse button at a point or element.
    /// </summary>
    Task MouseDownAsync(MouseTarget target, MouseButton button, CancellationToken ct = default);

    /// <summary>
    /// Releases a mouse button at a point or element.
    /// </summary>
    Task MouseUpAsync(MouseTarget target, MouseButton button, CancellationToken ct = default);

    /// <summary>
    /// Scrolls the mouse wheel at a point or element.
    /// </summary>
    Task MouseWheelAsync(MouseTarget target, int deltaX, int deltaY, CancellationToken ct = default);

    /// <summary>
    /// Checks an element.
    /// </summary>
    Task CheckAsync(string refId, CancellationToken ct = default);

    /// <summary>
    /// Unchecks an element.
    /// </summary>
    Task UncheckAsync(string refId, CancellationToken ct = default);

    /// <summary>
    /// Selects child items within a container element.
    /// </summary>
    Task SelectAsync(string refId, SelectionTarget[] targets, SelectionMode mode = SelectionMode.Replace, CancellationToken ct = default);

    /// <summary>
    /// Focuses an element.
    /// </summary>
    Task FocusAsync(string refId, CancellationToken ct = default);

    /// <summary>
    /// Scrolls an element into view.
    /// </summary>
    Task ScrollIntoViewAsync(string refId, CancellationToken ct = default);

    /// <summary>
    /// Scrolls an element using the specified mode.
    /// </summary>
    Task ScrollAsync(string refId, ScrollMode mode, CancellationToken ct = default);

    /// <summary>
    /// Inspects an element and returns its properties.
    /// </summary>
    Task<InspectResult> InspectAsync(string refId, CancellationToken ct = default);

    /// <summary>
    /// Captures a screenshot of the window or a specific element.
    /// </summary>
    Task<ScreenshotResult> ScreenshotAsync(string? refId, string? outPath, CancellationToken ct = default);

    /// <summary>
    /// Resizes the attached window.
    /// </summary>
    Task ResizeAsync(int? width, int? height, CancellationToken ct = default);

    /// <summary>
    /// Minimizes the attached window.
    /// </summary>
    Task MinimizeAsync(CancellationToken ct = default);

    /// <summary>
    /// Maximizes the attached window.
    /// </summary>
    Task MaximizeAsync(CancellationToken ct = default);

    /// <summary>
    /// Restores the attached window.
    /// </summary>
    Task RestoreAsync(CancellationToken ct = default);

    /// <summary>
    /// Waits for a ref to reach a specific state.
    /// </summary>
    Task<WaitForResult> WaitForRefAsync(string refId, WaitForState state, TimeSpan timeout, CancellationToken ct = default);

    /// <summary>
    /// Waits for an element query to reach a specific state.
    /// </summary>
    Task<WaitForResult> WaitForQueryAsync(WaitForElementQuery query, WaitForState state, TimeSpan timeout, CancellationToken ct = default);

    /// <summary>
    /// Closes the attached window.
    /// </summary>
    Task CloseAsync(CancellationToken ct = default);

    /// <summary>
    /// Kills the attached process.
    /// </summary>
    Task<KillMethod> KillAsync(bool force = false, int timeoutMs = 5000, CancellationToken ct = default);
}
