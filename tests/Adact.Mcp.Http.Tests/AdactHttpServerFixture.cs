using System.Net;

using Adact.Cli.Server;
using Adact.Tests.Common;

using Microsoft.AspNetCore.Builder;

using Xunit;

namespace Adact.Mcp.Http.Tests;

/// <summary>Provides a shared fixture for tests.</summary>
public sealed class AdactHttpServerFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);

    private WebApplication? _app;
    private bool _usesExternalServer;

    /// <summary>Gets or sets the Base Address value.</summary>
    public Uri BaseAddress { get; private set; } = null!;

    /// <summary>Gets a value indicating whether Uses External Server.</summary>
    public bool UsesExternalServer => _usesExternalServer;

    /// <summary>Initializes the fixture.</summary>
    public async Task InitializeAsync()
    {
        var externalBaseAddress = GetExternalServerUri();
        if (externalBaseAddress is not null)
        {
            BaseAddress = externalBaseAddress;
            _usesExternalServer = true;
            await WaitForReadyAsync(BaseAddress, StartupTimeout).ConfigureAwait(false);
            return;
        }

        _app = HttpHost.BuildApplication(IPAddress.Loopback, 0);
        await _app.StartAsync();
        var url = _app.Urls.FirstOrDefault()
            ?? throw new InvalidOperationException("Failed to determine bound URL for the test HTTP server.");
        BaseAddress = new Uri(new Uri(url), HttpHost.McpPath);
    }

    /// <summary>Releases resources.</summary>
    public async Task DisposeAsync()
    {
        if (_usesExternalServer) return;

        if (_app is not null)
        {
            try { await _app.StopAsync(); } catch { }
            await _app.DisposeAsync();
            _app = null;
        }
    }

    internal static Uri? GetExternalServerUri()
        => GetExternalServerUri(Environment.GetEnvironmentVariable);

    internal static Uri? GetExternalServerUri(Func<string, string?> getEnvironmentVariable)
    {
        return ExternalServerHelper.GetExternalServerUri(getEnvironmentVariable);
    }

    private static async Task WaitForReadyAsync(Uri baseAddress, TimeSpan timeout)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Exception? last = null;
        while (sw.Elapsed < timeout)
        {
            try
            {
                using var _ = await http.GetAsync(baseAddress).ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                await Task.Delay(200).ConfigureAwait(false);
            }
        }

        throw new TimeoutException(
            $"External adact HTTP daemon did not become ready within {timeout.TotalSeconds:F0}s on {baseAddress}.",
            last);
    }
}

/// <summary>Defines a shared test collection.</summary>
[CollectionDefinition("AdactHttp", DisableParallelization = true)]
public sealed class AdactHttpCollection : ICollectionFixture<AdactHttpServerFixture>
{
}
