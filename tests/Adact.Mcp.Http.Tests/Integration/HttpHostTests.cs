using System.Net;

using Adact.Cli.Server;
using Adact.Engine;
using Adact.Mcp.Common;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Xunit;

namespace Adact.Mcp.Http.Tests.Integration;

/// <summary>Contains tests for the Http Host behavior.</summary>
[Trait("Layer", "Integration")]
public sealed class HttpHostTests
{
    /// <summary>Performs the Build Application Configures Mcp Route And Core Services operation.</summary>
    [Fact]
    public void BuildApplication_ConfiguresMcpRouteAndCoreServices()
    {
        using var app = HttpHost.BuildApplication(IPAddress.Loopback, 0);

        var routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(e => e.RoutePattern.RawText?.TrimEnd('/'));

        Assert.Contains(HttpHost.McpPath, routes);
        Assert.IsType<UiaEngine>(app.Services.GetRequiredService<UiaEngine>());
        Assert.IsType<SessionStore>(app.Services.GetRequiredService<SessionStore>());
        Assert.IsType<WindowRefStore>(app.Services.GetRequiredService<WindowRefStore>());

        var daemonControl = app.Services.GetRequiredService<IDaemonControl>();
        Assert.True(daemonControl.IsSupported);
    }

    /// <summary>Performs the Daemon Control Stop Async Requests Application Stop operation.</summary>
    [Fact]
    public async Task DaemonControl_StopAsync_RequestsApplicationStop()
    {
        await using var app = HttpHost.BuildApplication(IPAddress.Loopback, 0);
        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        var daemonControl = app.Services.GetRequiredService<IDaemonControl>();

        await daemonControl.StopAsync(new CancellationToken(canceled: true));

        Assert.True(lifetime.ApplicationStopping.IsCancellationRequested);
    }
}
