using System.CommandLine;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>attach</c> コマンド。Window Ref もしくは matching flags で window に attach し、
/// session を作成する。設計 docs/spec/cli.md attach 項。
/// </summary>
internal static class AttachCommand
{
    /// <summary>
    /// attach コマンドの引数バリデーション対象。Unit テストから直接呼び出すため
    /// snapshot 関連の補助オプション (--no-snapshot/--snapshot-dir) は含めない。
    /// </summary>
    /// <param name="Ref">位置引数として与えられた Window Ref (例: <c>w1</c>)。</param>
    /// <param name="ProcessName">--process-name。</param>
    /// <param name="Title">--title。</param>
    /// <param name="ProcessId">--process-id。</param>
    /// <param name="ClassName">--class-name。</param>
    internal sealed record AttachArgs(
        string? Ref,
        string? ProcessName,
        string? Title,
        int? ProcessId,
        string? ClassName);

    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>attach サブコマンド。</returns>
    public static Command Build()
    {
        var refArg = new Argument<string?>("ref")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Window Ref ID like 'w1' (from list-apps).",
        };
        var processName = new Option<string?>("--process-name") { Description = "Process name (e.g. CalculatorApp)." };
        var title = new Option<string?>("--title") { Description = "Window title (case-insensitive, exact match)." };
        var processId = new Option<int?>("--process-id") { Description = "Process ID." };
        var className = new Option<string?>("--class-name") { Description = "Win32 class name." };
        var noSnapshot = new Option<bool>("--no-snapshot") { Description = "Do not capture a snapshot on success." };
        var snapshotDir = new Option<string?>("--snapshot-dir") { Description = "Snapshot output directory (default '.adact/')." };
        var server = CommandHelpers.CreateServerOption();

        var cmd = new Command("attach", "Attach to a window as a session.");
        cmd.Arguments.Add(refArg);
        cmd.Options.Add(processName);
        cmd.Options.Add(title);
        cmd.Options.Add(processId);
        cmd.Options.Add(className);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);
        cmd.Options.Add(server);

        cmd.SetAction((parseResult, ct) =>
        {
            var args = new AttachArgs(
                Ref: parseResult.GetValue(refArg),
                ProcessName: parseResult.GetValue(processName),
                Title: parseResult.GetValue(title),
                ProcessId: parseResult.GetValue(processId),
                ClassName: parseResult.GetValue(className));

            // 引数バリデーションは接続前に実施する。
            var (errorCode, errorMessage) = ValidateAttachArgs(args);
            if (errorCode is not null)
            {
                CliError.Write(errorCode, errorMessage ?? "invalid arguments.");
                return Task.FromResult(ExitCodes.UserError);
            }

            var arguments = BuildArguments(args);
            var noSnap = parseResult.GetValue(noSnapshot);
            var dir = parseResult.GetValue(snapshotDir);
            var serverArg = parseResult.GetValue(server);

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => ExecuteAsync(client, arguments, noSnap, dir, token),
                ct);
        });

        return cmd;
    }

    /// <summary>
    /// attach 引数のバリデーションのみ実施する (MCP 呼び出しは行わない)。
    /// 不正なら <c>(errorCode, errorMessage)</c>、正常なら <c>(null, null)</c> を返す。
    /// </summary>
    /// <param name="args">attach 引数。</param>
    /// <returns>(エラーコード, メッセージ) のタプル。有効なら両方 null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="args"/> が null。</exception>
    internal static (string? errorCode, string? errorMessage) ValidateAttachArgs(AttachArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var hasFlags = args.ProcessName is not null
            || args.Title is not null
            || args.ProcessId is not null
            || args.ClassName is not null;

        if (!string.IsNullOrEmpty(args.Ref))
        {
            if (!RefValidator.IsWindowRef(args.Ref))
            {
                return (ErrorCodes.InvalidArgument,
                    $"ref must be in 'w<n>' form, got '{args.Ref}'.");
            }
            if (hasFlags)
            {
                return (ErrorCodes.InvalidArgument,
                    "Positional ref and matching flags (--process-name/--title/--process-id/--class-name) are mutually exclusive.");
            }
            return (null, null);
        }

        if (!hasFlags)
        {
            return (ErrorCodes.InvalidArgument,
                "Specify either positional ref (w<n>) or at least one of --process-name/--title/--process-id/--class-name.");
        }

        return (null, null);
    }

    /// <summary>
    /// <see cref="AttachArgs"/> を MCP <c>windows_attach</c> に渡す辞書引数に変換する。
    /// Ref と matching flags は互いに排他使用されるため、Ref があれば windowRef のみを指定する。
    /// </summary>
    /// <param name="args">バリデーション済み attach 引数。</param>
    /// <returns>MCP <c>windows_attach</c> に渡す辞書。</returns>
    private static Dictionary<string, object?> BuildArguments(AttachArgs args)
    {
        if (!string.IsNullOrEmpty(args.Ref))
        {
            return new Dictionary<string, object?> { ["windowRef"] = args.Ref };
        }

        var dict = new Dictionary<string, object?>();
        if (args.ProcessName is not null) dict["processName"] = args.ProcessName;
        if (args.Title is not null) dict["windowTitle"] = args.Title;
        if (args.ClassName is not null) dict["className"] = args.ClassName;
        if (args.ProcessId is not null) dict["processId"] = args.ProcessId.Value;
        return dict;
    }

    /// <summary>接続済みクライアントに対し <c>windows_attach</c> を呼び、成功時は sessionId ・ windowRef ・ snapshot を出力する。</summary>
    /// <param name="client">接続済み MCP クライアント。</param>
    /// <param name="arguments"><c>windows_attach</c> に渡す引数。</param>
    /// <param name="noSnapshot">true なら attach 成功後の snapshot 取得をスキップする。</param>
    /// <param name="snapshotDir">snapshot 保存先 (null なら既定 <c>.adact/</c>)。</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>exit code。</returns>
    private static async Task<int> ExecuteAsync(
        AdactMcpClient client,
        Dictionary<string, object?> arguments,
        bool noSnapshot,
        string? snapshotDir,
        CancellationToken ct)
    {
        var attachResult = await client.CallToolAsync("windows_attach", arguments, ct).ConfigureAwait(false);

        var errorExit = McpResponse.TryReportError(attachResult);
        if (errorExit is { } code) return code;

        var info = McpResponse.GetJson(attachResult);
        var sessionId = JsonHelpers.GetStringOrNull(info, "sessionId");
        var windowRef = JsonHelpers.GetStringOrNull(info, "windowRef");

        if (string.IsNullOrEmpty(sessionId))
        {
            CliError.Write(ErrorCodes.InternalError, "windows_attach response missing 'sessionId'.");
            return ExitCodes.CommandFailed;
        }

        // 出力順は sessionId / windowRef / snapshot (設計 009 §5.2、011 §4.5)。
        // sessionId / windowRef は attach 結果から書き出し、snapshot は
        // WriteSnapshotResultAsync (writeSessionId=false) に委譲する。
        KeyValueWriter.Write("sessionId", sessionId);
        if (!string.IsNullOrEmpty(windowRef))
        {
            KeyValueWriter.Write("windowRef", windowRef);
        }

        if (noSnapshot)
        {
            return ExitCodes.Success;
        }

        return await CommandHelpers.WriteSnapshotResultAsync(
            client,
            sessionId,
            snapshotDir,
            ct,
            writeSessionId: false).ConfigureAwait(false);
    }
}
