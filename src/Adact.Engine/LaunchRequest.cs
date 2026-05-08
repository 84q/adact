namespace Adact.Engine;

/// <summary>
/// Describes a process launch request.
/// </summary>
public sealed record LaunchRequest(
    string Executable,
    IReadOnlyList<string>? Arguments = null,
    string? WorkingDirectory = null,
    IReadOnlyDictionary<string, string>? Environment = null);
