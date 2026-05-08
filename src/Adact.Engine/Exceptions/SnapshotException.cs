namespace Adact.Engine.Exceptions;

/// <summary>
/// Thrown when snapshot generation fails.
/// </summary>
public sealed class SnapshotException : AdactException
{
    /// <summary>
    /// Creates a new snapshot exception.
    /// </summary>
    public SnapshotException(string message) : base(message) { }

    /// <summary>
    /// Creates a new snapshot exception with an inner exception.
    /// </summary>
    public SnapshotException(string message, Exception inner) : base(message, inner) { }
}
