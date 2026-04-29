using System.Globalization;
using System.Text;

namespace Adact.Cli.Snapshots;

/// <summary>
/// snapshot テキスト形式 (Phase 7) の name / aid / value 用エスケープヘルパ。設計 016 §2.5。
///
/// ルール:
/// <list type="bullet">
///   <item><c>"</c> → <c>\"</c></item>
///   <item><c>\</c> → <c>\\</c></item>
///   <item>改行 (LF) → <c>\n</c></item>
///   <item>タブ → <c>\t</c></item>
///   <item>その他の制御文字 (U+0000..U+001F、U+007F) → <c>\uXXXX</c></item>
///   <item>それ以外 (日本語含む通常文字) → 生のまま</item>
/// </list>
/// CR (U+000D) もその他の制御文字として <c>\u000D</c> 扱いとする (LF/TAB のみ短縮表記対象)。
/// </summary>
internal static class SnapshotTextEscaper
{
    /// <summary>name / aid / value を表示用に escape する。<c>null</c> はそのまま <c>null</c>。</summary>
    /// <param name="value">エスケープ対象文字列。</param>
    /// <returns>エスケープ済み文字列。<paramref name="value"/> が null なら null。</returns>
    public static string? Escape(string? value)
    {
        if (value is null) return null;
        if (value.Length == 0) return string.Empty;

        var sb = new StringBuilder(value.Length + 8);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (ch < 0x20 || ch == 0x7F)
                    {
                        sb.Append("\\u")
                         .Append(((int)ch).ToString("X4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                    break;
            }
        }
        return sb.ToString();
    }

    /// <summary>name / aid / value を <c>"..."</c> で囲んだ表示形に整形する。<c>null</c>/空は <c>null</c>。</summary>
    /// <param name="value">クオート対象文字列。</param>
    /// <returns><c>"escaped"</c> 形式の文字列、または <paramref name="value"/> が null/空の場合は null。</returns>
    public static string? Quote(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return "\"" + Escape(value) + "\"";
    }
}
