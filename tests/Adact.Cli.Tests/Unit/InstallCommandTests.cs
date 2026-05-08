using Adact.Cli.Commands;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Install Command behavior.</summary>
[Trait("Layer", "Unit")]
public class InstallCommandTests
{
    private static readonly IReadOnlySet<string> ExpectedDocumentedCommands =
      new HashSet<string>(System.StringComparer.Ordinal)
      {
    "list-windows", "attach", "snapshot", "click", "fill",
    "doubleclick", "hover", "mousemove", "mousedown", "mouseup", "mousewheel",
    "keypress", "keydown", "keyup", "type",
    "check", "uncheck", "select", "focus", "scroll-into-view", "scroll",
    "resize-window", "minimize-window", "maximize-window", "restore-window",
    "inspect", "screenshot",
    "launch",
    "wait-for-element", "wait-for-window",
    "detach", "close-window", "kill",
      };

    private static readonly IReadOnlySet<string> ExpectedReferenceFiles =
      new HashSet<string>(System.StringComparer.Ordinal)
      {
    "session-bootstrap",
    "snapshots-and-inspection",
    "element-actions",
    "mouse-and-keyboard",
    "selection-and-state",
    "window-and-lifecycle",
    "error-codes",
    "popup-and-modal",
    "file-dialog",
      };

    /// <summary>Performs the Reference Files Match Expected Grouped Set operation.</summary>
    [Fact]
    public void ReferenceFiles_MatchExpectedGroupedSet()
    {
        var refDir = Path.Combine(InstallCommand.ResolveSourceDirectory(), "references");
        var actual = Directory.EnumerateFiles(refDir, "*.md")
          .Select(Path.GetFileNameWithoutExtension)
          .ToHashSet(System.StringComparer.Ordinal);

        Assert.Equal(ExpectedReferenceFiles.OrderBy(x => x), actual!.OrderBy(x => x));
    }

    /// <summary>Performs the Expected Documented Commands Are All Registered Subcommands operation.</summary>
    [Fact]
    public void ExpectedDocumentedCommands_AreAllRegisteredSubcommands()
    {
        var registered = Program.BuildRoot().Subcommands
          .Select(c => c.Name)
          .ToHashSet(System.StringComparer.Ordinal);

        foreach (var name in ExpectedDocumentedCommands)
        {
            Assert.Contains(name, registered);
        }
    }

    /// <summary>Resolves the Resolve Target Directory Matches Design Matrix value.</summary>
    [Theory]
    [InlineData("copilot", false, ".github/skills")]
    [InlineData("claude", false, ".claude/skills")]
    [InlineData("codex", false, ".agents/skills")]
    [InlineData("copilot", true, ".copilot/skills")]
    [InlineData("claude", true, ".claude/skills")]
    [InlineData("codex", true, ".agents/skills")]
    public void ResolveTargetDirectory_MatchesDesignMatrix(string client, bool global, string expectedTail)
    {
        var cwd = Path.Combine(Path.GetTempPath(), "adact-cwd");
        var home = Path.Combine(Path.GetTempPath(), "adact-home");

        var resolved = InstallCommand.ResolveTargetDirectory(client, global, cwd, home);

        var basePath = global ? home : cwd;
        var expected = Path.GetFullPath(Path.Combine(basePath, expectedTail));
        Assert.Equal(expected, resolved);
    }

    /// <summary>Resolves the Resolve Target Directory Unknown Client Throws value.</summary>
    [Fact]
    public void ResolveTargetDirectory_UnknownClient_Throws()
    {
        Assert.Throws<System.ArgumentException>(() =>
          InstallCommand.ResolveTargetDirectory("vim", global: false, cwd: "C:\\", homeDir: "C:\\"));
    }

    /// <summary>Performs the Skill Names All Have Source Directories operation.</summary>
    [Fact]
    public void SkillNames_AllHaveSourceDirectories()
    {
        var root = InstallCommand.ResolveSourceRootDirectory();

        foreach (var skill in InstallCommand.SkillNames)
        {
            Assert.True(Directory.Exists(Path.Combine(root, skill)), $"missing Skill source: {skill}");
        }
    }
}
