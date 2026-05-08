using Adact.Cli.Snapshots;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Snapshot Text Escaper behavior.</summary>
[Trait("Layer", "Unit")]
public class SnapshotTextEscaperTests
{
    /// <summary>Performs the Escape Null Input Returns Null operation.</summary>
    [Fact]
    public void Escape_NullInput_ReturnsNull()
    {
        Assert.Null(SnapshotTextEscaper.Escape(null));
    }

    /// <summary>Performs the Escape Escapes Special Chars operation.</summary>
    [Fact]
    public void Escape_EscapesSpecialChars()
    {
        Assert.Equal("a\\\"b\\\\c\\nd\\te", SnapshotTextEscaper.Escape("a\"b\\c\nd\te"));
    }

    /// <summary>Performs the Escape Preserves Unicode operation.</summary>
    [Fact]
    public void Escape_PreservesUnicode()
    {
    }

    /// <summary>Performs the Escape Other Control Chars Become Unicode Escape operation.</summary>
    [Fact]
    public void Escape_OtherControlChars_BecomeUnicodeEscape()
    {
        Assert.Equal("\\u0001", SnapshotTextEscaper.Escape("\u0001"));
        Assert.Equal("\\u000D", SnapshotTextEscaper.Escape("\r"));
        Assert.Equal("\\u007F", SnapshotTextEscaper.Escape("\u007F"));
    }

    /// <summary>Performs the Quote Null Or Empty Returns Null operation.</summary>
    [Fact]
    public void Quote_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(SnapshotTextEscaper.Quote(null));
        Assert.Null(SnapshotTextEscaper.Quote(""));
    }

    /// <summary>Performs the Quote Wraps And Escapes operation.</summary>
    [Fact]
    public void Quote_WrapsAndEscapes()
    {
        Assert.Equal("\"a\\\"b\"", SnapshotTextEscaper.Quote("a\"b"));
    }
}
