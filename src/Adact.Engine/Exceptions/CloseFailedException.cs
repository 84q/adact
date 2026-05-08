namespace Adact.Engine.Exceptions;

/// <summary>
/// Thrown when closing a window fails.
/// </summary>
public sealed class CloseFailedException : AdactException
{
    /// <summary>
    /// Creates a new close-failed exception.
    /// </summary>
    public CloseFailedException(string message) : base(message) { }

    /// <summary>
    /// Creates a new close-failed exception with an inner exception.
    /// </summary>
    public CloseFailedException(string message, Exception inner) : base(message, inner) { }
}
