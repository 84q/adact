using Adact.Cli.Commands;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// 設計 013 §5.2 (Skill 同期):
/// grouped reference docs のファイル集合と、Skill 対象 CLI サブコマンド集合の整合を検証する。
/// </summary>
[Trait("Layer", "Unit")]
public class InstallCommandTests
{
    /// <summary>
    /// Skill が説明対象とする CLI サブコマンド集合。
    /// </summary>
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
      };

    /// <summary>
    /// grouped reference docs のファイル名集合が想定セットと一致することを確認する。
    /// </summary>
    [Fact]
    public void ReferenceFiles_MatchExpectedGroupedSet()
    {
        var refDir = Path.Combine(InstallCommand.ResolveSourceDirectory(), "references");
        var actual = Directory.EnumerateFiles(refDir, "*.md")
          .Select(Path.GetFileNameWithoutExtension)
          .ToHashSet(System.StringComparer.Ordinal);

        Assert.Equal(ExpectedReferenceFiles.OrderBy(x => x), actual!.OrderBy(x => x));
    }

    /// <summary>
    /// Skill 対象として期待されるコマンド名が、全て実際に CLI に登録されていることを確認する。
    /// Skill だけ残って CLI から消えたという逆位相ケースの検出。
    /// </summary>
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

    /// <summary>
    /// client × global の組み合わせで ResolveTargetDirectory が設計 013 §5.1 のマトリクスと一致することを確認する。
    /// install 出力先の判定ロジックの回帰防止。
    /// </summary>
    /// <param name="client">クライアント名。</param>
    /// <param name="global">--global フラグ。</param>
    /// <param name="expectedTail">期待される相対パス。</param>
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

    /// <summary>未知クライアント名を渡したとき ArgumentException が伝播されることを確認する。</summary>
    [Fact]
    public void ResolveTargetDirectory_UnknownClient_Throws()
    {
        Assert.Throws<System.ArgumentException>(() =>
          InstallCommand.ResolveTargetDirectory("vim", global: false, cwd: "C:\\", homeDir: "C:\\"));
    }

    /// <summary>install 対象 Skill のソースディレクトリが揃っていることを確認する。</summary>
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
