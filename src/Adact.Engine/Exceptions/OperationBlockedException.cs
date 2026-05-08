namespace Adact.Engine.Exceptions;

/// <summary>
/// Thrown when the current desktop session blocks an operation.
/// </summary>
public sealed class OperationBlockedException : AdactException
{
    /// <summary>
    /// Creates a new blocked-operation exception.
    /// </summary>
    /// <summary>
    /// Creates a new blocked-operation exception.
    /// </summary>
    public OperationBlockedException(string reason, Exception innerException)
        : base($"operation blocked: {reason}", innerException)
    {
    }
}
