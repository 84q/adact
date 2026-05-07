using System.CommandLine;

using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary><c>check</c> コマンド。Toggle/Selection 系要素を On 状態にする (auto-snapshot あり、idempotent)。</summary>
internal static class CheckCommand
{
    /// <summary>check サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build() => RefOnlyCommandBuilder.Build(
        name: "check",
        description: "Ensure a checkbox / toggle / radio is in the On state. Idempotent.",
        toolName: "adact_check",
        autoSnapshot: true);
}

/// <summary><c>uncheck</c> コマンド。Toggle 系要素を Off 状態にする (auto-snapshot あり、idempotent)。</summary>
internal static class UncheckCommand
{
    /// <summary>uncheck サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build() => RefOnlyCommandBuilder.Build(
        name: "uncheck",
        description: "Ensure a checkbox / toggle is in the Off state. Idempotent.",
        toolName: "adact_uncheck",
        autoSnapshot: true);
}

/// <summary><c>focus</c> コマンド。指定要素にキーボードフォーカスを当てる (低レベル)。</summary>
internal static class FocusCommand
{
    /// <summary>focus サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build() => RefOnlyCommandBuilder.Build(
        name: "focus",
        description: "Set keyboard focus to the element identified by ref.",
        toolName: "adact_focus",
        autoSnapshot: false);
}

/// <summary><c>scroll-into-view</c> コマンド。ScrollItemPattern で要素を可視範囲にスクロールする (低レベル)。</summary>
internal static class ScrollIntoViewCommand
{
    /// <summary>scroll-into-view サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build() => RefOnlyCommandBuilder.Build(
        name: "scroll-into-view",
        description: "Scroll the element into view using ScrollItemPattern.",
        toolName: "adact_scroll_into_view",
        autoSnapshot: false);
}

/// <summary><c>scroll</c> コマンド。ScrollPattern でコンテナ要素をスクロールする。</summary>
internal static class ScrollCommand
{
    /// <summary>scroll サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build()
    {
        var refArg = new Argument<string>("ref") { Description = "Ref ID of the scrollable container element." };
        var percentH = new Option<int?>("--percent-h") { Description = "Horizontal scroll position in percent (0-100)." };
        var percentV = new Option<int?>("--percent-v") { Description = "Vertical scroll position in percent (0-100)." };
        var smallH = new Option<int?>("--small-h") { Description = "Number of small horizontal scrolls. Positive=right, negative=left." };
        var smallV = new Option<int?>("--small-v") { Description = "Number of small vertical scrolls. Positive=down, negative=up." };
        var largeH = new Option<int?>("--large-h") { Description = "Number of large horizontal scrolls. Positive=right, negative=left." };
        var largeV = new Option<int?>("--large-v") { Description = "Number of large vertical scrolls. Positive=down, negative=up." };

        var cmd = new Command("scroll", "Scroll a container element using ScrollPattern. Specify one group: percent, small, or large.");
        cmd.Arguments.Add(refArg);
        cmd.Options.Add(percentH);
        cmd.Options.Add(percentV);
        cmd.Options.Add(smallH);
        cmd.Options.Add(smallV);
        cmd.Options.Add(largeH);
        cmd.Options.Add(largeV);

        cmd.SetAction((parseResult, ct) =>
        {
            var refVal = parseResult.GetValue(refArg)!;
            var ph = parseResult.GetValue(percentH);
            var pv = parseResult.GetValue(percentV);
            var sh = parseResult.GetValue(smallH);
            var sv = parseResult.GetValue(smallV);
            var lh = parseResult.GetValue(largeH);
            var lv = parseResult.GetValue(largeV);
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);

            bool hasPercent = ph is not null || pv is not null;
            bool hasSmall = sh is not null || sv is not null;
            bool hasLarge = lh is not null || lv is not null;
            int groupCount = (hasPercent ? 1 : 0) + (hasSmall ? 1 : 0) + (hasLarge ? 1 : 0);

            if (groupCount == 0)
                return Task.FromResult(OperationOptions.ReportUserError(
                    "At least one scroll parameter must be specified (--percent-h/--percent-v, --small-h/--small-v, or --large-h/--large-v)."));
            if (groupCount > 1)
                return Task.FromResult(OperationOptions.ReportUserError(
                    "percent, small, and large groups are mutually exclusive. Specify only one group."));

            var args = new Dictionary<string, object?> { ["ref"] = refVal };
            if (ph is not null) args["percentH"] = ph.Value;
            if (pv is not null) args["percentV"] = pv.Value;
            if (sh is not null) args["smallH"] = sh.Value;
            if (sv is not null) args["smallV"] = sv.Value;
            if (lh is not null) args["largeH"] = lh.Value;
            if (lv is not null) args["largeV"] = lv.Value;

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                async (client, token) =>
                {
                    var result = await client.CallToolAsync("adact_scroll", args, token).ConfigureAwait(false);
                    var errorExit = McpResponse.TryReportError(result);
                    if (errorExit is { } code) return code;
                    return ExitCodes.Success;
                },
                ct);
        });

        return cmd;
    }
}
