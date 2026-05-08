namespace Adact.Engine.Exceptions;

/// <summary>
/// Thrown when an element interaction fails.
/// </summary>
public sealed class ElementInteractionException : AdactException
{
    /// <summary>
    /// Gets the ref ID that failed.
    /// </summary>
    public string RefId { get; }

    /// <summary>
    /// Gets the failed operation name.
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// Creates a new element-interaction exception.
    /// </summary>
    public ElementInteractionException(string refId, string operation, string message)
        : base($"Failed to {operation} on ref '{refId}': {message}")
    {
        RefId = refId;
        Operation = operation;
    }

    /// <summary>
    /// Creates a new element-interaction exception with an inner exception.
    /// </summary>
    public ElementInteractionException(string refId, string operation, string message, Exception inner)
        : base($"Failed to {operation} on ref '{refId}': {message}", inner)
    {
        RefId = refId;
        Operation = operation;
    }
}
