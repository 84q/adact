using System.Net;
using System.Text.Json;

using Adact.Cli.Connection;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using Xunit;

namespace Adact.Cli.Tests.Integration;

/// <summary>
/// Verifies <see cref="AdactMcpClient"/> against an in-process MCP HTTP server.
/// </summary>
[Trait("Layer", "Integration")]
[Collection(AdactMcpClientCollection.Name)]
public sealed class AdactMcpClientIntegrationTests
{
    private readonly AdactMcpClientServerFixture _fixture;

    /// <summary>Creates the test class with the shared in-process MCP server fixture.</summary>
    public AdactMcpClientIntegrationTests(AdactMcpClientServerFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>ConnectAsync preserves the resolved endpoint while completing the MCP handshake.</summary>
    [Fact]
    public async Task ConnectAsync_OnLoopbackMcpServer_ReturnsClientWithEndpoint()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var endpoint = ServerEndpoint.Parse(_fixture.Endpoint.ToString());

        await using var client = await AdactMcpClient.ConnectAsync(endpoint, loggerFactory: null, cts.Token);

        Assert.Same(endpoint, client.Endpoint);
        Assert.Equal(_fixture.Endpoint, client.Endpoint.Url);
    }

    /// <summary>CallToolAsync sends arguments over HTTP and returns content plus structured content.</summary>
    [Fact]
    public async Task CallToolAsync_WithArguments_ReturnsServerResult()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var endpoint = ServerEndpoint.Parse(_fixture.Endpoint.ToString());
        await using var client = await AdactMcpClient.ConnectAsync(endpoint, loggerFactory: null, cts.Token);

        var result = await client.CallToolAsync(
            "test_echo",
            new Dictionary<string, object?>
            {
                ["message"] = "hello",
                ["repeat"] = 2,
            },
            cts.Token);

        Assert.False(result.IsError ?? false);
        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        Assert.Equal("hellohello", text);
        Assert.True(result.StructuredContent.HasValue);
        var structured = result.StructuredContent.Value;
        Assert.Equal("hello", structured.GetProperty("message").GetString());
        Assert.Equal(2, structured.GetProperty("repeat").GetInt32());
    }

    /// <summary>Tool-level error results are returned as MCP error payloads, not transport exceptions.</summary>
    [Fact]
    public async Task CallToolAsync_WhenToolReturnsError_PreservesErrorResult()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var endpoint = ServerEndpoint.Parse(_fixture.Endpoint.ToString());
        await using var client = await AdactMcpClient.ConnectAsync(endpoint, loggerFactory: null, cts.Token);

        var result = await client.CallToolAsync("test_error", arguments: null, cts.Token);

        Assert.True(result.IsError);
        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        Assert.Contains("error TEST_ERROR", text, StringComparison.Ordinal);
        Assert.True(result.StructuredContent.HasValue);
        Assert.Equal("TEST_ERROR", result.StructuredContent.Value.GetProperty("code").GetString());
    }

    /// <summary>DisposeAsync tears down the underlying SDK client so later tool calls cannot proceed.</summary>
    [Fact]
    public async Task DisposeAsync_AfterConnect_PreventsFurtherToolCalls()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var endpoint = ServerEndpoint.Parse(_fixture.Endpoint.ToString());
        var client = await AdactMcpClient.ConnectAsync(endpoint, loggerFactory: null, cts.Token);

        await client.DisposeAsync();

        var ex = await Record.ExceptionAsync(async () =>
            await client.CallToolAsync("test_echo", arguments: null, cts.Token));

        Assert.True(
            ex is ObjectDisposedException or TaskCanceledException,
            $"Expected a disposed client to reject tool calls, got {ex?.GetType().FullName ?? "no exception"}.");
    }

    [McpServerToolType]
    private sealed class TestTools
    {
        [McpServerTool(Name = "test_echo")]
        public static CallToolResult Echo(string message, int repeat)
        {
            var text = string.Concat(Enumerable.Repeat(message, repeat));
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = text }],
                StructuredContent = JsonSerializer.SerializeToElement(new
                {
                    message,
                    repeat,
                    text,
                }),
            };
        }

        [McpServerTool(Name = "test_error")]
        public static CallToolResult Error()
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "error TEST_ERROR\nmessage deterministic failure" }],
                StructuredContent = JsonSerializer.SerializeToElement(new
                {
                    code = "TEST_ERROR",
                    message = "deterministic failure",
                }),
            };
        }
    }

    /// <summary>Fixture that hosts a deterministic MCP server without GUI, UIA, or adact serve.</summary>
    public sealed class AdactMcpClientServerFixture : IAsyncLifetime
    {
        private const string McpPath = "/mcp";
        private WebApplication? _app;

        /// <summary>The MCP endpoint, including the /mcp path.</summary>
        public Uri Endpoint { get; private set; } = null!;

        /// <summary>Starts the in-process Streamable HTTP MCP server on an ephemeral loopback port.</summary>
        public async Task InitializeAsync()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, port: 0));
            builder.Logging.ClearProviders();
            builder.Services
                .AddMcpServer(options =>
                {
                    options.ServerInfo = new Implementation
                    {
                        Name = "adact-cli-test",
                        Version = "1.0.0",
                    };
                })
                .WithHttpTransport(options => options.Stateless = true)
                .WithTools<TestTools>();

            _app = builder.Build();
            _app.MapMcp(McpPath);
            await _app.StartAsync();

            var url = _app.Urls.FirstOrDefault()
                ?? throw new InvalidOperationException("Failed to determine bound URL for the test MCP server.");
            Endpoint = new Uri(new Uri(url), McpPath);
        }

        /// <summary>Stops and disposes the in-process MCP server.</summary>
        public async Task DisposeAsync()
        {
            if (_app is null) return;

            try { await _app.StopAsync(); } catch { }
            await _app.DisposeAsync();
            _app = null;
        }
    }
}

/// <summary>Serializes tests that share the in-process MCP HTTP server.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AdactMcpClientCollection : ICollectionFixture<AdactMcpClientIntegrationTests.AdactMcpClientServerFixture>
{
    /// <summary>The xUnit collection name for AdactMcpClient integration tests.</summary>
    public const string Name = "AdactMcpClient";
}
