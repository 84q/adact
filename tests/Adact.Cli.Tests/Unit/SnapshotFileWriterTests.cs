using Adact.Cli.Snapshots;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="SnapshotFileWriter"/> の出力パス・ファイル名・エンコーディング (UTF-8 BOM なし) を検証する Unit テスト。
/// snapshot.md の出力ファイル仕様 (設計 009) の回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
public class SnapshotFileWriterTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _origCwd;

    /// <summary>テスト用一時ディレクトリを作成し、そこを cwd にする。</summary>
    public SnapshotFileWriterTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "adact-snap-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _origCwd = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _tempRoot;
    }

    /// <summary>cwd を復元し、一時ディレクトリを再帰削除する。</summary>
    public void Dispose()
    {
        Environment.CurrentDirectory = _origCwd;
        try { Directory.Delete(_tempRoot, recursive: true); } catch { }
        GC.SuppressFinalize(this);
    }

    /// <summary>dir 未指定時は .adact/session-{sid}-{timestamp}.txt となり、gen- 接頭辞が入らず / 区切りになることを確認する。</summary>
    [Fact]
    public void Write_DefaultsToAdactDir_ProducesFile()
    {
        var text = "---\nsessionId: s1\n---\n- Window\n";
        var path = SnapshotFileWriter.Write(text, sid: 1);

        Assert.StartsWith(".adact/session-1-", path);
        Assert.DoesNotContain("gen-", path, StringComparison.Ordinal);
        Assert.EndsWith(".txt", path);
        Assert.DoesNotContain('\\', path);

        var fullPath = Path.Combine(_tempRoot, path.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(fullPath));
        var content = File.ReadAllText(fullPath);
        Assert.Equal(text, content);
    }

    /// <summary>dir オーバーライド指定時はそのディレクトリ下に出力されることを確認する。</summary>
    [Fact]
    public void Write_DirOverride_UsesGivenDirectory()
    {
        var text = "---\n- A\n";
        var customDir = Path.Combine(_tempRoot, "out");
        var path = SnapshotFileWriter.Write(text, sid: 2, dir: customDir);

        Assert.Contains("session-2-", path, StringComparison.Ordinal);
        Assert.EndsWith(".txt", path);
        Assert.DoesNotContain("gen-", path, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(customDir,
            Path.GetFileName(path.Replace('/', Path.DirectorySeparatorChar)))));
    }

    /// <summary>BOM なし UTF-8 で出力され (エコシステム互換性)、日本語文字列もそのまま保存されることを確認する。</summary>
    [Fact]
    public void Write_WritesUtf8WithoutBom()
    {
        var text = "- Window \"電卓\"\n";
        var path = SnapshotFileWriter.Write(text, sid: 1);
        var bytes = File.ReadAllBytes(Path.Combine(_tempRoot, path.Replace('/', Path.DirectorySeparatorChar)));

        // No UTF-8 BOM
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        Assert.Contains("電卓", System.Text.Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);
    }
}
