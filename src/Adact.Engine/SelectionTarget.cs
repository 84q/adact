namespace Adact.Engine;

/// <summary>
/// 選択対象のアイテムを識別する判別型。Name / Index / ItemRef のいずれかで指定する。
/// </summary>
public abstract record SelectionTarget
{
    private protected SelectionTarget() { }

    /// <summary>Name で選択対象を指定する。</summary>
    public sealed record ByName(string Name) : SelectionTarget;

    /// <summary>0-based index で選択対象を指定する。</summary>
    public sealed record ByIndex(int Index) : SelectionTarget;

    /// <summary>snapshot の Element Ref で選択対象を指定する。</summary>
    public sealed record ByItemRef(string ItemRef) : SelectionTarget;

    /// <summary>Name で <see cref="SelectionTarget"/> を生成する。</summary>
    public static SelectionTarget FromName(string name) => new ByName(name);

    /// <summary>0-based index で <see cref="SelectionTarget"/> を生成する。</summary>
    public static SelectionTarget FromIndex(int index) => new ByIndex(index);

    /// <summary>Element Ref で <see cref="SelectionTarget"/> を生成する。</summary>
    public static SelectionTarget FromItemRef(string itemRef) => new ByItemRef(itemRef);
}
