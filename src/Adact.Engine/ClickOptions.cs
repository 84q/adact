namespace Adact.Engine;

/// <summary>
/// <see cref="WindowSession.ClickAsync(string, ClickOptions?, System.Threading.CancellationToken)"/> の動作を制御するオプション。
/// 既定値 (引数省略時) では従来挙動 (左クリック 1 回、修飾キーなし、要素中央) を維持する。
/// </summary>
/// <param name="Double">true の場合は OS ダブルクリック判定内で 2 回クリックする (旧 API 互換、Phase 8 では <c>doubleclick</c> 推奨)。</param>
/// <param name="Button">押下するボタン。既定は <see cref="MouseButton.Left"/>。</param>
/// <param name="Count">汎用 N 連打回数 (1 以上)。OS ダブルクリック判定は保証しない。</param>
/// <param name="Modifiers">クリック中に押下したままにする修飾キー名 (Shift / Control / Alt / Meta / Win / Windows)。null / 空は修飾なし。</param>
/// <param name="PositionX">要素の bounding rect 左上を基準とする X オフセット (px)。null の場合は要素中央を使う。</param>
/// <param name="PositionY">要素の bounding rect 左上を基準とする Y オフセット (px)。null の場合は要素中央を使う。</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1720:Identifier contains type name",
    Justification = "Keep the original public API until ClickOptions gains real option fields.")]
public sealed record ClickOptions(
    bool Double = false,
    MouseButton Button = MouseButton.Left,
    int Count = 1,
    System.Collections.Generic.IReadOnlyList<string>? Modifiers = null,
    int? PositionX = null,
    int? PositionY = null);
