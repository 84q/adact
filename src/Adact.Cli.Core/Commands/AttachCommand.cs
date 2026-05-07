using System.CommandLine;
using System.Text.Json;

using Adact.Cli.Connection;
using Adact.Cli.Output;

using ModelContextProtocol.Protocol;

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
            Description = "Window Ref ID like 'w1' (from list-windows).",
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
                "Specify a positional ref (w<n>) obtained from list-windows.");
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
        var windowInfo = info.TryGetProperty("windowInfo", out var wi) ? wi : default;
        var processId = JsonHelpers.GetIntAsStringOrNull(windowInfo, "processId");
        var title = JsonHelpers.GetStringOrNull(windowInfo, "windowTitle");

        if (string.IsNullOrEmpty(sessionId))
        {
            CliError.Write(ErrorCodes.InternalError, "windows_attach response missing 'sessionId'.");
            return ExitCodes.CommandFailed;
        }

        var bodyFields = new List<KeyValuePair<string, string?>>
        {
            CliOutput.Field("sessionId", sessionId),
            CliOutput.Field("processId", processId),
            CliOutput.Field("title", title),
        };

        if (noSnapshot)
        {
            CliOutput.WriteYamlSuccess(metaFields: null, bodyFields);
            return ExitCodes.Success;
        }

        return await WriteAttachWithSnapshotAsync(client, sessionId, snapshotDir, bodyFields, ct).ConfigureAwait(false);
    }

    private static async Task<int> WriteAttachWithSnapshotAsync(
        IAdactMcpClient client,
        string sessionId,
        string? snapshotDir,
        IReadOnlyList<KeyValuePair<string, string?>> bodyFields,
        CancellationToken ct)
    {
        var snapshotResult = await client.CallToolAsync(
            "windows_snapshot",
            new Dictionary<string, object?> { ["sessionId"] = sessionId },
            ct).ConfigureAwait(false);
        var errorExit = McpResponse.TryReportError(snapshotResult);
        if (errorExit is { } code) return code;

        var snapJson = McpResponse.GetJson(snapshotResult);
        var raw = snapshotResult.Content is { Count: > 0 } content && content[0] is TextContentBlock tcb && !string.IsNullOrEmpty(tcb.Text)
            ? tcb.Text
            : snapJson.GetRawText();

        string text;
        try
        {
            var (meta, root) = Snapshots.SnapshotJsonParser.Parse(raw);
            var filtered = Snapshots.SnapshotTreeFilter.Apply(root, Snapshots.SnapshotTreeFilter.FilterOperable);
            text = Snapshots.SnapshotTextFormatter.Format(meta, filtered, Snapshots.SnapshotTreeFilter.FilterOperable);
        }
        catch (JsonException ex)
        {
            CliError.Write(ErrorCodes.InternalError, $"Failed to parse snapshot response: {ex.Message}");
            return ExitCodes.CommandFailed;
        }

        var (path, isNew) = Snapshots.SnapshotFileWriter.Write(text, int.Parse(sessionId[1..], System.Globalization.CultureInfo.InvariantCulture), snapshotDir);
        var snapshotPath = $"{path} {(isNew ? "(changed)" : "(unchanged)")}";
        CliOutput.WriteYamlSuccess([CliOutput.Field("snapshotPath", snapshotPath)], bodyFields);
        return ExitCodes.Success;
    }
}
