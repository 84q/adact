using System.Threading;

using Adact.Cli.Snapshots;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Snapshot File Writer behavior.</summary>
[Trait("Layer", "Unit")]
public class SnapshotFileWriterTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _origCwd;

    /// <summary>Initializes a new instance of the Snapshot File Writer Tests class.</summary>
    public SnapshotFileWriterTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "adact-snap-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _origCwd = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _tempRoot;
    }

    /// <summary>Releases resources.</summary>
    public void Dispose()
    {
        Environment.CurrentDirectory = _origCwd;
        try { Directory.Delete(_tempRoot, recursive: true); } catch { }
        GC.SuppressFinalize(this);
    }

    /// <summary>Performs the Write Defaults To Adact Dir Produces File operation.</summary>
    [Fact]
    public void Write_DefaultsToAdactDir_ProducesFile()
    {
        var text = "---\nsessionId: s1\n---\n- Window\n";
        var (path, isNew) = SnapshotFileWriter.Write(text, sid: 1);

        Assert.True(isNew);
        Assert.StartsWith(".adact/session-1-", path);
        Assert.DoesNotContain("gen-", path, StringComparison.Ordinal);
        Assert.EndsWith(".txt", path);
        Assert.DoesNotContain('\\', path);

        var fullPath = Path.Combine(_tempRoot, path.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(fullPath));
        var content = File.ReadAllText(fullPath);
        Assert.Equal(text, content);
    }

    /// <summary>Performs the Write Dir Override Uses Given Directory operation.</summary>
    [Fact]
    public void Write_DirOverride_UsesGivenDirectory()
    {
        var text = "---\n- A\n";
        var customDir = Path.Combine(_tempRoot, "out");
        var (path, isNew) = SnapshotFileWriter.Write(text, sid: 2, dir: customDir);

        Assert.True(isNew);
        Assert.Contains("session-2-", path, StringComparison.Ordinal);
        Assert.EndsWith(".txt", path);
        Assert.DoesNotContain("gen-", path, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(customDir,
            Path.GetFileName(path.Replace('/', Path.DirectorySeparatorChar)))));
    }

    /// <summary>Performs the Write Writes Utf8 Without Bom operation.</summary>
    [Fact]
    public void Write_WritesUtf8WithoutBom()
    {
        var text = "- Window \"Calculator\"\n";
        var (path, isNew) = SnapshotFileWriter.Write(text, sid: 1);
        Assert.True(isNew);
        var bytes = File.ReadAllBytes(Path.Combine(_tempRoot, path.Replace('/', Path.DirectorySeparatorChar)));

        // No UTF-8 BOM
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        Assert.Contains("Calculator", System.Text.Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);
    }

    /// <summary>Performs the Write Same Content Twice Reuses Existing File operation.</summary>
    [Fact]
    public void Write_SameContentTwice_ReusesExistingFile()
    {
        var text = "---\nsessionId: s1\n---\n- Window\n";
        var (firstPath, firstIsNew) = SnapshotFileWriter.Write(text, sid: 1);
        Assert.True(firstIsNew);

        var (secondPath, secondIsNew) = SnapshotFileWriter.Write(text, sid: 1);
        Assert.False(secondIsNew);
        Assert.Equal(firstPath, secondPath);

        // Only one file should exist
        var files = Directory.GetFiles(Path.Combine(_tempRoot, ".adact"), "session-1-*.txt");
        Assert.Single(files);
    }

    /// <summary>Performs the Write Different Content Creates New File operation.</summary>
    [Fact]
    public void Write_DifferentContent_CreatesNewFile()
    {
        var text1 = "---\nsessionId: s1\n---\n- Window\n";
        var (firstPath, firstIsNew) = SnapshotFileWriter.Write(text1, sid: 1);
        Assert.True(firstIsNew);

        // Ensure a different timestamp to avoid filename collision
        Thread.Sleep(2);

        var text2 = "---\nsessionId: s1\n---\n- Button\n";
        var (secondPath, secondIsNew) = SnapshotFileWriter.Write(text2, sid: 1);
        Assert.True(secondIsNew);
        Assert.NotEqual(firstPath, secondPath);

        var files = Directory.GetFiles(Path.Combine(_tempRoot, ".adact"), "session-1-*.txt");
        Assert.Equal(2, files.Length);
    }
}
