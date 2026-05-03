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
    /// <summary>
    /// 与えられた snapshot テキストを UTF-8 (BOM なし) でファイルに書き出す。
    /// 前回の snapshot と内容が同一の場合は新規ファイルを作成せず、前回のファイルパスを返す。
    /// </summary>
    /// <param name="snapshotText">書き出す snapshot テキスト本体。</param>
    /// <param name="sid">セッション番号 (例: <c>s1</c> の <c>1</c>)。ファイル名生成に利用する。</param>
    /// <param name="dir">保存先ディレクトリ。null/空なら <c>.adact</c>。</param>
    /// <returns>
    /// path: CWD からの相対パス (slash 区切り)。stdout 出力用。
    /// isNew: 新規作成された場合は <see langword="true"/>、前回と同一で再利用された場合は <see langword="false"/>。
    /// </returns>
    public static (string path, bool isNew) Write(string snapshotText, int sid, string? dir = null)
    {
        ArgumentNullException.ThrowIfNull(snapshotText);

        var targetDir = string.IsNullOrEmpty(dir) ? ".adact" : dir;
        Directory.CreateDirectory(targetDir);

        // Deduplication: check if the most recent snapshot for this session is identical
        var pattern = $"session-{sid}-*.txt";
        var existingFiles = Directory.GetFiles(targetDir, pattern);
        if (existingFiles.Length > 0)
        {
            var mostRecent = existingFiles
                .Select(f => new FileInfo(f))
                .OrderByDescending(fi => fi.LastWriteTimeUtc)
                .ThenByDescending(fi => fi.Name)
                .First();

            var lastContent = File.ReadAllText(mostRecent.FullName);
            if (NormalizeForComparison(lastContent) == NormalizeForComparison(snapshotText))
            {
                var rel = Path.GetRelativePath(Environment.CurrentDirectory, mostRecent.FullName);
                return (rel.Replace('\\', '/'), false);
            }
        }

        // Create new file
        var ts = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff", CultureInfo.InvariantCulture);
        var filename = $"session-{sid}-{ts}.txt";
        var path = Path.Combine(targetDir, filename);
        File.WriteAllText(path, snapshotText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var relPath = Path.GetRelativePath(Environment.CurrentDirectory, path);
        return (relPath.Replace('\\', '/'), true);
    }

    /// <summary>
    /// snapshot テキストから比較用の正規化表現を取得する。
    /// フロントマター内の <c>generatedAt</c> などメタデータタイムスタンプを無視し、
    /// UIA ツリー本体の同一性のみを比較する。
    /// </summary>
    private static string NormalizeForComparison(string text)
    {
        // Split on "\n---\n" to separate front matter from tree content.
        // The snapshot format is: "---\n<front matter>\n---\n<tree content>".
        var parts = text.Split(["\n---\n"], 2, StringSplitOptions.None);
        return parts.Length == 2 ? parts[1] : text;
    }
}
