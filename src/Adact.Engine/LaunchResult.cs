namespace Adact.Engine;

/// <summary>
/// Describes the result of launching a process.
/// </summary>
public sealed record LaunchResult(
    int Pid,
    string ProcessName,
    string? ExecutablePath);
