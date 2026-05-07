using System.CommandLine;
using System.Text.Json;
using System.IO.Pipes;

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
    private static readonly AsyncLocal<CommandRuntime?> RuntimeOverride = new();
    private static readonly CommandRuntime DefaultRuntime = CommandRuntime.CreateDefault();
    private static CommandRuntime Runtime => RuntimeOverride.Value ?? DefaultRuntime;

    private static readonly TimeSpan AutoStartReconnectRetryDelay = TimeSpan.FromMilliseconds(150);
    private const int AutoStartReconnectRetryCount = 5;

    /// <summary>
    /// 接続解決 → MCP 接続 → コマンド実装の呼び出し、を共通化する。
    /// 解決失敗 → INVALID_ARGUMENT (exit 2)、接続失敗 → CONNECTION_FAILED (exit 3)、
    /// その他 → INTERNAL_ERROR (exit 1)。
    /// </summary>
    /// <param name="serverArg"><c>--server</c> の値。null/空白なら Named Pipe を使用。</param>
    /// <param name="exec">接続済みクライアントを受け取り、MCP を呼び出して exit code を返すコマンド実装。</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>exit code。</returns>
    public static async Task<int> RunWithClientAsync(
        string? serverArg,
        Func<IAdactMcpClient, CancellationToken, Task<int>> exec,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(exec);

        // --server 指定時: HTTPモード、未指定時: Named Pipe
        var httpEndpoint = ConnectionResolver.ResolveHttpEndpoint(serverArg);

        if (httpEndpoint is not null)
        {
            // HTTP モード
            return await RunWithHttpClientAsync(httpEndpoint, exec, ct).ConfigureAwait(false);
        }
        else
        {
            // Named Pipe モード
            var pipeEndpoint = ConnectionResolver.ResolveNamedPipeEndpoint();
            return await RunWithNamedPipeClientAsync(pipeEndpoint, exec, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// HTTP クライアントで接続してコマンドを実行する。
    /// </summary>
    private static async Task<int> RunWithHttpClientAsync(
        ServerEndpoint endpoint,
        Func<IAdactMcpClient, CancellationToken, Task<int>> exec,
        CancellationToken ct)
    {
        try
        {
            await using var client = await Runtime.ConnectHttpClientAsync(endpoint, ct).ConfigureAwait(false);
            return await exec(client, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ConnectionErrors.ReportAndReturnExitCode(ex, endpoint);
        }
    }

    /// <summary>
    /// Named Pipe クライアントで接続してコマンドを実行する。
    /// 接続失敗時は CONNECTION_FAILED エラー。
    /// </summary>
    private static async Task<int> RunWithNamedPipeClientAsync(
        NamedPipeEndPoint endpoint,
        Func<IAdactMcpClient, CancellationToken, Task<int>> exec,
        CancellationToken ct)
    {
        try
        {
            await using var client = await Runtime.ConnectNamedPipeClientAsync(endpoint, ct).ConfigureAwait(false);
            return await exec(client, ct).ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            // Named Pipe 接続タイムアウト時は CONNECTION_FAILED
            return ReportNamedPipeConnectionFailed(endpoint, ex.Message);
        }
        catch (IOException ex)
        {
            // Named Pipe 接続失敗時は CONNECTION_FAILED
            return ReportNamedPipeConnectionFailed(endpoint, ex.Message);
        }
        catch (Exception ex)
        {
            CliError.Write(
                ErrorCodes.InternalError,
                $"Unexpected error while connecting to named pipe '{endpoint.PipeName}': {ex.Message}");
            return ExitCodes.CommandFailed;
        }
    }

    /// <summary>
    /// Named Pipe 接続失敗を報告する。
    /// </summary>
    private static int ReportNamedPipeConnectionFailed(NamedPipeEndPoint endpoint, string message)
    {
        CliError.Write(
            ErrorCodes.ConnectionFailed,
            $"No ADACT server is running. {message}",
            "Run 'adact serve pipe' to start the server with named pipe transport (local), or 'adact serve http' for remote access.");
        return ExitCodes.ConnectionFailed;
    }

    /// <summary>
    /// 接続解決 → （未起動時は自動起動）→ MCP 接続 → コマンド実装の呼び出し、を共通化する。
    /// list-windows と launch 専用。自動起動が有効な場合はサーバー未起動時に自動起動を試みる。
    /// </summary>
    /// <param name="serverArg"><c>--server</c> の値。null/空白なら Named Pipe を使用。</param>
    /// <param name="exec">接続済みクライアントを受け取り、MCP を呼び出して exit code を返すコマンド実装。</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>exit code。</returns>
    public static async Task<int> RunWithClientAndAutoStartAsync(
        string? serverArg,
        Func<IAdactMcpClient, CancellationToken, Task<int>> exec,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(exec);

        // --server 指定時: HTTPモード（自動起動なし）
        var httpEndpoint = ConnectionResolver.ResolveHttpEndpoint(serverArg);
        if (httpEndpoint is not null)
        {
            return await RunWithHttpClientAsync(httpEndpoint, exec, ct).ConfigureAwait(false);
        }

        // Named Pipe モード
        var pipeEndpoint = ConnectionResolver.ResolveNamedPipeEndpoint();

        // まず、短いタイムアウトでサーバーが起動しているか確認（高速パス）
        var isRunning = await Runtime.IsServerRunningAsync(pipeEndpoint, 100, ct).ConfigureAwait(false);

        if (isRunning)
        {
            // サーバーが起動している - 通常接続
            try
            {
                await using var client = await Runtime.ConnectNamedPipeClientAsync(pipeEndpoint, ct).ConfigureAwait(false);
                return await exec(client, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return ReportNamedPipeConnectionFailed(pipeEndpoint, ex.Message);
            }
        }

        // サーバー未起動 - 自動起動を試みる（遅延なし）
        if (Runtime.TryAutoStartServerAsync is not null)
        {
            var started = await Runtime.TryAutoStartServerAsync(ct).ConfigureAwait(false);
            if (started)
            {
                try
                {
                    await using var client = await ConnectNamedPipeClientAfterAutoStartAsync(pipeEndpoint, ct).ConfigureAwait(false);
                    return await exec(client, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    return ReportNamedPipeConnectionFailed(pipeEndpoint, ex.Message);
                }
            }
        }

        // 自動起動失敗または無効
        return ReportNamedPipeConnectionFailed(pipeEndpoint, "Named pipe connection failed and auto-start was not available or failed.");
    }

    private static async Task<IAdactMcpClient> ConnectNamedPipeClientAfterAutoStartAsync(
        NamedPipeEndPoint endpoint,
        CancellationToken ct)
    {
        Exception? last = null;

        for (var attempt = 1; attempt <= AutoStartReconnectRetryCount; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                return await Runtime.ConnectNamedPipeClientAsync(endpoint, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is TimeoutException or IOException)
            {
                last = ex;
                if (attempt == AutoStartReconnectRetryCount)
                {
                    throw;
                }

                await Task.Delay(AutoStartReconnectRetryDelay, ct).ConfigureAwait(false);
            }
        }

        throw last ?? new TimeoutException("Named pipe reconnect failed after auto-start.");
    }

    internal static IDisposable PushRuntime(CommandRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        var previous = RuntimeOverride.Value;
        RuntimeOverride.Value = runtime;
        return new Scope(() => RuntimeOverride.Value = previous);
    }

    internal sealed record CommandRuntime(
        Func<ServerEndpoint, CancellationToken, Task<IAdactMcpClient>> ConnectHttpClientAsync,
        Func<NamedPipeEndPoint, CancellationToken, Task<IAdactMcpClient>> ConnectNamedPipeClientAsync,
        Func<NamedPipeEndPoint, int, CancellationToken, Task<bool>> IsServerRunningAsync,
        Func<CancellationToken, Task<bool>>? TryAutoStartServerAsync)
    {
        public static CommandRuntime CreateDefault(Func<CancellationToken, Task<bool>>? tryAutoStartServerAsync = null)
            => new(
                static async (endpoint, ct) => await AdactMcpClient.ConnectAsync(endpoint, loggerFactory: null, ct).ConfigureAwait(false),
                static async (endpoint, ct) => await NamedPipeMcpClient.ConnectAsync(endpoint, loggerFactory: null, ct).ConfigureAwait(false),
                NamedPipeMcpClient.IsServerRunningAsync,
                tryAutoStartServerAsync);
    }

    private sealed class Scope(Action onDispose) : IDisposable
    {
        private readonly Action _onDispose = onDispose;
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _onDispose();
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
    /// 共通 <c>--server</c> Option。設計 009 §3 / §4.2、031。
    /// RootCommand に <c>Recursive = true</c> で登録することで、全サブコマンドで利用可能になる。
    /// </summary>
    public static readonly Option<string?> ServerOption = new("--server")
    {
        Description = "Connection target URL (e.g. http://127.0.0.1:41300/mcp). "
            + "Falls back to .adact/config.json or the default endpoint.",
        Recursive = true,
    };

    /// <summary>
    /// MCP <c>adact_snapshot</c> を呼び、結果を CLI 側で operable / raw フィルタを適用した上で
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

        var snapResult = await client.CallToolAsync("adact_snapshot", snapArgs, ct).ConfigureAwait(false);
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
            CliError.Write(ErrorCodes.InternalError, "adact_snapshot response missing sessionId.");
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

        var snapshotPath = $"{path} {(isNew ? "(changed)" : "(unchanged)")}";
        var treeText = ExtractSnapshotTreeText(text);

        if (writeContentToStdout)
        {
            CliOutput.WriteSnapshotSuccess(
                snapshotPath,
                [CliOutput.Field("sessionId", resolvedSid)],
                treeText);
        }
        else
        {
            var metaFields = new[] { CliOutput.Field("snapshotPath", snapshotPath) };
            var bodyFields = new List<KeyValuePair<string, string?>>();
            if (writeSessionId)
            {
                bodyFields.Add(CliOutput.Field("sessionId", resolvedSid));
            }

            CliOutput.WriteYamlSuccess(metaFields, bodyFields);
        }
        return ExitCodes.Success;
    }

    /// <summary>
    /// click / fill など「Element Ref を操作 → snapshot 自動取得 → stdout 出力」の共通フロー。
    /// 設計 009 §4.4 / §5.2。snapshot 部分は <see cref="WriteSnapshotResultAsync"/> に委譲する。
    /// </summary>
    /// <param name="client">接続済み MCP クライアント。</param>
    /// <param name="actionName">CLI 側のアクション名 (例: <c>click</c>)。</param>
    /// <param name="operationToolName">"adact_click" または "adact_fill" など。</param>
    /// <param name="operationArgs">操作ツールに渡す引数。</param>
    /// <param name="elementRef">操作対象の Element Ref ID。snapshot 用 sessionId 抽出に利用。</param>
    /// <param name="noSnapshot">true なら snapshot 取得をスキップ。</param>
    /// <param name="snapshotDir">snapshot 保存先 (null なら既定 .adact/)。</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>exit code。</returns>
    public static async Task<int> RunRefOperationAndAutoSnapshotAsync(
        IAdactMcpClient client,
        string actionName,
        string operationToolName,
        Dictionary<string, object?> operationArgs,
        string elementRef,
        bool noSnapshot,
        string? snapshotDir,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(actionName);
        ArgumentNullException.ThrowIfNull(operationToolName);
        ArgumentNullException.ThrowIfNull(operationArgs);
        ArgumentNullException.ThrowIfNull(elementRef);

        var opResult = await client.CallToolAsync(operationToolName, operationArgs, ct).ConfigureAwait(false);
        var opErrorExit = McpResponse.TryReportError(opResult);
        if (opErrorExit is { } code) return code;

        var sessionRef = RefValidator.ExtractSessionId(elementRef);

        return await WriteRefOperationSuccessAsync(
            client,
            actionName,
            operationArgs,
            sessionRef,
            noSnapshot,
            snapshotDir,
            ct).ConfigureAwait(false);
    }

    /// <summary>
    /// session id ベースの操作 (resize/minimize/maximize/restore 等) を呼び、成功時に auto-snapshot を取得する共通フロー。
    /// </summary>
    /// <param name="client">接続済み MCP クライアント。</param>
    /// <param name="actionName">CLI 側のアクション名 (例: <c>resize</c>)。</param>
    /// <param name="operationToolName">"adact_resize_window" など操作 MCP ツール名。</param>
    /// <param name="operationArgs">操作ツールに渡す引数 (sessionId は呼び出し側で詰めること)。</param>
    /// <param name="sessionId">対象 session ID (例 "s1")。null は active session。</param>
    /// <param name="noSnapshot">true なら snapshot 取得をスキップし、sessionId のみ出力する。</param>
    /// <param name="snapshotDir">snapshot 保存先 (null なら既定 .adact/)。</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>exit code。</returns>
    public static async Task<int> RunSessionOperationAndAutoSnapshotAsync(
        IAdactMcpClient client,
        string actionName,
        string operationToolName,
        Dictionary<string, object?> operationArgs,
        string? sessionId,
        bool noSnapshot,
        string? snapshotDir,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(actionName);
        ArgumentNullException.ThrowIfNull(operationToolName);
        ArgumentNullException.ThrowIfNull(operationArgs);

        var opResult = await client.CallToolAsync(operationToolName, operationArgs, ct).ConfigureAwait(false);
        var opErrorExit = McpResponse.TryReportError(opResult);
        if (opErrorExit is { } code) return code;

        return await WriteSessionOperationSuccessAsync(
            client,
            actionName,
            operationArgs,
            sessionId,
            noSnapshot,
            snapshotDir,
            ct).ConfigureAwait(false);
    }

    public static int WriteToolSuccess(string actionName, IEnumerable<KeyValuePair<string, string?>> bodyFields)
    {
        CliOutput.WriteYamlSuccess(metaFields: null, bodyFields.Prepend(CliOutput.Field("action", actionName)));
        return ExitCodes.Success;
    }

    private static async Task<int> WriteRefOperationSuccessAsync(
        IAdactMcpClient client,
        string actionName,
        Dictionary<string, object?> operationArgs,
        string? sessionId,
        bool noSnapshot,
        string? snapshotDir,
        CancellationToken ct)
    {
        _ = actionName;
        _ = operationArgs;

        if (noSnapshot)
        {
            CliOutput.WriteYamlSuccess(metaFields: null, Array.Empty<KeyValuePair<string, string?>>());
            return ExitCodes.Success;
        }

        return await WriteSnapshotMetadataAndBodyAsync(client, sessionId, snapshotDir, Array.Empty<KeyValuePair<string, string?>>(), ct).ConfigureAwait(false);
    }

    private static async Task<int> WriteSessionOperationSuccessAsync(
        IAdactMcpClient client,
        string actionName,
        Dictionary<string, object?> operationArgs,
        string? sessionId,
        bool noSnapshot,
        string? snapshotDir,
        CancellationToken ct)
    {
        _ = actionName;
        _ = operationArgs;

        if (noSnapshot)
        {
            CliOutput.WriteYamlSuccess(metaFields: null, Array.Empty<KeyValuePair<string, string?>>());
            return ExitCodes.Success;
        }

        return await WriteSnapshotMetadataAndBodyAsync(client, sessionId, snapshotDir, Array.Empty<KeyValuePair<string, string?>>(), ct).ConfigureAwait(false);
    }

    private static async Task<int> WriteSnapshotMetadataAndBodyAsync(
        IAdactMcpClient client,
        string? sessionId,
        string? snapshotDir,
        IReadOnlyList<KeyValuePair<string, string?>> bodyFields,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);

        IReadOnlyDictionary<string, object?>? snapArgs = string.IsNullOrEmpty(sessionId)
            ? null
            : new Dictionary<string, object?> { ["sessionId"] = sessionId };

        var snapResult = await client.CallToolAsync("adact_snapshot", snapArgs, ct).ConfigureAwait(false);
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
            CliError.Write(ErrorCodes.InternalError, "adact_snapshot response missing sessionId.");
            return ExitCodes.CommandFailed;
        }

        var raw = ExtractSnapshotJsonText(snapResult, snapJson);
        string text;
        try
        {
            var (parsedMeta, parsedRoot) = SnapshotJsonParser.Parse(raw);
            var filtered = SnapshotTreeFilter.Apply(parsedRoot, SnapshotTreeFilter.FilterOperable);
            text = SnapshotTextFormatter.Format(parsedMeta, filtered, SnapshotTreeFilter.FilterOperable);
        }
        catch (JsonException ex)
        {
            CliError.Write(ErrorCodes.InternalError,
                $"Failed to parse snapshot response: {ex.Message}");
            return ExitCodes.CommandFailed;
        }

        var sidNum = ParseSidNumber(resolvedSid);
        var (path, isNew) = SnapshotFileWriter.Write(text, sidNum, snapshotDir);
        var snapshotPath = $"{path} {(isNew ? "(changed)" : "(unchanged)")}";
        CliOutput.WriteYamlSuccess(
            [CliOutput.Field("snapshotPath", snapshotPath)],
            bodyFields);
        return ExitCodes.Success;
    }

    private static string ExtractSnapshotTreeText(string snapshotText)
    {
        ArgumentNullException.ThrowIfNull(snapshotText);

        const string separator = "---\n";
        if (!snapshotText.StartsWith(separator, StringComparison.Ordinal))
        {
            return snapshotText;
        }

        var second = snapshotText.IndexOf(separator, separator.Length, StringComparison.Ordinal);
        if (second < 0)
        {
            return snapshotText;
        }

        return snapshotText[(second + separator.Length)..];
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
