using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// detach / close / kill の共通実装。設計 009 §4.5 / §5.2。
/// 出力は <c>sessionId &lt;sid&gt;</c> のあとに literal キー (例: detached / closed / killed) を順に出す。
/// </summary>
internal static class LifecycleCommandImpl
{
  public static async Task<int> ExecuteAsync(
      AdactMcpClient client,
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

    KeyValueWriter.Write("sessionId", resolvedSid);
    foreach (var line in literalLines)
    {
      Console.Out.WriteLine(line);
    }
    return ExitCodes.Success;
  }
}
