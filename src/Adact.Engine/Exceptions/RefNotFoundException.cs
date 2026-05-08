namespace Adact.Engine.Exceptions;

/// <summary>
/// Thrown when a ref cannot be resolved in the current snapshot.
/// </summary>
public sealed class RefNotFoundException : AdactException
{
    /// <summary>
    /// Gets the ref ID that failed to resolve.
    /// </summary>
    public string RefId { get; }

    /// <summary>
    /// Gets the reason the ref could not be resolved.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Creates a new ref-not-found exception.
    /// </summary>
    public RefNotFoundException(string refId, string? reason = null)
        : base($"Ref ID '{refId}' is not valid for this session{(reason is null ? "" : $": {reason}")}")
    {
        RefId = refId;
        Reason = reason;
    }
}
