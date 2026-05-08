namespace Adact.Engine;

/// <summary>
/// Selection modes used by container selection.
/// </summary>
public enum SelectionMode
{
    /// <summary>
    /// Replaces the current selection.
    /// </summary>
    Replace,

    /// <summary>
    /// Adds to the current selection.
    /// </summary>
    Add,

    /// <summary>
    /// Removes from the current selection.
    /// </summary>
    Remove,
}
