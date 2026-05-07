using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// detach / close / kill の共通実装。設計 009 §4.5 / §5.2。
/// 出力は <c>sessionId &lt;sid&gt;</c> のあとに literal キー (例: detached / closed / killed) を順に出す。
/// </summary>
internal static class LifecycleCommandImpl
{
    /// <summary>
    /// detach / close / kill の共通実装。tool を呼び出し、成功時に sessionId + literal 行を stdout に出力する。
    /// </summary>
    /// <param name="client">接続済み MCP クライアント。</param>
    /// <param name="toolName">呼び出す MCP tool 名 (例: <c>adact_close_window</c>)。</param>
    /// <param name="sessionId">対象 session ID。null/空なら active session。</param>
    /// <param name="literalLines">sessionId 出力後に stdout へ追加する literal 行集合 (例: <c>closed</c>, <c>detached</c>)。</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>exit code (成功 0)。</returns>
    /// <exception cref="ArgumentNullException">必須引数が null。</exception>
    public static async Task<int> ExecuteAsync(
        IAdactMcpClient client,
        string toolName,
        string? sessionId,
        IReadOnlyList<string> literalLines,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(toolName);
        ArgumentNullException.ThrowIfNull(literalLines);

        IReadOnlyDictionary<string, object?>? args = string.IsNullOrEmpty(sessionId)
            ? null
            : new Dictionary<string, object?> { ["sessionId"] = sessionId };

        var result = await client.CallToolAsync(toolName, args, ct).ConfigureAwait(false);

        var errorExit = McpResponse.TryReportError(result);
        if (errorExit is { } code) return code;

        var info = McpResponse.GetJson(result);
        var resolvedSid = JsonHelpers.GetStringOrNull(info, "sessionId") ?? sessionId;
        if (string.IsNullOrEmpty(resolvedSid))
        {
            CliError.Write(ErrorCodes.InternalError, $"{toolName} response missing 'sessionId'.");
            return ExitCodes.CommandFailed;
        }

        var bodyFields = new List<KeyValuePair<string, string?>> { CliOutput.Field("sessionId", resolvedSid) };
        foreach (var line in literalLines)
        {
            bodyFields.Add(CliOutput.Field(line, "true"));
        }

        CliOutput.WriteYamlSuccess(metaFields: null, bodyFields);
        return ExitCodes.Success;
    }

    /// <summary>
    /// 追加引数とレスポンスフィールド読み取りをサポートする拡張版。
    /// </summary>
    /// <param name="client">接続済み MCP クライアント。</param>
    /// <param name="toolName">呼び出す MCP tool 名。</param>
    /// <param name="sessionId">対象 session ID。null/空なら active session。</param>
    /// <param name="extraArgs">sessionId 以外に tool へ渡す追加引数。</param>
    /// <param name="literalLines">sessionId 出力後に stdout へ追加する literal 行集合。</param>
    /// <param name="responseFields">JSON レスポンスから読み取って出力するフィールド名一覧。</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>exit code (成功 0)。</returns>
    public static async Task<int> ExecuteAsync(
        IAdactMcpClient client,
        string toolName,
        string? sessionId,
        IReadOnlyDictionary<string, object?>? extraArgs,
        IReadOnlyList<string> literalLines,
        IReadOnlyList<string>? responseFields,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(toolName);
        ArgumentNullException.ThrowIfNull(literalLines);

        var args = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(sessionId))
            args["sessionId"] = sessionId;
        if (extraArgs is not null)
        {
            foreach (var kv in extraArgs)
                args[kv.Key] = kv.Value;
        }

        var result = await client.CallToolAsync(toolName, args.Count > 0 ? args : null, ct).ConfigureAwait(false);

        var errorExit = McpResponse.TryReportError(result);
        if (errorExit is { } code) return code;

        var info = McpResponse.GetJson(result);
        var resolvedSid = JsonHelpers.GetStringOrNull(info, "sessionId") ?? sessionId;
        if (string.IsNullOrEmpty(resolvedSid))
        {
            CliError.Write(ErrorCodes.InternalError, $"{toolName} response missing 'sessionId'.");
            return ExitCodes.CommandFailed;
        }

        var bodyFields = new List<KeyValuePair<string, string?>> { CliOutput.Field("sessionId", resolvedSid) };
        foreach (var line in literalLines)
        {
            bodyFields.Add(CliOutput.Field(line, "true"));
        }
        if (responseFields is not null)
        {
            foreach (var field in responseFields)
            {
                var value = JsonHelpers.GetStringOrNull(info, field);
                if (value is not null)
                    bodyFields.Add(CliOutput.Field(field, value));
            }
        }

        CliOutput.WriteYamlSuccess(metaFields: null, bodyFields);
        return ExitCodes.Success;
    }
}
