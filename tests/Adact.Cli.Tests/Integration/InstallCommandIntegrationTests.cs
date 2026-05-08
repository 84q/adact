using Xunit;

namespace Adact.Cli.Tests.Integration;

/// <summary>Contains tests for the Install Command Integration behavior.</summary>
[Trait("Layer", "Integration")]
public class InstallCommandIntegrationTests
{
    private static readonly IReadOnlyDictionary<string, string[]> ExpectedSkillFiles =
      new Dictionary<string, string[]>(StringComparer.Ordinal)
      {
          ["adact-cli"] =
          [
              "SKILL.md",
              "references/session-bootstrap.md",
              "references/snapshots-and-inspection.md",
              "references/element-actions.md",
              "references/mouse-and-keyboard.md",
              "references/selection-and-state.md",
              "references/window-and-lifecycle.md",
          ],
          ["adact-flaui-testgen"] =
          [
              "SKILL.md",
              "references/observation-template.md",
              "references/output-format.md",
              "references/flaui-xunit-guidelines.md",
              "references/pom-guidelines.md",
          ],
      };

    private static readonly string[] ExpectedAdactCliFiles =
    {
        "SKILL.md",
        "references/session-bootstrap.md",
        "references/snapshots-and-inspection.md",
        "references/element-actions.md",
        "references/mouse-and-keyboard.md",
        "references/selection-and-state.md",
        "references/window-and-lifecycle.md",
    };

    /// <summary>Performs the Install Cwd Writes Skill Files operation.</summary>
    [Theory]
    [InlineData("copilot", ".github/skills/adact-cli")]
    [InlineData("claude", ".claude/skills/adact-cli")]
    [InlineData("codex", ".agents/skills/adact-cli")]
    public void Install_Cwd_WritesSkillFiles(string client, string relativeTail)
    {
        using var temp = new TempDirectory();
        var result = CliProcess.Run(
          $"install --skills {client}",
          workingDirectory: temp.Path);

        AssertSuccess(result);
        var targetDir = Path.Combine(temp.Path, relativeTail.Replace('/', Path.DirectorySeparatorChar));
        AssertSkillFilesExist(targetDir);
        Assert.Contains("installed: true", result.Stdout);
    }

    /// <summary>Performs the Install Global Writes Skill Files operation.</summary>
    [Theory]
    [InlineData("copilot", ".copilot/skills/adact-cli")]
    [InlineData("claude", ".claude/skills/adact-cli")]
    [InlineData("codex", ".agents/skills/adact-cli")]
    public void Install_Global_WritesSkillFiles(string client, string relativeTail)
    {
        using var cwd = new TempDirectory();
        using var home = new TempDirectory();

        var env = new Dictionary<string, string?>
        {
            ["USERPROFILE"] = home.Path,
        };

        var result = CliProcess.Run(
          $"install --skills {client} --global",
          workingDirectory: cwd.Path,
          environment: env);

        AssertSuccess(result);
        var targetDir = Path.Combine(home.Path, relativeTail.Replace('/', Path.DirectorySeparatorChar));
        AssertSkillFilesExist(targetDir);
        Assert.Contains("installed: true", result.Stdout);

        // This is the last line of defence verifying that the USERPROFILE override
        // actually took effect: if the install command had ignored USERPROFILE and
        // fallen back to the real home directory (or written into cwd by mistake),
        // these directories would appear under the temporary cwd.
        Assert.False(Directory.Exists(Path.Combine(cwd.Path, ".github")));
        Assert.False(Directory.Exists(Path.Combine(cwd.Path, ".claude")));
        Assert.False(Directory.Exists(Path.Combine(cwd.Path, ".agents")));
    }

    /// <summary>Performs the Install Twice Overwrites Existing Files operation.</summary>
    [Fact]
    public void Install_Twice_OverwritesExistingFiles()
    {
        using var temp = new TempDirectory();
        var targetDir = Path.Combine(temp.Path, ".github", "skills", "adact-cli");
        var skillFile = Path.Combine(targetDir, "SKILL.md");

        var first = CliProcess.Run("install --skills copilot", workingDirectory: temp.Path);
        AssertSuccess(first);

        File.WriteAllText(skillFile, "STALE PLACEHOLDER\n");
        Assert.Equal("STALE PLACEHOLDER\n", File.ReadAllText(skillFile));

        var second = CliProcess.Run("install --skills copilot", workingDirectory: temp.Path);
        AssertSuccess(second);

        var content = File.ReadAllText(skillFile);
        Assert.DoesNotContain("STALE PLACEHOLDER", content);
        Assert.Contains("name: adact-cli", content);
    }

    private static void AssertSuccess(CliResult result)
    {
        Assert.True(result.ExitCode == 0,
          $"install exit={result.ExitCode}\nstdout: {result.Stdout}\nstderr: {result.Stderr}");
    }

    private static void AssertSkillFilesExist(string targetDir)
    {
        Assert.True(Directory.Exists(targetDir), $"target directory missing: {targetDir}");
        foreach (var rel in ExpectedAdactCliFiles)
        {
            var path = Path.Combine(targetDir, rel.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), $"expected Skill file missing: {path}");
        }

        var skillsRoot = Directory.GetParent(targetDir)?.FullName;
        Assert.NotNull(skillsRoot);
        foreach (var (skill, files) in ExpectedSkillFiles)
        {
            var skillDir = Path.Combine(skillsRoot, skill);
            Assert.True(Directory.Exists(skillDir), $"target Skill directory missing: {skillDir}");
            foreach (var rel in files)
            {
                var path = Path.Combine(skillDir, rel.Replace('/', Path.DirectorySeparatorChar));
                Assert.True(File.Exists(path), $"expected Skill file missing: {path}");
            }
        }
    }
}

internal sealed class TempDirectory : System.IDisposable
{
    /// <summary>Gets the Path value.</summary>
    public string Path { get; }

    /// <summary>Initializes a new instance of the Temp Directory class.</summary>
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(
          System.IO.Path.GetTempPath(),
          "adact-test-" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>Releases resources.</summary>
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch
        {
        }
    }
}
