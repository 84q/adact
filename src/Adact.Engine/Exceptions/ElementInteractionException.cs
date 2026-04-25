namespace Adact.Engine.Exceptions;

public sealed class ElementInteractionException : AdactException
{
    public string RefId { get; }
    public string Operation { get; }

    public ElementInteractionException(string refId, string operation, string message)
        : base($"Failed to {operation} on ref '{refId}': {message}")
    {
        RefId = refId;
        Operation = operation;
    }

    public ElementInteractionException(string refId, string operation, string message, Exception inner)
        : base($"Failed to {operation} on ref '{refId}': {message}", inner)
    {
        RefId = refId;
        Operation = operation;
    }
}
