using System.Globalization;
using System.Text;

namespace Adact.Cli.Snapshots;

/// <summary>
///
/// <list type="bullet">
///   <item><c>"</c> → <c>\"</c></item>
///   <item><c>\</c> → <c>\\</c></item>
/// </list>
/// </summary>
internal static class SnapshotTextEscaper
{
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

    public static string? Quote(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return "\"" + Escape(value) + "\"";
    }
}
