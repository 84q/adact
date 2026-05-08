namespace Adact.Engine;

/// <summary>
/// Represents a scroll operation mode.
/// </summary>
public abstract record ScrollMode;

/// <summary>
/// Scrolls to a percentage position.
/// </summary>
public sealed record PercentMode(int? PercentH, int? PercentV) : ScrollMode;

/// <summary>
/// Scrolls by small increments.
/// </summary>
public sealed record SmallMode(int? DeltaH, int? DeltaV) : ScrollMode;

/// <summary>
/// Scrolls by large increments.
/// </summary>
public sealed record LargeMode(int? DeltaH, int? DeltaV) : ScrollMode;
