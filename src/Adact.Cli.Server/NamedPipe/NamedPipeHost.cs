using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

using Adact.Engine;
using Adact.Mcp.Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Adact.Cli.Server.NamedPipe;

/// <summary>
/// </summary>
public static class NamedPipeHost
{
    /// <summary>
    /// </summary>
    public const int ExitCodeEnvironmentNotSupported = 4;

    private const int ConnectionRetryDelayMs = 100;

    /// <summary>
    /// </summary>
    /// <returns>
    /// </returns>
    /// <remarks>
    /// </remarks>
    public static async Task<int> RunAsync(string pipeName, CancellationToken ct)
    {
        if (!EnsureInteractiveSession())
        {
            return ExitCodeEnvironmentNotSupported;
        }

        var loggerFactory = CreateLoggerFactory();
        var logger = loggerFactory.CreateLogger(typeof(NamedPipeHost));

        logger.LogInformation("Starting Named Pipe MCP server on {PipeName}", pipeName);

        try
        {
            await RunServerAsync(pipeName, loggerFactory, ct).ConfigureAwait(false);
            return 0;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Server stopped (cancelled)");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Server error");
            Console.Error.WriteLine($"error INTERNAL_ERROR");
            Console.Error.WriteLine($"message {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// </summary>
    private static async Task RunServerAsync(string pipeName, ILoggerFactory loggerFactory, CancellationToken ct)
    {
        var shortPipeName = pipeName;
        if (shortPipeName.StartsWith(@"\\.\pipe\", StringComparison.Ordinal))
        {
            shortPipeName = shortPipeName[@"\\.\pipe\".Length..];
        }

        var services = new ServiceCollection();
        services.AddSingleton(loggerFactory);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.AddSingleton<UiaEngine>(sp => new UiaEngine(sp.GetRequiredService<ILoggerFactory>()));
        services.AddSingleton<SessionStore>();
        services.AddSingleton<WindowRefStore>();
        using var parentServiceProvider = services.BuildServiceProvider();

        var connections = new ConcurrentDictionary<Guid, NamedPipeConnection>();

        using var serverCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var serverCt = serverCts.Token;

        try
        {
            Console.WriteLine($"### Success");
            Console.WriteLine($"Daemon listening on {pipeName}");

            while (!serverCt.IsCancellationRequested)
            {
                var pipeStream = NamedPipeServerStreamAcl.Create(
                    shortPipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous,
                    inBufferSize: 0,
                    outBufferSize: 0,
                    pipeSecurity: CreateCurrentUserOnlyPipeSecurity());

                try
                {
                    await pipeStream.WaitForConnectionAsync(serverCt).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    pipeStream.Dispose();
                    break;
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger(typeof(NamedPipeHost))
                        .LogError(ex, "Error waiting for pipe connection");
                    pipeStream.Dispose();
                    await Task.Delay(ConnectionRetryDelayMs, serverCt).ConfigureAwait(false);
                    continue;
                }

                var connectionLogger = loggerFactory.CreateLogger<NamedPipeConnection>();
                var connection = new NamedPipeConnection(pipeStream, connectionLogger, serverCts);

                connections.TryAdd(connection.ConnectionId, connection);

                _ = HandleConnectionAsync(connection, connections, parentServiceProvider, serverCt);
            }
        }
        finally
        {
            foreach (var conn in connections.Values)
            {
                try
                {
                    await conn.DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                }
            }

            connections.Clear();
        }
    }

    /// <summary>
    /// </summary>
    private static async Task HandleConnectionAsync(
        NamedPipeConnection connection,
        ConcurrentDictionary<Guid, NamedPipeConnection> connections,
        IServiceProvider parentServiceProvider,
        CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            await connection.RunAsync(parentServiceProvider, cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            parentServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(NamedPipeHost))
                .LogError(ex, "Error handling connection {ConnectionId}", connection.ConnectionId);
        }
        finally
        {
            connections.TryRemove(connection.ConnectionId, out _);
            await connection.DisposeAsync().ConfigureAwait(false);
        }
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
            Console.Error.WriteLine("hint launch 'adact serve pipe' from the interactive logon session that owns the target GUI windows");
            return false;
        }

        Console.Error.WriteLine(
            $"info interactive session ok (SessionId={probe.SessionId}, WindowStation={probe.WindowStationName})");
        return true;
    }

    /// <summary>
    /// </summary>
    private static ILoggerFactory CreateLoggerFactory()
    {
        return LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.IncludeScopes = false;
            });
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }

    /// <summary>
    /// </summary>
    private static PipeSecurity CreateCurrentUserOnlyPipeSecurity()
    {
        var pipeSecurity = new PipeSecurity();
        pipeSecurity.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        var currentUser = WindowsIdentity.GetCurrent().User
            ?? throw new InvalidOperationException("Failed to resolve current Windows user SID.");

        pipeSecurity.AddAccessRule(new PipeAccessRule(
            currentUser,
            PipeAccessRights.FullControl,
            AccessControlType.Allow));

        return pipeSecurity;
    }

}
