using System.CommandLine;
using System.Globalization;
using System.Text.Json;

using Adact.Cli.Connection;
using Adact.Cli.Output;
using Adact.Cli.Snapshots;

using ModelContextProtocol.Protocol;

namespace Adact.Cli.Commands;

internal static class CommandHelpers
{
  /// <summary>
  /// 接続解決 → MCP 接続 → コマンド実装の呼び出し、を共通化する。
  /// 解決失敗 → INVALID_ARGUMENT (exit 2)、接続失敗 → CONNECTION_FAILED (exit 3)、
  /// その他 → INTERNAL_ERROR (exit 1)。
  /// </summary>
  public static async Task<int> RunWithClientAsync(
      string? serverArg,
      Func<AdactMcpClient, CancellationToken, Task<int>> exec,
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
      await using var client = await AdactMcpClient.ConnectAsync(endpoint, loggerFactory: null, ct).ConfigureAwait(false);
      return await exec(client, ct).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      return ConnectionErrors.ReportAndReturnExitCode(ex, endpoint);
    }
  }

  /// <summary>
  /// Phase 5 #3 (CLI 骨格) のスタブ用ヘルパ。各コマンドの本実装は #4-#8 で順次差し替わるため、
  /// それまでの暫定として ErrorCodes.InternalError + "not implemented yet" メッセージを返す。
  /// </summary>
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
  /// <returns>exit code (成功時 0)。エラー時は <see cref="McpResponse.TryReportError"/> 経由で stderr 出力 + 1。</returns>
  public static async Task<int> WriteSnapshotResultAsync(
      AdactMcpClient client,
      string? sessionId,
      string? snapshotDir,
      CancellationToken ct,
      bool writeSessionId = true,
      string? filter = null)
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
    var path = SnapshotFileWriter.Write(text, sidNum, snapshotDir);

    if (writeSessionId)
    {
      KeyValueWriter.Write("sessionId", resolvedSid);
    }
    KeyValueWriter.Write("snapshot", path);
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
      AdactMcpClient client,
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

  private static int ParseSidNumber(string sessionId)
  {
    if (sessionId.Length >= 2 && sessionId[0] == 's'
        && int.TryParse(sessionId.AsSpan(1), out var n))
    {
      return n;
    }
    return 0;
  }

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
