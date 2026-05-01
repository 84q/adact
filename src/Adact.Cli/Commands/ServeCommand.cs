using System.CommandLine;

using Adact.Cli.Output;
using Adact.Cli.Server;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>serve</c> サブコマンド。HTTP transport で MCP daemon を 127.0.0.1:&lt;port&gt; (既定 41300) 上で起動する。
/// </summary>
internal static class ServeCommand
{
    /// <summary>--port 未指定時の既定ポート。</summary>
    private const int DefaultPort = 41300;

    /// <summary>--host 未指定時の既定ホスト。</summary>
    private const string DefaultHost = "127.0.0.1";

    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>serve サブコマンド。</returns>
    public static Command Build()
    {
        var port = new Option<int>("--port")
        {
            Description = "TCP port for the HTTP MCP listener (0-65535).",
            DefaultValueFactory = _ => DefaultPort,
        };
        port.Validators.Add(static result =>
        {
            // パース失敗 ("abc" 等) は System.CommandLine の標準エラーに委ねる。
            // GetValueOrDefault<int>() は変換失敗時に例外を投げるため、ここで弾く。
            if (result.Tokens.Count > 0 && !int.TryParse(result.Tokens[0].Value, out _))
            {
                return;
            }

            var value = result.GetValueOrDefault<int>();
            if (value < 0 || value > 65535)
            {
                result.AddError($"--port value '{value}' is out of range (expected 0-65535).");
            }
        });

        var host = new Option<string>("--host")
        {
            Description = "IP address to bind the HTTP listener to (default 127.0.0.1). Use 0.0.0.0 to listen on all interfaces.",
            DefaultValueFactory = _ => DefaultHost,
        };

        var cmd = new Command("serve", "Run as an HTTP MCP server on <host>:<port> (default 127.0.0.1:41300).");
        cmd.Options.Add(port);
        cmd.Options.Add(host);

        cmd.SetAction(async (parseResult, ct) =>
        {
            var p = parseResult.GetValue(port);
            var h = parseResult.GetValue(host);

            if (!System.Net.IPAddress.TryParse(h, out var ipAddress))
            {
                CliError.Write(ErrorCodes.InvalidArgument, $"--host '{h}' is not a valid IP address.");
                return ExitCodes.UserError;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            try
            {
                return await HttpHost.RunAsync(ipAddress, p, cts.Token).ConfigureAwait(false);
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
