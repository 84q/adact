using System.CommandLine;
using System.Text.Json;
using System.IO.Pipes;

using Adact.Cli.Connection;
using Adact.Cli.Output;
using Adact.Cli.Snapshots;

using ModelContextProtocol.Protocol;

namespace Adact.Cli.Commands;

/// <summary>
/// Provides shared helpers for CLI commands that call the MCP server.
/// </summary>
internal static class CommandHelpers
{
    private static readonly AsyncLocal<CommandRuntime?> RuntimeOverride = new();
    private static readonly CommandRuntime DefaultRuntime = CommandRuntime.CreateDefault();
    private static CommandRuntime Runtime => RuntimeOverride.Value ?? DefaultRuntime;

    private static readonly TimeSpan AutoStartReconnectRetryDelay = TimeSpan.FromMilliseconds(150);
    private const int AutoStartReconnectRetryCount = 5;

    /// <summary>
    /// Executes a command with a connected MCP client.
    /// </summary>
    /// <param name="serverArg">The optional server endpoint override.</param>
    /// <param name="exec">The operation to run with the connected client.</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>exit code。</returns>
    public static async Task<int> RunWithClientAsync(
        string? serverArg,
        Func<IAdactMcpClient, CancellationToken, Task<int>> exec,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(exec);

        var httpEndpoint = ConnectionResolver.ResolveHttpEndpoint(serverArg);

        if (httpEndpoint is not null)
        {
            return await RunWithHttpClientAsync(httpEndpoint, exec, ct).ConfigureAwait(false);
        }
        else
        {
            var pipeEndpoint = ConnectionResolver.ResolveNamedPipeEndpoint();
            return await RunWithNamedPipeClientAsync(pipeEndpoint, exec, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
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
            return ReportNamedPipeConnectionFailed(endpoint, ex.Message);
        }
        catch (IOException ex)
        {
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
    /// Executes a command with a connected MCP client and auto-start support for named-pipe servers.
    /// </summary>
    /// <param name="serverArg">The optional server endpoint override.</param>
    /// <param name="exec">The operation to run with the connected client.</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>exit code。</returns>
    public static async Task<int> RunWithClientAndAutoStartAsync(
        string? serverArg,
        Func<IAdactMcpClient, CancellationToken, Task<int>> exec,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(exec);

        var httpEndpoint = ConnectionResolver.ResolveHttpEndpoint(serverArg);
        if (httpEndpoint is not null)
        {
            return await RunWithHttpClientAsync(httpEndpoint, exec, ct).ConfigureAwait(false);
        }

        var pipeEndpoint = ConnectionResolver.ResolveNamedPipeEndpoint();

        var isRunning = await Runtime.IsServerRunningAsync(pipeEndpoint, 100, ct).ConfigureAwait(false);

        if (isRunning)
        {
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
    /// </summary>
    public static int NotYetImplemented(string commandName)
    {
        CliError.Write(
            ErrorCodes.InternalError,
            $"{commandName}: not implemented yet (Phase 5 in progress).");
        return ExitCodes.CommandFailed;
    }

    /// <summary>
    /// </summary>
    public static readonly Option<string?> ServerOption = new("--server")
    {
        Description = "Connection target URL (e.g. http://127.0.0.1:41300/mcp). "
            + "Falls back to .adact/config.json or the default endpoint.",
        Recursive = true,
    };

    /// <summary>
    /// Captures a snapshot and writes the formatted result to CLI output.
    /// </summary>
    /// <param name="client">The connected MCP client.</param>
    /// <param name="sessionId">The optional session id to snapshot.</param>
    /// <param name="snapshotDir">The optional directory where snapshot files are written.</param>
    /// <param name="ct">cancellation token。</param>
    /// <param name="writeSessionId"><see langword="true"/> to include the session id in YAML output.</param>
    /// <param name="filter">The snapshot filter name.</param>
    /// <param name="writeContentToStdout">
    /// <see langword="true"/> to print the formatted snapshot body to standard output.
    /// </param>
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
    /// Runs an element-ref tool call and emits the follow-up snapshot unless disabled.
    /// </summary>
    /// <param name="client">The connected MCP client.</param>
    /// <param name="actionName">The user-facing action name.</param>
    /// <param name="operationToolName">The MCP tool name to invoke.</param>
    /// <param name="operationArgs">The tool arguments.</param>
    /// <param name="elementRef">The target element reference.</param>
    /// <param name="noSnapshot"><see langword="true"/> to skip the auto-snapshot output.</param>
    /// <param name="snapshotDir">The optional directory where snapshot files are written.</param>
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
    /// Runs a session-scoped tool call and emits the follow-up snapshot unless disabled.
    /// </summary>
    /// <param name="client">The connected MCP client.</param>
    /// <param name="actionName">The user-facing action name.</param>
    /// <param name="operationToolName">The MCP tool name to invoke.</param>
    /// <param name="operationArgs">The tool arguments.</param>
    /// <param name="sessionId">The optional target session id.</param>
    /// <param name="noSnapshot"><see langword="true"/> to skip the auto-snapshot output.</param>
    /// <param name="snapshotDir">The optional directory where snapshot files are written.</param>
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
