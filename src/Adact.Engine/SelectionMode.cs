namespace Adact.Engine;

/// <summary>
/// 複数選択時のモードを表す。
/// </summary>
public enum SelectionMode
{
    /// <summary>最初のアイテムで Select()、2番目以降で AddToSelection()。既存選択は解除される。</summary>
    Replace,

    /// <summary>全アイテムで AddToSelection()。既存選択を維持したまま追加。</summary>
    Add,

    /// <summary>全アイテムで RemoveFromSelection()。既存選択から除外。</summary>
    Remove,
}
