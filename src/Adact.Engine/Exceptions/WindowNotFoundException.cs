namespace Adact.Engine.Exceptions;

/// <summary>
/// Thrown when a requested window cannot be found.
/// </summary>
public sealed class WindowNotFoundException : AdactException
{
    /// <summary>
    /// Gets the window handle that could not be resolved.
    /// </summary>
    public nint Hwnd { get; }

    /// <summary>
    /// Creates a new window-not-found exception.
    /// </summary>
    public WindowNotFoundException(nint hwnd)
        : base($"No window found for hwnd 0x{hwnd.ToInt64():X}.")
    {
        Hwnd = hwnd;
    }
}
