using Adact.Cli.Snapshots;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="SnapshotTextEscaper"/> の文字列エスケープと Quote 処理を検証する Unit テスト。
/// snapshot text format の安全な出力仕様の回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
public class SnapshotTextEscaperTests
{
    /// <summary>null 入力は null をそのまま返すことを確認する。</summary>
    [Fact]
    public void Escape_NullInput_ReturnsNull()
    {
        Assert.Null(SnapshotTextEscaper.Escape(null));
    }

    /// <summary>ダブルクオート / バックスラッシュ / \n / \t がそれぞれ期待通りエスケープされることを確認する。</summary>
    [Fact]
    public void Escape_EscapesSpecialChars()
    {
        Assert.Equal("a\\\"b\\\\c\\nd\\te", SnapshotTextEscaper.Escape("a\"b\\c\nd\te"));
    }

    /// <summary>全角文字 (Unicode BMP) はエスケープせずそのまま保持されることを確認する。</summary>
    [Fact]
    public void Escape_PreservesUnicode()
    {
        Assert.Equal("電卓", SnapshotTextEscaper.Escape("電卓"));
    }

    /// <summary>\n/\t 以外の C0/C1 コントロール文字は \uXXXX 形式にエスケープされることを確認する。</summary>
    [Fact]
    public void Escape_OtherControlChars_BecomeUnicodeEscape()
    {
        Assert.Equal("\\u0001", SnapshotTextEscaper.Escape("\u0001"));
        Assert.Equal("\\u000D", SnapshotTextEscaper.Escape("\r"));
        Assert.Equal("\\u007F", SnapshotTextEscaper.Escape("\u007F"));
    }

    /// <summary>Quote は null/空文字列をそのまま null として返すことを確認する ("" を \"\" として出さない)。</summary>
    [Fact]
    public void Quote_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(SnapshotTextEscaper.Quote(null));
        Assert.Null(SnapshotTextEscaper.Quote(""));
    }

    /// <summary>Quote が入力を \" でラップし、中身のダブルクオートもエスケープすることを確認する。</summary>
    [Fact]
    public void Quote_WrapsAndEscapes()
    {
        Assert.Equal("\"a\\\"b\"", SnapshotTextEscaper.Quote("a\"b"));
    }
}
