using System.CommandLine;
using System.Text;
using System.Text.Json;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

internal static class CloseAllCommand
{
  public static Command Build()
  {
    var server = CommandHelpers.CreateServerOption();

    var cmd = new Command("close-all", "Close all attached windows (per-session result on stdout).");
    cmd.Options.Add(server);

    cmd.SetAction((parseResult, ct) =>
    {
      var serverArg = parseResult.GetValue(server);

      return CommandHelpers.RunWithClientAsync(
              serverArg,
              (client, token) => ExecuteAsync(client, token),
              ct);
    });

    return cmd;
  }

  private static async Task<int> ExecuteAsync(AdactMcpClient client, CancellationToken ct)
  {
    var result = await client.CallToolAsync("windows_close_all", arguments: null, ct).ConfigureAwait(false);

    var errorExit = McpResponse.TryReportError(result);
    if (errorExit is { } code) return code;

    var info = McpResponse.GetJson(result);
    var (output, exit) = FormatResults(info);
    Console.Out.Write(output);
    return exit;
  }

  /// <summary>
  /// <c>windows_close_all</c> レスポンスの <c>results</c> 配列を TSV 文字列 (ヘッダ無し) に変換する。
  /// 設計 §4.5 / §5.2。1 つでも fail があれば exit 1、すべて ok なら exit 0、空配列も exit 0。
  /// </summary>
  /// <returns>(stdout に書き出すべき TSV テキスト, exit code)</returns>
  internal static (string output, int exit) FormatResults(JsonElement info)
  {
    var sb = new StringBuilder();
    var hasFailure = false;

    if (info.ValueKind == JsonValueKind.Object
        && info.TryGetProperty("results", out var results)
        && results.ValueKind == JsonValueKind.Array)
    {
      foreach (var entry in results.EnumerateArray())
      {
        if (entry.ValueKind != JsonValueKind.Object) continue;

        var sid = JsonHelpers.GetStringOrNull(entry, "sessionId") ?? "";
        var resultStr = JsonHelpers.GetStringOrNull(entry, "result") ?? "";
        var error = JsonHelpers.GetStringOrNull(entry, "error");

        sb.Append(sid).Append('\t').Append(resultStr);
        if (!string.Equals(resultStr, "ok", StringComparison.Ordinal))
        {
          hasFailure = true;
          if (!string.IsNullOrEmpty(error))
          {
            sb.Append('\t').Append(error);
          }
        }
        sb.Append('\n');
      }
    }

    var exit = hasFailure ? ExitCodes.CommandFailed : ExitCodes.Success;
    return (sb.ToString(), exit);
  }
}
