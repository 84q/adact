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
    [InlineData("dblclick")]
    [InlineData("hover")]
    [InlineData("type")]
    [InlineData("check")]
    [InlineData("uncheck")]
    [InlineData("select")]
    [InlineData("clear")]
    [InlineData("resize")]
    [InlineData("minimize")]
    [InlineData("maximize")]
    [InlineData("restore")]
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
    [InlineData("mouse-move")]
    [InlineData("mouse-down")]
    [InlineData("mouse-up")]
    [InlineData("mouse-wheel")]
    [InlineData("press")]
    [InlineData("key-down")]
    [InlineData("key-up")]
    [InlineData("inspect")]
    [InlineData("screenshot")]
    [InlineData("wait-for")]
    [InlineData("wait-for-window")]
    public void NonAutoSnapshotCommands_DoNotHaveNoSnapshotOption(string commandName)
    {
        var cmd = BuildCommand(commandName);

        Assert.DoesNotContain(cmd.Options, o => o.Name == NoSnapshotOptionName);
    }

    private static Command BuildCommand(string commandName) => commandName switch
    {
        "click" => ClickCommand.Build(),
        "dblclick" => DblclickCommand.Build(),
        "hover" => HoverCommand.Build(),
        "type" => TypeCommand.Build(),
        "check" => CheckCommand.Build(),
        "uncheck" => UncheckCommand.Build(),
        "select" => SelectCommand.Build(),
        "clear" => ClearCommand.Build(),
        "press" => PressCommand.Build(),
        "mouse-wheel" => MouseWheelCommand.Build(),
        "resize" => ResizeCommand.Build(),
        "minimize" => MinimizeCommand.Build(),
        "maximize" => MaximizeCommand.Build(),
        "restore" => RestoreCommand.Build(),
        "focus" => FocusCommand.Build(),
        "scroll-into-view" => ScrollIntoViewCommand.Build(),
        "mouse-move" => MouseMoveCommand.Build(),
        "mouse-down" => MouseDownCommand.Build(),
        "mouse-up" => MouseUpCommand.Build(),
        "key-down" => KeyDownCommand.Build(),
        "key-up" => KeyUpCommand.Build(),
        "inspect" => InspectCommand.Build(),
        "screenshot" => ScreenshotCommand.Build(),
        "wait-for" => WaitForCommand.Build(),
        "wait-for-window" => WaitForWindowCommand.Build(),
        _ => throw new ArgumentOutOfRangeException(nameof(commandName), commandName, "Unknown command."),
    };
}
