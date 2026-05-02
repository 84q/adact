using Adact.Tests.Common;

using Xunit;

namespace Adact.Mcp.Http.Tests;

/// <summary>
/// Verifies external daemon URL resolution for HTTP MCP smoke / E2E fixtures.
/// </summary>
[Trait("Layer", "Unit")]
[Collection(EnvironmentCollection.Name)]
public class AdactHttpServerFixtureTests
{
    /// <summary>Unset environment variable keeps the default in-process server behavior.</summary>
    [Fact]
    public void GetExternalServerUri_WhenUnset_ReturnsNull()
    {
        using var _ = new EnvironmentVariableScope(ExternalServerHelper.ServerUrlEnvironmentVariable, null);

        Assert.Null(AdactHttpServerFixture.GetExternalServerUri());
    }

    /// <summary>A configured HTTP MCP endpoint is trimmed and returned as the fixture base URL.</summary>
    [Fact]
    public void GetExternalServerUri_WhenSet_ReturnsTrimmedAbsoluteUrl()
    {
        using var _ = new EnvironmentVariableScope(
            ExternalServerHelper.ServerUrlEnvironmentVariable,
            "  http://127.0.0.1:41300/mcp  ");

        Assert.Equal(new Uri("http://127.0.0.1:41300/mcp"), AdactHttpServerFixture.GetExternalServerUri());
    }

    /// <summary>Invalid endpoint values fail fast with a message that names the environment variable.</summary>
    [Theory]
    [InlineData("127.0.0.1:41300/mcp")]
    [InlineData("ftp://127.0.0.1:41300/mcp")]
    [InlineData("not a url")]
    public void GetExternalServerUri_WhenInvalid_Throws(string value)
    {
        using var _ = new EnvironmentVariableScope(ExternalServerHelper.ServerUrlEnvironmentVariable, value);

        var ex = Assert.Throws<InvalidOperationException>(() => AdactHttpServerFixture.GetExternalServerUri());
        Assert.Contains(ExternalServerHelper.ServerUrlEnvironmentVariable, ex.Message, StringComparison.Ordinal);
    }
}

/// <summary>
/// Serializes tests that mutate process-wide environment variables.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class EnvironmentCollection
{
    /// <summary>Collection name used by tests that mutate process-wide environment variables.</summary>
    public const string Name = "Environment";
}

internal sealed class EnvironmentVariableScope : IDisposable
{
    private readonly string _name;
    private readonly string? _previous;

    public EnvironmentVariableScope(string name, string? value)
    {
        _name = name;
        _previous = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(_name, _previous);
    }
}
