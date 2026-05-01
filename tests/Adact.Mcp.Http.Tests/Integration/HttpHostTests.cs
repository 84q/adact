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

/// <summary>
/// HTTP MCP host の構成を、実 GUI / 実 UIA に触らず検証するテスト。
/// </summary>
[Trait("Layer", "Integration")]
public sealed class HttpHostTests
{
    /// <summary>
    /// <see cref="HttpHost.BuildApplication"/> が MCP endpoint と主要 singleton を構成することを確認する。
    /// </summary>
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

    /// <summary>
    /// HTTP mode の daemon control が host lifetime に停止要求を出すことを確認する。
    /// </summary>
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
