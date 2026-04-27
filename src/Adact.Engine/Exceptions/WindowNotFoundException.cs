namespace Adact.Engine.Exceptions;

public sealed class WindowNotFoundException : AdactException
{
    public AttachQuery Query { get; }
    public WindowNotFoundException(AttachQuery query)
        : base($"No window matched the attach query: {Describe(query)}")
    {
        Query = query;
    }

    private static string Describe(AttachQuery q)
    {
        var parts = new List<string>();
        if (q.ProcessName is not null) parts.Add($"processName=\"{q.ProcessName}\"");
        if (q.WindowTitle is not null) parts.Add($"windowTitle=\"{q.WindowTitle}\"");
        if (q.ProcessId is not null) parts.Add($"pid={q.ProcessId}");
        return parts.Count == 0 ? "(empty)" : string.Join(", ", parts);
    }
}
