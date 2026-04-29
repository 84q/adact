using Adact.Cli.Snapshots;

using Xunit;

namespace Adact.Cli.Tests.Unit;

[Trait("Layer", "Unit")]
public class SnapshotTextEscaperTests
{
    [Fact]
    public void Escape_NullInput_ReturnsNull()
    {
        Assert.Null(SnapshotTextEscaper.Escape(null));
    }

    [Fact]
    public void Escape_EscapesSpecialChars()
    {
        Assert.Equal("a\\\"b\\\\c\\nd\\te", SnapshotTextEscaper.Escape("a\"b\\c\nd\te"));
    }

    [Fact]
    public void Escape_PreservesUnicode()
    {
        Assert.Equal("電卓", SnapshotTextEscaper.Escape("電卓"));
    }

    [Fact]
    public void Escape_OtherControlChars_BecomeUnicodeEscape()
    {
        Assert.Equal("\\u0001", SnapshotTextEscaper.Escape("\u0001"));
        Assert.Equal("\\u000D", SnapshotTextEscaper.Escape("\r"));
        Assert.Equal("\\u007F", SnapshotTextEscaper.Escape("\u007F"));
    }

    [Fact]
    public void Quote_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(SnapshotTextEscaper.Quote(null));
        Assert.Null(SnapshotTextEscaper.Quote(""));
    }

    [Fact]
    public void Quote_WrapsAndEscapes()
    {
        Assert.Equal("\"a\\\"b\"", SnapshotTextEscaper.Quote("a\"b"));
    }
}
