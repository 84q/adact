using System.CommandLine;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>attach</c> コマンド。Window Ref を受け取り、対応する window に attach して
/// session を作成する。設計 docs/spec/cli.md attach 項。
/// </summary>
internal static class AttachCommand
{
    /// <summary>
    /// attach コマンドの引数バリデーション対象。Unit テストから直接呼び出すため
    /// snapshot 関連の補助オプション (--no-snapshot/--snapshot-dir) は含めない。
    /// </summary>
    /// <param name="Ref">位置引数として与えられた Window Ref (例: <c>w1</c>)。</param>
    internal sealed record AttachArgs(string? Ref);

    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>attach サブコマンド。</returns>
    public static Command Build()
    {
        var refArg = new Argument<string?>("ref")
        {
            Arity = ArgumentArity.ExactlyOne,
            Description = "Window Ref ID like 'w1' (from list-apps).",
        };
        var noSnapshot = new Option<bool>("--no-snapshot") { Description = "Do not capture a snapshot on success." };
        var snapshotDir = new Option<string?>("--snapshot-dir") { Description = "Snapshot output directory (default '.adact/')." };

        var cmd = new Command("attach", "Attach to a window as a session.");
        cmd.Arguments.Add(refArg);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);

        cmd.SetAction((parseResult, ct) =>
        {
            var args = new AttachArgs(Ref: parseResult.GetValue(refArg));

            // 引数バリデーションは接続前に実施する。
            var (errorCode, errorMessage) = ValidateAttachArgs(args);
            if (errorCode is not null)
            {
                CliError.Write(errorCode, errorMessage ?? "invalid arguments.");
                return Task.FromResult(ExitCodes.UserError);
            }

            var arguments = new Dictionary<string, object?> { ["windowRef"] = args.Ref };
            var noSnap = parseResult.GetValue(noSnapshot);
            var dir = parseResult.GetValue(snapshotDir);
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);

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

        if (string.IsNullOrEmpty(args.Ref))
        {
            return (ErrorCodes.InvalidArgument,
                "Specify a positional ref (w<n>) obtained from list-apps.");
        }

        if (!RefValidator.IsWindowRef(args.Ref))
        {
            return (ErrorCodes.InvalidArgument,
                $"ref must be in 'w<n>' form, got '{args.Ref}'.");
        }

        return (null, null);
    }

    /// <summary>接続済みクライアントに対し <c>windows_attach</c> を呼び、成功時は sessionId ・ windowRef ・ snapshot を出力する。</summary>
    /// <param name="client">接続済み MCP クライアント。</param>
    /// <param name="arguments"><c>windows_attach</c> に渡す引数。</param>
    /// <param name="noSnapshot">true なら attach 成功後の snapshot 取得をスキップする。</param>
    /// <param name="snapshotDir">snapshot 保存先 (null なら既定 <c>.adact/</c>)。</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>exit code。</returns>
    private static async Task<int> ExecuteAsync(
        IAdactMcpClient client,
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
