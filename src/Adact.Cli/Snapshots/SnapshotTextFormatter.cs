using System.Globalization;
using System.Text;

namespace Adact.Cli.Snapshots;

/// <summary>
/// snapshot 中間ツリーを Phase 7 の Playwright 風テキスト形式に整形する。設計 016 §2.5。
///
/// 出力形式:
/// <code>
/// ---
/// filter: operable
/// sessionId: s1
/// processName: notepad
/// processId: 1234
/// generatedAt: "2025-01-01T00:00:00Z"
/// ---
/// - Window "メモ帳" [ref=s1e1]
///   - MenuBar [ref=s1e2]
///     - MenuItem "ファイル" [ref=s1e3]
///   - Edit [aid="15.Edit"] [focused] [ref=s1e7]
/// </code>
///
/// 属性順は <c>aid → value → state-flags(disabled/focused/modal) → ref</c>。
/// className / helpText / boundingRect / isKeyboardFocusable / isOffscreen は出力しない。
/// </summary>
internal static class SnapshotTextFormatter
{
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
  /// frontmatter 用の YAML スカラ整形。設計 016 §3.3:
  /// 英数字・スペース・<c>_</c>・<c>-</c> のみで構成される非空文字列は裸出力、
  /// それ以外 (日本語、<c>:</c> 等の YAML メタ文字、空文字列を含む) は
  /// ダブルクォートで囲む。クォート時の内部エスケープは制御文字・タブ・改行・
  /// バックスラッシュ・ダブルクォートのみで、それ以外 (日本語等) は素通し。
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
