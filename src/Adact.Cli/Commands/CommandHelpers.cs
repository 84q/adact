using System.CommandLine;
using System.Text.Json;

using Adact.Cli.Connection;
using Adact.Cli.Output;
using Adact.Cli.Snapshots;

using ModelContextProtocol.Protocol;

namespace Adact.Cli.Commands;

/// <summary>
/// CLI サブコマンドが共通で使う接続・エラー処理・<c>--server</c> Option・snapshot 取得 フローヘルパー。
/// </summary>
internal static class CommandHelpers
{
    internal static Func<ServerEndpoint, CancellationToken, Task<IAdactMcpClient>> ConnectClientAsync { get; set; }
        = static async (endpoint, ct) => await AdactMcpClient.ConnectAsync(
            endpoint,
            loggerFactory: null,
            ct).ConfigureAwait(false);

    /// <summary>
    /// 接続解決 → MCP 接続 → コマンド実装の呼び出し、を共通化する。
    /// 解決失敗 → INVALID_ARGUMENT (exit 2)、接続失敗 → CONNECTION_FAILED (exit 3)、
    /// その他 → INTERNAL_ERROR (exit 1)。
    /// </summary>
    /// <param name="serverArg"><c>--server</c> の値。null なら <c>.adact/config.json</c> / 既定エンドポイントを試行する。</param>
    /// <param name="exec">接続済みクライアントを受け取り、MCP を呼び出して exit code を返すコマンド実装。</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>exit code。</returns>
    public static async Task<int> RunWithClientAsync(
        string? serverArg,
        Func<IAdactMcpClient, CancellationToken, Task<int>> exec,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(exec);

        ServerEndpoint endpoint;
        try
        {
            endpoint = ConnectionResolver.Resolve(serverArg);
        }
        catch (InvalidUrlException ex)
        {
            return ConnectionErrors.ReportResolutionError(ex);
        }
        catch (ConfigParseException ex)
        {
            return ConnectionErrors.ReportResolutionError(ex);
        }

        try
        {
            await using var client = await ConnectClientAsync(endpoint, ct).ConfigureAwait(false);
            return await exec(client, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ConnectionErrors.ReportAndReturnExitCode(ex, endpoint);
        }
    }

    /// <summary>
    /// 将来コマンド追加時の暫定スタブ。INTERNAL_ERROR + "not implemented yet" を stderr に出力し、
    /// <see cref="ExitCodes.CommandFailed"/> を返す。
    /// </summary>
    /// <param name="commandName">未実装として報告するコマンド名。</param>
    /// <returns>常に <see cref="ExitCodes.CommandFailed"/>。</returns>
    public static int NotYetImplemented(string commandName)
    {
        CliError.Write(
            ErrorCodes.InternalError,
            $"{commandName}: not implemented yet (Phase 5 in progress).");
        return ExitCodes.CommandFailed;
    }

    /// <summary>
    /// 共通 <c>--server</c> Option。設計 009 §3 / §4.2。
    /// 各コマンドはこのヘルパで Option を生成し、AddOption で root に登録する。
    /// </summary>
    /// <returns>全コマンド共用の <c>--server</c> Option。</returns>
    public static Option<string?> CreateServerOption() =>
        new("--server")
        {
            Description = "Connection target URL (e.g. http://127.0.0.1:41300/mcp). "
                + "Falls back to .adact/config.json or the default endpoint.",
        };

    /// <summary>
    /// MCP <c>windows_snapshot</c> を呼び、結果を CLI 側で operable / raw フィルタを適用した上で
    /// テキスト整形して <see cref="SnapshotFileWriter"/> でファイルに書き出し、stdout に
    /// <c>sessionId / snapshot</c> を出力する。設計 009 §5.2、011 §4.5、016 §2。
    /// </summary>
    /// <param name="client">接続済み MCP クライアント。</param>
    /// <param name="sessionId">対象 session ID (例 "s1")。null なら active session。</param>
    /// <param name="snapshotDir">snapshot 保存先 (null なら既定 .adact/)。</param>
    /// <param name="ct">cancellation token。</param>
    /// <param name="writeSessionId">true の場合 stdout に sessionId 行を書き出す。
    /// 呼び出し側で既に出力済みの場合 (例: attach コマンド) は false を指定する。</param>
    /// <param name="filter">"operable" / "raw" を指定する CLI フィルタ。null/省略時は operable。</param>
    /// <param name="writeContentToStdout">
    /// true の場合、snapshot テキスト本体も stdout に書き出す。
    /// <c>snapshot</c> コマンド専用。click/fill 等の auto-snapshot では false のままとする。
    /// </param>
    /// <returns>exit code (成功時 0)。エラー時は <see cref="McpResponse.TryReportError"/> 経由で stderr 出力 + 1。</returns>
    /// <exception cref="ArgumentNullException">client が null。</exception>
    public static async Task<int> WriteSnapshotResultAsync(
        IAdactMcpClient client,
        string? sessionId,
        string? snapshotDir,
        CancellationToken ct,
        bool writeSessionId = true,
        string? filter = null,
        bool writeContentToStdout = false)
    {
        ArgumentNullException.ThrowIfNull(client);

        var resolvedFilter = string.IsNullOrEmpty(filter) ? SnapshotTreeFilter.FilterOperable : filter;
        if (!SnapshotTreeFilter.IsKnownFilter(resolvedFilter))
        {
            CliError.Write(ErrorCodes.InvalidArgument,
                $"Unknown filter '{resolvedFilter}'. Use 'operable' or 'raw'.");
            return ExitCodes.UserError;
        }
        resolvedFilter = SnapshotTreeFilter.Normalize(resolvedFilter);

        IReadOnlyDictionary<string, object?>? snapArgs = string.IsNullOrEmpty(sessionId)
            ? null
            : new Dictionary<string, object?> { ["sessionId"] = sessionId };

        var snapResult = await client.CallToolAsync("windows_snapshot", snapArgs, ct).ConfigureAwait(false);
        var snapErrorExit = McpResponse.TryReportError(snapResult);
        if (snapErrorExit is { } snapCode) return snapCode;

        var snapJson = McpResponse.GetJson(snapResult);
        var meta = snapJson.ValueKind == JsonValueKind.Object && snapJson.TryGetProperty("_meta", out var m)
            ? m
            : default;

        var resolvedSid = (meta.ValueKind == JsonValueKind.Object
            ? JsonHelpers.GetStringOrNull(meta, "sessionId")
            : null) ?? sessionId;

        if (string.IsNullOrEmpty(resolvedSid))
        {
            CliError.Write(ErrorCodes.InternalError, "windows_snapshot response missing sessionId.");
            return ExitCodes.CommandFailed;
        }

        var raw = ExtractSnapshotJsonText(snapResult, snapJson);
        string text;
        try
        {
            var (parsedMeta, parsedRoot) = SnapshotJsonParser.Parse(raw);
            var filtered = SnapshotTreeFilter.Apply(parsedRoot, resolvedFilter);
            text = SnapshotTextFormatter.Format(parsedMeta, filtered, resolvedFilter);
        }
        catch (JsonException ex)
        {
            CliError.Write(ErrorCodes.InternalError,
                $"Failed to parse snapshot response: {ex.Message}");
            return ExitCodes.CommandFailed;
        }

        var sidNum = ParseSidNumber(resolvedSid);
        var (path, isNew) = SnapshotFileWriter.Write(text, sidNum, snapshotDir);

        if (writeSessionId)
        {
            KeyValueWriter.Write("sessionId", resolvedSid);
        }
        KeyValueWriter.Write("snapshot", path);
        if (!isNew)
        {
            KeyValueWriter.Write("unchanged", "true");
        }

        if (writeContentToStdout)
        {
            Console.Out.WriteLine(text);
        }
        return ExitCodes.Success;
    }

    /// <summary>
    /// click / fill など「Element Ref を操作 → snapshot 自動取得 → stdout 出力」の共通フロー。
    /// 設計 009 §4.4 / §5.2。snapshot 部分は <see cref="WriteSnapshotResultAsync"/> に委譲する。
    /// </summary>
    /// <param name="client">接続済み MCP クライアント。</param>
    /// <param name="operationToolName">"windows_click" または "windows_fill" など。</param>
    /// <param name="operationArgs">操作ツールに渡す引数。</param>
    /// <param name="elementRef">操作対象の Element Ref ID。snapshot 用 sessionId 抽出に利用。</param>
    /// <param name="noSnapshot">true なら snapshot 取得をスキップ。</param>
    /// <param name="snapshotDir">snapshot 保存先 (null なら既定 .adact/)。</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>exit code。</returns>
    public static async Task<int> RunRefOperationAndAutoSnapshotAsync(
        IAdactMcpClient client,
        string operationToolName,
        Dictionary<string, object?> operationArgs,
        string elementRef,
        bool noSnapshot,
        string? snapshotDir,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(operationToolName);
        ArgumentNullException.ThrowIfNull(operationArgs);
        ArgumentNullException.ThrowIfNull(elementRef);

        var opResult = await client.CallToolAsync(operationToolName, operationArgs, ct).ConfigureAwait(false);
        var opErrorExit = McpResponse.TryReportError(opResult);
        if (opErrorExit is { } code) return code;

        var sessionRef = RefValidator.ExtractSessionId(elementRef);

        if (noSnapshot)
        {
            // snapshot 抑制時は最低限の手掛かりとして ref から抽出した sessionId のみ出力する。
            if (!string.IsNullOrEmpty(sessionRef))
            {
                KeyValueWriter.Write("sessionId", sessionRef);
            }
            return ExitCodes.Success;
        }

        return await WriteSnapshotResultAsync(client, sessionRef, snapshotDir, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// session id ベースの操作 (resize/minimize/maximize/restore 等) を呼び、成功時に auto-snapshot を取得する共通フロー。
    /// </summary>
    /// <param name="client">接続済み MCP クライアント。</param>
    /// <param name="operationToolName">"windows_resize" など操作 MCP ツール名。</param>
    /// <param name="operationArgs">操作ツールに渡す引数 (sessionId は呼び出し側で詰めること)。</param>
    /// <param name="sessionId">対象 session ID (例 "s1")。null は active session。</param>
    /// <param name="noSnapshot">true なら snapshot 取得をスキップし、sessionId のみ出力する。</param>
    /// <param name="snapshotDir">snapshot 保存先 (null なら既定 .adact/)。</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>exit code。</returns>
    public static async Task<int> RunSessionOperationAndAutoSnapshotAsync(
        IAdactMcpClient client,
        string operationToolName,
        Dictionary<string, object?> operationArgs,
        string? sessionId,
        bool noSnapshot,
        string? snapshotDir,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(operationToolName);
        ArgumentNullException.ThrowIfNull(operationArgs);

        var opResult = await client.CallToolAsync(operationToolName, operationArgs, ct).ConfigureAwait(false);
        var opErrorExit = McpResponse.TryReportError(opResult);
        if (opErrorExit is { } code) return code;

        if (noSnapshot)
        {
            // snapshot 抑制時は手掛かりとして sessionId のみ出力する (明示指定があれば)。
            if (!string.IsNullOrEmpty(sessionId))
            {
                KeyValueWriter.Write("sessionId", sessionId);
            }
            return ExitCodes.Success;
        }

        // minimize 後は座標取得が失敗し snapshot 自体が SNAPSHOT_FAILED を返し得るが、
        // ツール側で invalid 化した snapshot レスポンスはエラーマップ済みなので CLI は通常通り扱う。
        // ユーザは --no-snapshot を併用することで snapshot をスキップ可能。
        return await WriteSnapshotResultAsync(client, sessionId, snapshotDir, ct).ConfigureAwait(false);
    }

    /// <summary>セッション ID 文字列 (<c>s1</c> など) から数値部分 (1) を取り出す。不正形式は 0 を返す。</summary>
    /// <param name="sessionId">セッション ID (例: <c>s1</c>)。</param>
    /// <returns>セッション ID の数値部分。不正形式の場合は 0。</returns>
    private static int ParseSidNumber(string sessionId)
    {
        if (sessionId.Length >= 2 && sessionId[0] == 's'
            && int.TryParse(sessionId.AsSpan(1), out var n))
        {
            return n;
        }
        return 0;
    }

    /// <summary>MCP <see cref="CallToolResult"/> から snapshot JSON 生文字列を取り出す。Content[0].Text を優先、なければ parsed の <see cref="JsonElement.GetRawText"/>。</summary>
    /// <param name="result">MCP ツール呼び出しの生レスポンス。</param>
    /// <param name="parsed">事前に parse 済みの <see cref="JsonElement"/>。Content に text がない場合の fallback として使用する。</param>
    /// <returns>snapshot JSON の生文字列。</returns>
    private static string ExtractSnapshotJsonText(CallToolResult result, JsonElement parsed)
    {
        if (result.Content is { Count: > 0 } content
            && content[0] is TextContentBlock tcb
            && !string.IsNullOrEmpty(tcb.Text))
        {
            return tcb.Text;
        }
        return parsed.GetRawText();
    }
}
