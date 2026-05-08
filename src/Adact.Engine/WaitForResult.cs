namespace Adact.Engine;

/// <summary>
/// Describes the result of a wait-for operation.
/// </summary>
public sealed record WaitForResult(string? Ref, WaitForState State);
