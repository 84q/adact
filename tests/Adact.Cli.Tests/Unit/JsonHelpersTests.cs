using System.Text.Json;

using Adact.Cli.Commands;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="JsonHelpers"/> の Unit テスト。MCP レスポンス JSON の読み出しヘルパの動作を検証する。
/// </summary>
[Trait("Layer", "Unit")]
public class JsonHelpersTests
{
    // --- GetStringOrNull ---

    [Fact]
    public void GetStringOrNull_StringProperty_ReturnsValue()
    {
        var json = JsonSerializer.SerializeToElement(new { name = "hello" });
        Assert.Equal("hello", JsonHelpers.GetStringOrNull(json, "name"));
    }

    [Fact]
    public void GetStringOrNull_MissingProperty_ReturnsNull()
    {
        var json = JsonSerializer.SerializeToElement(new { name = "hello" });
        Assert.Null(JsonHelpers.GetStringOrNull(json, "missing"));
    }

    [Fact]
    public void GetStringOrNull_NullProperty_ReturnsNull()
    {
        var json = JsonDocument.Parse("""{"name": null}""").RootElement;
        Assert.Null(JsonHelpers.GetStringOrNull(json, "name"));
    }

    [Fact]
    public void GetStringOrNull_NumberProperty_ReturnsToString()
    {
        var json = JsonSerializer.SerializeToElement(new { val = 42 });
        Assert.Equal("42", JsonHelpers.GetStringOrNull(json, "val"));
    }

    [Fact]
    public void GetStringOrNull_NonObject_ReturnsNull()
    {
        var json = JsonSerializer.SerializeToElement("just a string");
        Assert.Null(JsonHelpers.GetStringOrNull(json, "anything"));
    }

    // --- GetIntAsStringOrNull ---

    [Fact]
    public void GetIntAsStringOrNull_NumberProperty_ReturnsString()
    {
        var json = JsonSerializer.SerializeToElement(new { pid = 1234 });
        Assert.Equal("1234", JsonHelpers.GetIntAsStringOrNull(json, "pid"));
    }

    [Fact]
    public void GetIntAsStringOrNull_StringProperty_ReturnsAsIs()
    {
        var json = JsonSerializer.SerializeToElement(new { pid = "5678" });
        Assert.Equal("5678", JsonHelpers.GetIntAsStringOrNull(json, "pid"));
    }

    [Fact]
    public void GetIntAsStringOrNull_MissingProperty_ReturnsNull()
    {
        var json = JsonSerializer.SerializeToElement(new { });
        Assert.Null(JsonHelpers.GetIntAsStringOrNull(json, "pid"));
    }

    [Fact]
    public void GetIntAsStringOrNull_BoolProperty_ReturnsNull()
    {
        var json = JsonSerializer.SerializeToElement(new { flag = true });
        Assert.Null(JsonHelpers.GetIntAsStringOrNull(json, "flag"));
    }

    // --- GetIntOrNull ---

    [Fact]
    public void GetIntOrNull_NumberProperty_ReturnsInt()
    {
        var json = JsonSerializer.SerializeToElement(new { count = 7 });
        Assert.Equal(7, JsonHelpers.GetIntOrNull(json, "count"));
    }

    [Fact]
    public void GetIntOrNull_MissingProperty_ReturnsNull()
    {
        var json = JsonSerializer.SerializeToElement(new { });
        Assert.Null(JsonHelpers.GetIntOrNull(json, "count"));
    }

    [Fact]
    public void GetIntOrNull_StringProperty_ReturnsNull()
    {
        var json = JsonSerializer.SerializeToElement(new { count = "abc" });
        Assert.Null(JsonHelpers.GetIntOrNull(json, "count"));
    }

    [Fact]
    public void GetIntOrNull_NonObject_ReturnsNull()
    {
        var json = JsonSerializer.SerializeToElement(123);
        Assert.Null(JsonHelpers.GetIntOrNull(json, "anything"));
    }
}
