using System.CommandLine;

using Adact.Cli.Connection;
using Adact.Cli.Output;
using Adact.Cli.Server.NamedPipe;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>serve pipe</c> サブコマンド。Named Pipe transport で MCP daemon を起動する。
/// パイプ名はワークスペースハッシュから自動生成される。
/// </summary>
internal static class ServePipeCommand
{
    /// <summary>
    /// サーバーが既に起動しているか確認する関数。
    /// テストでモック可能にするため internal static プロパティとして公開する。
    /// </summary>
    internal static Func<NamedPipeEndPoint, int, CancellationToken, Task<bool>> IsServerRunningAsync { get; set; }
        = NamedPipeMcpClient.IsServerRunningAsync;

    /// <summary>
    /// Named Pipe MCP サーバーを起動する関数。
    /// テストでモック可能にするため internal static プロパティとして公開する。
    /// </summary>
    internal static Func<string, CancellationToken, Task<int>> RunNamedPipeHostAsync { get; set; }
        = NamedPipeHost.RunAsync;

    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>serve pipe サブコマンド。</returns>
    public static Command Build()
    {
        var cmd = new Command("pipe", "Run as a Named Pipe MCP server. Pipe name is auto-generated from workspace hash. (--server option is ignored for this command.)");

        cmd.SetAction(async (parseResult, ct) =>
        {
            // ワークスペースパスを解決
            var workspacePath = NamedPipeEndPoint.ResolveWorkspacePath();
            var endpoint = NamedPipeEndPoint.FromWorkspacePath(workspacePath);

            // 起動前にパイプの存在確認を行う
            if (await IsServerRunningAsync(endpoint, 100, ct).ConfigureAwait(false))
            {
                CliError.Write(
                    ErrorCodes.AlreadyRunning,
                    "A daemon is already running for this workspace.",
                    "Use 'adact daemon-stop' to stop the existing daemon first.");
                return ExitCodes.CommandFailed;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            try
            {
                return await RunNamedPipeHostAsync(endpoint.PipeName, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                CliError.Write(ErrorCodes.InternalError, ex.Message);
                return ExitCodes.CommandFailed;
            }
        });

        return cmd;
    }
}
