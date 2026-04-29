using System.CommandLine;

using Adact.Cli.Output;
using Adact.Cli.Server;

namespace Adact.Cli.Commands;

internal static class ServeCommand
{
    private const int DefaultPort = 41300;

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

        var cmd = new Command("serve", "Run as an HTTP MCP server on 127.0.0.1:<port> (default 41300).");
        cmd.Options.Add(port);

        cmd.SetAction(async (parseResult, ct) =>
        {
            var p = parseResult.GetValue(port);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            try
            {
                return await HttpHost.RunAsync(p, cts.Token).ConfigureAwait(false);
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
