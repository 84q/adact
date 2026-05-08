namespace Adact.Engine;

/// <summary>
/// Represents a target used when selecting child items.
/// </summary>
public abstract record SelectionTarget
{
    private protected SelectionTarget() { }

    /// <summary>
    /// Selects a child by name.
    /// </summary>
    public sealed record ByName(string Name) : SelectionTarget;

    /// <summary>
    /// Selects a child by zero-based index.
    /// </summary>
    public sealed record ByIndex(int Index) : SelectionTarget;

    /// <summary>
    /// Selects a child by element ref.
    /// </summary>
    public sealed record ByItemRef(string ItemRef) : SelectionTarget;

    /// <summary>
    /// Creates a name-based selection target.
    /// </summary>
    public static SelectionTarget FromName(string name) => new ByName(name);

    /// <summary>
    /// Creates an index-based selection target.
    /// </summary>
    public static SelectionTarget FromIndex(int index) => new ByIndex(index);

    /// <summary>
    /// Creates a ref-based selection target.
    /// </summary>
    public static SelectionTarget FromItemRef(string itemRef) => new ByItemRef(itemRef);
}
