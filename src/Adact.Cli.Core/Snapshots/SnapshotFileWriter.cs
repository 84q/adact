using System.Globalization;
using System.Text;

namespace Adact.Cli.Snapshots;

/// <summary>
/// </summary>
internal static class SnapshotFileWriter
{
    /// <summary>
    /// </summary>
    /// <returns>
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
    /// </summary>
    private static string NormalizeForComparison(string text)
    {
        // Split on "\n---\n" to separate front matter from tree content.
        // The snapshot format is: "---\n<front matter>\n---\n<tree content>".
        var parts = text.Split(["\n---\n"], 2, StringSplitOptions.None);
        return parts.Length == 2 ? parts[1] : text;
    }
}
