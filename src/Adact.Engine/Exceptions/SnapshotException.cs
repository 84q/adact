namespace Adact.Engine.Exceptions;

public sealed class SnapshotException : AdactException
{
    public SnapshotException(string message) : base(message) { }
    public SnapshotException(string message, Exception inner) : base(message, inner) { }
}
