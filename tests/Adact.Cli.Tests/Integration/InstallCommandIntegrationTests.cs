using Xunit;

namespace Adact.Cli.Tests.Integration;

/// <summary>
/// 設計 013 §5.1: <c>adact install --skills &lt;client&gt; [--global]</c> の Integration テスト。
/// 3 client × {cwd, --global} = 6 ケース + 上書き 1 ケース。
///
/// 実 adact.exe をテンポラリ cwd で起動し、期待パスにファイルが展開されることを検証する。
/// <c>--global</c> 版は USERPROFILE 環境変数を差し替えてホーム領域のテンポラリ化を行う
/// (設計 013 §5.1 「環境変数等で `~` を差し替え可能な設計」)。
/// </summary>
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

    /// <summary>
    /// cwd モードで install した際、クライアント別の期待パスに SKILL ファイル一式が展開されることを確認する。
    /// 設計 013 §5.1 のクライアント対応マトリクスの回帰防止。
    /// </summary>
    /// <param name="client">クライアント名 (copilot / claude / codex)。</param>
    /// <param name="relativeTail">cwd からの期待サブパス。</param>
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

    /// <summary>
    /// --global モードで install した際、USERPROFILE オーバーライド先に SKILL ファイルが展開され、
    /// cwd には全く何も書き込まれないことを確認する。
    /// USERPROFILE オーバーライドが効かず本物のホームへ出てしまう事故を防ぐため。
    /// </summary>
    /// <param name="client">クライアント名。</param>
    /// <param name="relativeTail">USERPROFILE からの期待サブパス。</param>
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

        // cwd 側には何も書き込まれないこと。
        // This is the last line of defence verifying that the USERPROFILE override
        // actually took effect: if the install command had ignored USERPROFILE and
        // fallen back to the real home directory (or written into cwd by mistake),
        // these directories would appear under the temporary cwd.
        Assert.False(Directory.Exists(Path.Combine(cwd.Path, ".github")));
        Assert.False(Directory.Exists(Path.Combine(cwd.Path, ".claude")));
        Assert.False(Directory.Exists(Path.Combine(cwd.Path, ".agents")));
    }

    /// <summary>
    /// 2 回目の install で既存ファイルが上書きされ、人為的に書き換えたプレースホルダーが消えることを確認する。
    /// install の idempotent 仕様 (古い SKILL を残さない) の回帰防止。
    /// </summary>
    [Fact]
    public void Install_Twice_OverwritesExistingFiles()
    {
        using var temp = new TempDirectory();
        var targetDir = Path.Combine(temp.Path, ".github", "skills", "adact-cli");
        var skillFile = Path.Combine(targetDir, "SKILL.md");

        // 1 回目。
        var first = CliProcess.Run("install --skills copilot", workingDirectory: temp.Path);
        AssertSuccess(first);

        // 既存ファイルを書き換えて、2 回目で上書きされる (== 元に戻る) ことを確認する。
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

/// <summary>
/// テスト用の使い捨て一時ディレクトリ。Dispose で再帰削除する。
/// </summary>
internal sealed class TempDirectory : System.IDisposable
{
    /// <summary>一時ディレクトリの絶対パス。</summary>
    public string Path { get; }

    /// <summary>一時ディレクトリを GUID 付きで作成する。</summary>
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(
          System.IO.Path.GetTempPath(),
          "adact-test-" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>一時ディレクトリを再帰削除する。</summary>
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
            // テスト後始末の失敗はテスト結果に影響させない。
        }
    }
}
