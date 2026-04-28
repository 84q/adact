using System.CommandLine;
using System.Net.Http;
using System.Net.Sockets;

using Adact.Cli.Connection;
using Adact.Cli.Output;

using ModelContextProtocol.Protocol;

namespace Adact.Cli.Commands;

internal static class DaemonStopCommand
{
  public static Command Build()
  {
    var server = CommandHelpers.CreateServerOption();

    var cmd = new Command("daemon-stop", "Stop a local HTTP MCP daemon gracefully.");
    cmd.Options.Add(server);

    cmd.SetAction((parseResult, ct) =>
    {
      var serverArg = parseResult.GetValue(server);
      return RunAsync(serverArg, ct);
    });

    return cmd;
  }

  private static async Task<int> RunAsync(string? serverArg, CancellationToken ct)
  {
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

    // 設計 §3.4 / §6.3: daemon-stop はローカル接続専用。host が localhost 以外なら
    // MCP 呼び出しを行わずに LOCAL_ONLY エラーで終了する。
    if (!endpoint.IsLocalhost)
    {
      CliError.Write(
          ErrorCodes.LocalOnly,
          $"daemon-stop requires a localhost target, but '{endpoint.Url.Host}' is not local.",
          "run 'adact daemon-stop' on the same host as 'adact serve', or omit --server to use the default localhost endpoint.");
      return ExitCodes.UserError;
    }

    AdactMcpClient? client = null;
    try
    {
      try
      {
        client = await AdactMcpClient.ConnectAsync(endpoint, loggerFactory: null, ct).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        return ConnectionErrors.ReportAndReturnExitCode(ex, endpoint);
      }

      CallToolResult result;
      try
      {
        result = await client.CallToolAsync("daemon_stop", arguments: null, ct).ConfigureAwait(false);
      }
      catch (Exception ex) when (!ct.IsCancellationRequested && IsConnectionDropException(ex))
      {
        // daemon_stop の応答前に daemon が落ちて HTTP セッションが切断されるケース。
        // 設計 §4.5 / §6.x: 切断は「既に停止した」と見なし success 扱い。
        Console.Out.WriteLine("stopped");
        return ExitCodes.Success;
      }

      var errorExit = McpResponse.TryReportError(result);
      if (errorExit is { } code) return code;

      Console.Out.WriteLine("stopped");
      return ExitCodes.Success;
    }
    finally
    {
      // daemon_stop に成功すると daemon プロセスが直ちに終了し、HTTP セッションが
      // 閉じられている可能性が高い。Dispose 時の接続切断系例外は「既に停止した」
      // と見なして握り潰す。設計 §4.5 / §6.x。
      if (client is not null)
      {
        try
        {
          await client.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (IsConnectionDropException(ex))
        {
          // ignore
        }
      }
    }
  }

  /// <summary>
  /// daemon との HTTP セッション切断に伴う例外を判定する。
  /// CancellationToken 系 (OperationCanceledException / TaskCanceledException) はユーザの
  /// Ctrl+C 由来で発生しうるため除外する (設計 009 §6.x、Phase5 #8 m2 指摘)。
  /// </summary>
  internal static bool IsConnectionDropException(Exception ex)
  {
    for (var cur = ex; cur is not null; cur = cur.InnerException)
    {
      if (cur is HttpRequestException
          || cur is SocketException
          || cur is IOException
          || cur is ObjectDisposedException)
      {
        return true;
      }
    }
    return false;
  }
}
