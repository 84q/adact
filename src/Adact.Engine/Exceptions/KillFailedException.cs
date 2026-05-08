namespace Adact.Engine.Exceptions;

/// <summary>
/// Thrown when terminating a process fails.
/// </summary>
public sealed class KillFailedException : AdactException
{
    /// <summary>
    /// Creates a new kill-failed exception.
    /// </summary>
    public KillFailedException(string message) : base(message) { }

    /// <summary>
    /// Creates a new kill-failed exception with an inner exception.
    /// </summary>
    public KillFailedException(string message, Exception inner) : base(message, inner) { }
}
