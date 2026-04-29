using System.Globalization;
using System.Text;

namespace Adact.Cli.Snapshots;

/// <summary>
/// snapshot テキストをファイルに書き出すヘルパ。設計 009 §4.4 / §5.2、011 §4.5、016 §2。
/// 既定の保存先は <c>.adact/</c>。ファイル名は <c>session-&lt;sid&gt;-&lt;UTC ts&gt;.txt</c>。
/// 戻り値は CWD からの相対パス (slash 区切り) で stdout 表示用。
/// </summary>
internal static class SnapshotFileWriter
{
    public static string Write(string snapshotText, int sid, string? dir = null)
    {
        ArgumentNullException.ThrowIfNull(snapshotText);

        var ts = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff", CultureInfo.InvariantCulture);
        var filename = $"session-{sid}-{ts}.txt";
        var targetDir = string.IsNullOrEmpty(dir) ? ".adact" : dir;
        Directory.CreateDirectory(targetDir);
        var path = Path.Combine(targetDir, filename);
        File.WriteAllText(path, snapshotText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var rel = Path.GetRelativePath(Environment.CurrentDirectory, path);
        return rel.Replace('\\', '/');
    }
}
