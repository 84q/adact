using System.Net;

using Adact.Engine;
using Adact.Mcp.Common;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Adact.Cli.Server;

/// <summary>
/// </summary>
public static class HttpHost
{
    public const string McpPath = "/mcp";

    /// <summary>
    /// </summary>
    public const int ExitCodeEnvironmentNotSupported = 4;

    /// <summary>
    /// </summary>
    /// <returns>
    /// </returns>
    /// <remarks>
    /// </remarks>
    public static async Task<int> RunAsync(IPAddress hostAddress, int port, CancellationToken ct)
    {
        if (!EnsureInteractiveSession())
        {
            return ExitCodeEnvironmentNotSupported;
        }

        var app = BuildApplication(hostAddress, port);
        await app.RunAsync(ct).ConfigureAwait(false);
        return 0;
    }

    /// <summary>
    /// </summary>
    private static bool EnsureInteractiveSession()
    {
        var probe = InteractiveSessionGuard.Probe();
        if (!probe.Ok)
        {
            Console.Error.WriteLine($"error {InteractiveSessionGuard.ErrorCode}");
            Console.Error.WriteLine($"message {probe.Message}");
            Console.Error.WriteLine("hint launch 'adact serve' from the interactive logon session that owns the target GUI windows");
            return false;
        }

        Console.Error.WriteLine(
            $"info interactive session ok (SessionId={probe.SessionId}, WindowStation={probe.WindowStationName})");
        return true;
    }

    /// <summary>
    /// </summary>
    /// <remarks>
    /// </remarks>
    public static WebApplication BuildApplication(IPAddress hostAddress, int port)
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(hostAddress, port);
        });

        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(o =>
        {
            o.SingleLine = true;
            o.IncludeScopes = false;
        });
        builder.Services.Configure<ConsoleLoggerOptions>(o =>
            o.LogToStandardErrorThreshold = LogLevel.Trace);

        builder.Services.AddSingleton<UiaEngine>(sp =>
            new UiaEngine(sp.GetRequiredService<ILoggerFactory>()));
        builder.Services.AddSingleton<SessionStore>();
        builder.Services.AddSingleton<WindowRefStore>();
        builder.Services.AddSingleton<IDaemonControl, HttpDaemonControl>();

        builder.Services
            .AddMcpServer(o =>
            {
                o.ServerInfo = new ModelContextProtocol.Protocol.Implementation
                {
                    Name = "adact",
                    Version = ThisAssemblyVersion(),
                };
            })
            .WithHttpTransport(o => o.Stateless = true)
            .WithTools<WindowsTools>();

        var app = builder.Build();
        app.MapMcp(McpPath); // Phase 5: map to /mcp (009 §2.2)
        return app;
    }

    /// <summary>
    /// </summary>
    private static string ThisAssemblyVersion()
    {
        var v = typeof(HttpHost).Assembly.GetName().Version;
        return v?.ToString() ?? "0.0.0";
    }
}
