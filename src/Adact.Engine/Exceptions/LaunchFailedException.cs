namespace Adact.Engine.Exceptions;

/// <summary>
/// Thrown when launching a process fails.
/// </summary>
public sealed class LaunchFailedException : AdactException
{
    /// <summary>
    /// Creates a new launch-failed exception.
    /// </summary>
    public LaunchFailedException(string message) : base(message) { }

    /// <summary>
    /// Creates a new launch-failed exception with an inner exception.
    /// </summary>
    public LaunchFailedException(string message, Exception inner) : base(message, inner) { }
}
