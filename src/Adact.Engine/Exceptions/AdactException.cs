namespace Adact.Engine.Exceptions;

/// <summary>
/// Base class for engine-specific exceptions.
/// </summary>
public abstract class AdactException : Exception
{
    /// <summary>
    /// Initializes a new instance of the exception.
    /// </summary>
    protected AdactException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the exception with an inner exception.
    /// </summary>
    protected AdactException(string message, Exception inner) : base(message, inner) { }
}
