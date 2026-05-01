using Adact.Cli.Server;
using Adact.Engine;
using Adact.Mcp.Common;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Xunit;

namespace Adact.Cli.Tests.Integration;

/// <summary>
/// Verifies the HTTP MCP host composition without starting adact serve, GUI, or UIA.
/// </summary>
[Trait("Layer", "Integration")]
public sealed class HttpHostIntegrationTests
{
    /// <summary>BuildApplication maps the MCP endpoint on /mcp.</summary>
    [Fact]
    public async Task BuildApplication_WhenBuilt_MapsMcpRoute()
    {
        await using var app = HttpHost.BuildApplication(port: 0);
        var routeBuilder = (IEndpointRouteBuilder)app;

        var routePatterns = routeBuilder.DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToArray();

        Assert.Contains(routePatterns, pattern =>
            string.Equals(pattern, HttpHost.McpPath, StringComparison.Ordinal)
            || (pattern?.StartsWith(HttpHost.McpPath + "/", StringComparison.Ordinal) ?? false));
    }

    /// <summary>BuildApplication registers the shared services used by the MCP HTTP tools.</summary>
    [Fact]
    public async Task BuildApplication_WhenBuilt_RegistersRequiredSingletonServices()
    {
        await using var app = HttpHost.BuildApplication(port: 0);
        var services = app.Services;

        Assert.Same(
            services.GetRequiredService<UiaEngine>(),
            services.GetRequiredService<UiaEngine>());
        Assert.Same(
            services.GetRequiredService<SessionStore>(),
            services.GetRequiredService<SessionStore>());
        Assert.Same(
            services.GetRequiredService<WindowRefStore>(),
            services.GetRequiredService<WindowRefStore>());
        Assert.Same(
            services.GetRequiredService<IDaemonControl>(),
            services.GetRequiredService<IDaemonControl>());
    }

    /// <summary>The HTTP daemon control requests host shutdown through IHostApplicationLifetime.</summary>
    [Fact]
    public async Task StopAsync_WhenCalled_RequestsHostApplicationStop()
    {
        await using var app = HttpHost.BuildApplication(port: 0);
        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        var control = app.Services.GetRequiredService<IDaemonControl>();

        Assert.True(control.IsSupported);
        Assert.False(lifetime.ApplicationStopping.IsCancellationRequested);

        await control.StopAsync(new CancellationToken(canceled: true));

        Assert.True(lifetime.ApplicationStopping.IsCancellationRequested);
    }
}
