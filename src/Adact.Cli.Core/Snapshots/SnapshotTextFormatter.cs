using System.Globalization;
using System.Text;

namespace Adact.Cli.Snapshots;

/// <summary>
///
/// <code>
/// ---
/// filter: operable
/// sessionId: s1
/// processName: notepad
/// processId: 1234
/// generatedAt: "2025-01-01T00:00:00Z"
/// ---
///   - MenuBar [ref=s1e2]
///   - Edit [aid="15.Edit"] [focused] [ref=s1e7]
/// </code>
///
/// </summary>
internal static class SnapshotTextFormatter
{
    /// <summary>
    /// </summary>
    public static string Format(SnapshotMeta meta, SnapshotElement root, string filter)
    {
        ArgumentNullException.ThrowIfNull(meta);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(filter);

        var sb = new StringBuilder();
        WriteFrontmatter(sb, meta, filter);
        WriteElement(sb, root, depth: 0);
        return sb.ToString();
    }

    private static void WriteFrontmatter(StringBuilder sb, SnapshotMeta meta, string filter)
    {
        sb.Append("---\n");
        sb.Append("filter: ").Append(FormatYamlScalar(filter)).Append('\n');
        sb.Append("sessionId: ").Append(FormatYamlScalar(meta.SessionId)).Append('\n');
        if (!string.IsNullOrEmpty(meta.ProcessName))
        {
            sb.Append("processName: ").Append(FormatYamlScalar(meta.ProcessName)).Append('\n');
        }
        if (meta.ProcessId is { } pid)
        {
            sb.Append("processId: ").Append(pid.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }
        if (!string.IsNullOrEmpty(meta.GeneratedAt))
        {
            sb.Append("generatedAt: ").Append(FormatYamlScalar(meta.GeneratedAt)).Append('\n');
        }
        sb.Append("---\n");
    }

    /// <summary>
    /// </summary>
    private static string FormatYamlScalar(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (IsSafeBareScalar(value)) return value;
        return "\"" + EscapeYamlDoubleQuoted(value) + "\"";
    }

    private static bool IsSafeBareScalar(string s)
    {
        foreach (var ch in s)
        {
            var ok = (ch >= 'A' && ch <= 'Z')
                  || (ch >= 'a' && ch <= 'z')
                  || (ch >= '0' && ch <= '9')
                  || ch == ' ' || ch == '_' || ch == '-';
            if (!ok) return false;
        }
        return true;
    }

    private static string EscapeYamlDoubleQuoted(string s)
    {
        var sb = new StringBuilder(s.Length + 8);
        foreach (var ch in s)
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

    private static void WriteElement(StringBuilder sb, SnapshotElement el, int depth)
    {
        sb.Append(' ', depth * 2);
        sb.Append("- ");
        sb.Append(string.IsNullOrEmpty(el.Role) ? "(unknown)" : el.Role);

        var quotedName = SnapshotTextEscaper.Quote(el.Name);
        if (quotedName is not null)
        {
            sb.Append(' ').Append(quotedName);
        }

        var quotedAid = SnapshotTextEscaper.Quote(el.AutomationId);
        if (quotedAid is not null)
        {
            sb.Append(" [aid=").Append(quotedAid).Append(']');
        }

        var quotedValue = SnapshotTextEscaper.Quote(el.Value);
        if (quotedValue is not null)
        {
            sb.Append(" [value=").Append(quotedValue).Append(']');
        }

        if (!el.IsEnabled)
        {
            sb.Append(" [disabled]");
        }
        if (el.HasKeyboardFocus)
        {
            sb.Append(" [focused]");
        }
        if (el.IsSelected)
        {
            sb.Append(" [selected]");
        }
        if (el.IsModalDialog)
        {
            sb.Append(" [modal]");
        }

        if (!string.IsNullOrEmpty(el.Ref))
        {
            sb.Append(" [ref=").Append(el.Ref).Append(']');
        }

        sb.Append('\n');

        foreach (var c in el.Children)
        {
            WriteElement(sb, c, depth + 1);
        }
    }
}
