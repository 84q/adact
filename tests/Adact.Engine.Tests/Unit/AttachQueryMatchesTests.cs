using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>
/// <see cref="UiaEngine.Matches(WindowInfo, AttachQuery)"/> のクエリ適用ロジックを検証する Unit テスト。
/// ClassName/ProcessName の集合マッチ (AND 条件) と大小文字不一致 (送信仕様) の回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
public class AttachQueryMatchesTests
{
    private static WindowInfo Win(int pid, string proc, string title, string? className)
        => new(pid, proc, title, "Window", className, IntPtr.Zero);

    /// <summary>
    /// ClassName のみ指定クエリでクラス名が一致したとき true を返すことを確認する。
    /// </summary>
    [Fact]
    public void Matches_GivenClassNameOnly_ReturnsTrueForExactMatch()
    {
        var w = Win(100, "Notepad", "Untitled", "Notepad");
        var q = new AttachQuery(ClassName: "Notepad");
        Assert.True(UiaEngine.Matches(w, q));
    }

    /// <summary>
    /// ClassName 不一致時に false を返すことを確認する。
    /// </summary>
    [Fact]
    public void Matches_GivenClassNameMismatch_ReturnsFalse()
    {
        var w = Win(100, "Notepad", "Untitled", "Notepad");
        var q = new AttachQuery(ClassName: "ConsoleWindowClass");
        Assert.False(UiaEngine.Matches(w, q));
    }

    /// <summary>
    /// ClassName の大小文字を無視して一致することを確認する。
    /// Win32 ClassName のケースインセンシティブ (cli.md の attach 仕様) の回帰防止。
    /// </summary>
    [Fact]
    public void Matches_GivenClassNameWithDifferentCase_ReturnsTrue()
    {
        var w = Win(100, "Notepad", "Untitled", "Notepad");
        var q = new AttachQuery(ClassName: "NOTEPAD");
        Assert.True(UiaEngine.Matches(w, q));
    }

    /// <summary>
    /// ClassName と ProcessName を同時指定した場合、両方一致したときのみ true を返すことを確認する。
    /// AND 条件仕様の回帰防止。
    /// </summary>
    [Fact]
    public void Matches_GivenClassNameAndProcessName_RequiresBoth()
    {
        var w = Win(100, "Notepad", "Untitled", "Notepad");

        // 両方一致
        Assert.True(UiaEngine.Matches(w,
            new AttachQuery(ProcessName: "Notepad", ClassName: "Notepad")));

        // ProcessName 不一致
        Assert.False(UiaEngine.Matches(w,
            new AttachQuery(ProcessName: "Other", ClassName: "Notepad")));

        // ClassName 不一致
        Assert.False(UiaEngine.Matches(w,
            new AttachQuery(ProcessName: "Notepad", ClassName: "Other")));
    }

    /// <summary>
    /// ターゲットの ClassName が null なとき、ClassName クエリはマッチしないことを確認する。
    /// </summary>
    [Fact]
    public void Matches_GivenClassNameOnTargetIsNull_ReturnsFalse()
    {
        var w = Win(100, "X", "T", className: null);
        var q = new AttachQuery(ClassName: "Notepad");
        Assert.False(UiaEngine.Matches(w, q));
    }

    /// <summary>
    /// 全フィールド未指定の AttachQuery は何にもマッチしないことを確認する (意図しない全件マッチ防止)。
    /// </summary>
    [Fact]
    public void Matches_GivenAllFieldsNull_ReturnsFalse()
    {
        var w = Win(100, "Notepad", "Untitled", "Notepad");
        Assert.False(UiaEngine.Matches(w, new AttachQuery()));
    }
}
