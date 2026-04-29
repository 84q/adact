namespace Adact.Engine;

/// <summary>ClickAsync のオプション。Phase 2 では将来拡張用の置き場のみ。</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1720:Identifier contains type name",
    Justification = "Keep the original public API until ClickOptions gains real option fields.")]
public sealed record ClickOptions(
    bool Double = false);
