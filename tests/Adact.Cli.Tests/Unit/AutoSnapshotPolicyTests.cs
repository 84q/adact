using System.CommandLine;

using Adact.Cli.Commands;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// Phase 8 Step 8 補強: 設計書 <c>discussion/022_Phase8設計.md</c> §2 (auto-snapshot 発火ポリシー) で
/// 定義された各コマンドの <c>--no-snapshot</c> Option 有無が、System.CommandLine の
/// <see cref="Command"/> ビルダの実装と一致しているかを検証する回帰テスト。
/// </summary>
[Trait("Layer", "Unit")]
public class AutoSnapshotPolicyTests
{
    private const string NoSnapshotOptionName = "--no-snapshot";

    /// <summary>
    /// 設計書で「auto-snapshot あり」分類とされているコマンドは、
    /// Build() で生成された <see cref="Command"/> に <c>--no-snapshot</c> Option を持たねばならない。
    /// </summary>
    /// <param name="commandName">CLI サブコマンド名。</param>
    [Theory]
    [InlineData("click")]
    [InlineData("doubleclick")]
    [InlineData("hover")]
    [InlineData("type")]
    [InlineData("check")]
    [InlineData("uncheck")]
    [InlineData("select")]
    [InlineData("resize-window")]
    [InlineData("minimize-window")]
    [InlineData("maximize-window")]
    [InlineData("restore-window")]
    public void AutoSnapshotCommands_HaveNoSnapshotOption(string commandName)
    {
        var cmd = BuildCommand(commandName);

        Assert.Contains(cmd.Options, o => o.Name == NoSnapshotOptionName);
    }

    /// <summary>
    /// 設計書で「auto-snapshot なし (補助 / 取得・同期)」分類とされているコマンドは、
    /// Build() で生成された <see cref="Command"/> に <c>--no-snapshot</c> Option を持ってはならない。
    /// </summary>
    /// <param name="commandName">CLI サブコマンド名。</param>
    [Theory]
    [InlineData("focus")]
    [InlineData("scroll-into-view")]
    [InlineData("scroll")]
    [InlineData("mousemove")]
    [InlineData("mousedown")]
    [InlineData("mouseup")]
    [InlineData("mousewheel")]
    [InlineData("keypress")]
    [InlineData("keydown")]
    [InlineData("keyup")]
    [InlineData("inspect")]
    [InlineData("screenshot")]
    [InlineData("wait-for-element")]
    [InlineData("wait-for-window")]
    public void NonAutoSnapshotCommands_DoNotHaveNoSnapshotOption(string commandName)
    {
        var cmd = BuildCommand(commandName);

        Assert.DoesNotContain(cmd.Options, o => o.Name == NoSnapshotOptionName);
    }

    private static Command BuildCommand(string commandName) => commandName switch
    {
        "click" => ClickCommand.Build(),
        "doubleclick" => DoubleclickCommand.Build(),
        "hover" => HoverCommand.Build(),
        "type" => TypeCommand.Build(),
        "check" => CheckCommand.Build(),
        "uncheck" => UncheckCommand.Build(),
        "select" => SelectCommand.Build(),
        "keypress" => KeypressCommand.Build(),
        "mousewheel" => MousewheelCommand.Build(),
        "resize-window" => ResizeWindowCommand.Build(),
        "minimize-window" => MinimizeWindowCommand.Build(),
        "maximize-window" => MaximizeWindowCommand.Build(),
        "restore-window" => RestoreWindowCommand.Build(),
        "focus" => FocusCommand.Build(),
        "scroll-into-view" => ScrollIntoViewCommand.Build(),
        "scroll" => ScrollCommand.Build(),
        "mousemove" => MousemoveCommand.Build(),
        "mousedown" => MousedownCommand.Build(),
        "mouseup" => MouseupCommand.Build(),
        "keydown" => KeydownCommand.Build(),
        "keyup" => KeyupCommand.Build(),
        "inspect" => InspectCommand.Build(),
        "screenshot" => ScreenshotCommand.Build(),
        "wait-for-element" => WaitForElementCommand.Build(),
        "wait-for-window" => WaitForWindowCommand.Build(),
        _ => throw new ArgumentOutOfRangeException(nameof(commandName), commandName, "Unknown command."),
    };
}
