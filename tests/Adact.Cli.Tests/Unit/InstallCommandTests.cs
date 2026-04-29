using Adact.Cli.Commands;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// 設計 013 §5.2 (コマンド名同期):
/// <c>references/*.md</c> のファイル名集合と Skill 対象 CLI サブコマンド名集合が一致することを検証する。
/// 不一致 → サブコマンド追加・削除・改名時に Skill 更新漏れを検知する。
/// </summary>
[Trait("Layer", "Unit")]
public class InstallCommandTests
{
    /// <summary>
    /// 設計 013 §3.1 / 012 §3.1: Skill が説明対象とする CLI サブコマンド (5 コマンド)。
    /// 新たな CLI サブコマンドを Skill 化する場合は、ここと <c>references/*.md</c> の双方を更新する。
    /// </summary>
    private static readonly IReadOnlySet<string> ExpectedDocumentedCommands =
      new HashSet<string>(System.StringComparer.Ordinal)
      {
    "list-apps", "attach", "snapshot", "click", "fill",
    "dblclick", "hover", "mouse-move", "mouse-down", "mouse-up", "mouse-wheel",
    "press", "key-down", "key-up", "type",
    "check", "uncheck", "select", "focus", "clear", "scroll-into-view",
      };

    /// <summary>
    /// references/*.md のファイル名集合と、Skill 対象の期待 CLI コマンド集合が一致することを確認する。
    /// CLI コマンド追加・削除・改名時の Skill ドキュメント更新漏れ検出。
    /// </summary>
    [Fact]
    public void ReferenceFiles_MatchExpectedDocumentedSet()
    {
        var refDir = Path.Combine(InstallCommand.ResolveSourceDirectory(), "references");
        var actual = Directory.EnumerateFiles(refDir, "*.md")
          .Select(Path.GetFileNameWithoutExtension)
          .ToHashSet(System.StringComparer.Ordinal);

        Assert.Equal(ExpectedDocumentedCommands.OrderBy(x => x), actual!.OrderBy(x => x));
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
    [InlineData("copilot", false, ".github/skills/adact-cli")]
    [InlineData("claude", false, ".claude/skills/adact-cli")]
    [InlineData("codex", false, ".agents/skills/adact-cli")]
    [InlineData("copilot", true, ".copilot/skills/adact-cli")]
    [InlineData("claude", true, ".claude/skills/adact-cli")]
    [InlineData("codex", true, ".agents/skills/adact-cli")]
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
}
