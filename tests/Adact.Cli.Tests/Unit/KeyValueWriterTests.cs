using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="KeyValueWriter"/> の key-value 1 行出力を検証する Unit テスト。
/// </summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class KeyValueWriterTests
{
    /// <summary>基本的な key-value ペアが "key value\n" 形式で書き出されることを確認する。</summary>
    [Fact]
    public void Write_BasicKeyValue_OutputsKeySpaceValue()
    {
        var (stdout, _) = CapturedConsole.Run(() =>
        {
            KeyValueWriter.Write("name", "hello");
        });

        Assert.Equal("name hello" + Environment.NewLine, stdout);
    }

    /// <summary>スペースを含む値がそのまま 1 行に書き出されることを確認する。</summary>
    [Fact]
    public void Write_ValueWithSpaces_PreservesSpaces()
    {
        var (stdout, _) = CapturedConsole.Run(() =>
        {
            KeyValueWriter.Write("title", "hello world foo");
        });

        Assert.Equal("title hello world foo" + Environment.NewLine, stdout);
    }

    /// <summary>日本語を含む値が正しく書き出されることを確認する。</summary>
    [Fact]
    public void Write_ValueWithUnicode_PreservesCharacters()
    {
        var (stdout, _) = CapturedConsole.Run(() =>
        {
            KeyValueWriter.Write("msg", "こんにちは");
        });

        Assert.Equal("msg こんにちは" + Environment.NewLine, stdout);
    }

    /// <summary>空文字列の値が "key \n" として書き出されることを確認する。</summary>
    [Fact]
    public void Write_EmptyValue_OutputsKeyWithTrailingSpace()
    {
        var (stdout, _) = CapturedConsole.Run(() =>
        {
            KeyValueWriter.Write("key", "");
        });

        Assert.Equal("key " + Environment.NewLine, stdout);
    }

    /// <summary>タブや改行を含む特殊文字が値としてそのまま出力されることを確認する。</summary>
    [Fact]
    public void Write_ValueWithSpecialCharacters_OutputsAsIs()
    {
        var (stdout, _) = CapturedConsole.Run(() =>
        {
            KeyValueWriter.Write("data", "a\tb");
        });

        Assert.Equal("data a\tb" + Environment.NewLine, stdout);
    }
}
