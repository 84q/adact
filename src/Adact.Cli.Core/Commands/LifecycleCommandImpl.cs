using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// Provides shared implementations for lifecycle-oriented CLI commands.
/// </summary>
internal static class LifecycleCommandImpl
{
    /// <summary>
    /// Executes a lifecycle command with an optional session id.
    /// </summary>
    /// <param name="client">The connected MCP client.</param>
    /// <param name="toolName">The MCP tool name to invoke.</param>
    /// <param name="sessionId">The optional target session id.</param>
    /// <param name="literalLines">Additional success fields to write with a value of <c>true</c>.</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>The command exit code.</returns>
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
    /// Executes a lifecycle command with optional extra request and response fields.
    /// </summary>
    /// <param name="client">The connected MCP client.</param>
    /// <param name="toolName">The MCP tool name to invoke.</param>
    /// <param name="sessionId">The optional target session id.</param>
    /// <param name="extraArgs">Additional request arguments to send.</param>
    /// <param name="literalLines">Additional success fields to write with a value of <c>true</c>.</param>
    /// <param name="responseFields">Response field names to copy into the success output.</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>The command exit code.</returns>
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
