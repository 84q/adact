using System.Text.Json;

using Adact.Cli.Commands;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Json Helpers behavior.</summary>
[Trait("Layer", "Unit")]
public class JsonHelpersTests
{
    // --- GetStringOrNull ---

    /// <summary>Gets the Get String Or Null String Property Returns Value value.</summary>
    [Fact]
    public void GetStringOrNull_StringProperty_ReturnsValue()
    {
        var json = JsonSerializer.SerializeToElement(new { name = "hello" });
        Assert.Equal("hello", JsonHelpers.GetStringOrNull(json, "name"));
    }

    /// <summary>Gets the Get String Or Null Missing Property Returns Null value.</summary>
    [Fact]
    public void GetStringOrNull_MissingProperty_ReturnsNull()
    {
        var json = JsonSerializer.SerializeToElement(new { name = "hello" });
        Assert.Null(JsonHelpers.GetStringOrNull(json, "missing"));
    }

    /// <summary>Gets the Get String Or Null Null Property Returns Null value.</summary>
    [Fact]
    public void GetStringOrNull_NullProperty_ReturnsNull()
    {
        var json = JsonDocument.Parse("""{"name": null}""").RootElement;
        Assert.Null(JsonHelpers.GetStringOrNull(json, "name"));
    }

    /// <summary>Gets the Get String Or Null Number Property Returns To String value.</summary>
    [Fact]
    public void GetStringOrNull_NumberProperty_ReturnsToString()
    {
        var json = JsonSerializer.SerializeToElement(new { val = 42 });
        Assert.Equal("42", JsonHelpers.GetStringOrNull(json, "val"));
    }

    /// <summary>Gets the Get String Or Null Non Object Returns Null value.</summary>
    [Fact]
    public void GetStringOrNull_NonObject_ReturnsNull()
    {
        var json = JsonSerializer.SerializeToElement("just a string");
        Assert.Null(JsonHelpers.GetStringOrNull(json, "anything"));
    }

    // --- GetIntAsStringOrNull ---

    /// <summary>Gets the Get Int As String Or Null Number Property Returns String value.</summary>
    [Fact]
    public void GetIntAsStringOrNull_NumberProperty_ReturnsString()
    {
        var json = JsonSerializer.SerializeToElement(new { pid = 1234 });
        Assert.Equal("1234", JsonHelpers.GetIntAsStringOrNull(json, "pid"));
    }

    /// <summary>Gets the Get Int As String Or Null String Property Returns As Is value.</summary>
    [Fact]
    public void GetIntAsStringOrNull_StringProperty_ReturnsAsIs()
    {
        var json = JsonSerializer.SerializeToElement(new { pid = "5678" });
        Assert.Equal("5678", JsonHelpers.GetIntAsStringOrNull(json, "pid"));
    }

    /// <summary>Gets the Get Int As String Or Null Missing Property Returns Null value.</summary>
    [Fact]
    public void GetIntAsStringOrNull_MissingProperty_ReturnsNull()
    {
        var json = JsonSerializer.SerializeToElement(new { });
        Assert.Null(JsonHelpers.GetIntAsStringOrNull(json, "pid"));
    }

    /// <summary>Gets the Get Int As String Or Null Bool Property Returns Null value.</summary>
    [Fact]
    public void GetIntAsStringOrNull_BoolProperty_ReturnsNull()
    {
        var json = JsonSerializer.SerializeToElement(new { flag = true });
        Assert.Null(JsonHelpers.GetIntAsStringOrNull(json, "flag"));
    }

    // --- GetIntOrNull ---

    /// <summary>Gets the Get Int Or Null Number Property Returns Int value.</summary>
    [Fact]
    public void GetIntOrNull_NumberProperty_ReturnsInt()
    {
        var json = JsonSerializer.SerializeToElement(new { count = 7 });
        Assert.Equal(7, JsonHelpers.GetIntOrNull(json, "count"));
    }

    /// <summary>Gets the Get Int Or Null Missing Property Returns Null value.</summary>
    [Fact]
    public void GetIntOrNull_MissingProperty_ReturnsNull()
    {
        var json = JsonSerializer.SerializeToElement(new { });
        Assert.Null(JsonHelpers.GetIntOrNull(json, "count"));
    }

    /// <summary>Gets the Get Int Or Null String Property Returns Null value.</summary>
    [Fact]
    public void GetIntOrNull_StringProperty_ReturnsNull()
    {
        var json = JsonSerializer.SerializeToElement(new { count = "abc" });
        Assert.Null(JsonHelpers.GetIntOrNull(json, "count"));
    }

    /// <summary>Gets the Get Int Or Null Non Object Returns Null value.</summary>
    [Fact]
    public void GetIntOrNull_NonObject_ReturnsNull()
    {
        var json = JsonSerializer.SerializeToElement(123);
        Assert.Null(JsonHelpers.GetIntOrNull(json, "anything"));
    }
}
