using System.Text.Json;

using Adact.Cli.Snapshots;

using Xunit;

namespace Adact.Cli.Tests.Unit;

[Trait("Layer", "Unit")]
public class SnapshotFileWriterTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _origCwd;

    public SnapshotFileWriterTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "adact-snap-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _origCwd = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _tempRoot;
    }

    public void Dispose()
    {
        Environment.CurrentDirectory = _origCwd;
        try { Directory.Delete(_tempRoot, recursive: true); } catch { }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Write_DefaultsToAdactDir_ProducesFile()
    {
        var json = """{"_meta":{"sessionId":"s1","generation":1}}""";
        var path = SnapshotFileWriter.Write(json, sid: 1, generation: 1);

        Assert.StartsWith(".adact/session-1-gen-1-", path);
        Assert.EndsWith(".json", path);
        Assert.DoesNotContain('\\', path);

        var fullPath = Path.Combine(_tempRoot, path.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(fullPath));
        var content = File.ReadAllText(fullPath);
        Assert.Equal(json, content);
    }

    [Fact]
    public void Write_DirOverride_UsesGivenDirectory()
    {
        var json = """{"x":1}""";
        var customDir = Path.Combine(_tempRoot, "out");
        var path = SnapshotFileWriter.Write(json, sid: 2, generation: 5, dir: customDir);

        Assert.Contains("session-2-gen-5-", path);
        Assert.True(File.Exists(Path.Combine(customDir,
            Path.GetFileName(path.Replace('/', Path.DirectorySeparatorChar)))));
    }

    [Fact]
    public void Write_WritesUtf8WithoutBom()
    {
        var json = """{"jp":"電卓"}""";
        var path = SnapshotFileWriter.Write(json, sid: 1, generation: 1);
        var bytes = File.ReadAllBytes(Path.Combine(_tempRoot, path.Replace('/', Path.DirectorySeparatorChar)));

        // No UTF-8 BOM
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);

        // Valid JSON parse round-trip
        using var doc = JsonDocument.Parse(bytes);
        Assert.Equal("電卓", doc.RootElement.GetProperty("jp").GetString());
    }
}
