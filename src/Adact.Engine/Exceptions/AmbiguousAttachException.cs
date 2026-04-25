namespace Adact.Engine.Exceptions;

public sealed class AmbiguousAttachException : AdactException
{
    public AttachQuery Query { get; }
    public IReadOnlyList<WindowInfo> Candidates { get; }

    public AmbiguousAttachException(AttachQuery query, IReadOnlyList<WindowInfo> candidates)
        : base($"Multiple windows ({candidates.Count}) matched the attach query. Use ListWindowsAsync to disambiguate.")
    {
        Query = query;
        Candidates = candidates;
    }
}
