namespace Adact.Engine;

/// <summary>ClickAsync のオプション。Phase 2 では将来拡張用の置き場のみ。</summary>
/// <param name="Double">true の場合はダブルクリックを意図する (現状は実装で未使用、将来予約)。</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1720:Identifier contains type name",
    Justification = "Keep the original public API until ClickOptions gains real option fields.")]
public sealed record ClickOptions(
    bool Double = false);
