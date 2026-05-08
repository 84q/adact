namespace Adact.Engine.Exceptions;

/// <summary>
/// Thrown when a wait-for operation times out.
/// </summary>
public sealed class WaitTimeoutException : AdactException
{
    /// <summary>
    /// Creates a new wait-timeout exception.
    /// </summary>
    public WaitTimeoutException(string message) : base(message) { }
}
