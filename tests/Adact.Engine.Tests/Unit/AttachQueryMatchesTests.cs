using Xunit;

namespace Adact.Engine.Tests.Unit;

[Trait("Layer", "Unit")]
public class AttachQueryMatchesTests
{
    private static WindowInfo Win(int pid, string proc, string title, string? className)
        => new(pid, proc, title, "Window", className, IntPtr.Zero);

    [Fact]
    public void Matches_GivenClassNameOnly_ReturnsTrueForExactMatch()
    {
        var w = Win(100, "Notepad", "Untitled", "Notepad");
        var q = new AttachQuery(ClassName: "Notepad");
        Assert.True(UiaEngine.Matches(w, q));
    }

    [Fact]
    public void Matches_GivenClassNameMismatch_ReturnsFalse()
    {
        var w = Win(100, "Notepad", "Untitled", "Notepad");
        var q = new AttachQuery(ClassName: "ConsoleWindowClass");
        Assert.False(UiaEngine.Matches(w, q));
    }

    [Fact]
    public void Matches_GivenClassNameWithDifferentCase_ReturnsTrue()
    {
        var w = Win(100, "Notepad", "Untitled", "Notepad");
        var q = new AttachQuery(ClassName: "NOTEPAD");
        Assert.True(UiaEngine.Matches(w, q));
    }

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

    [Fact]
    public void Matches_GivenClassNameOnTargetIsNull_ReturnsFalse()
    {
        var w = Win(100, "X", "T", className: null);
        var q = new AttachQuery(ClassName: "Notepad");
        Assert.False(UiaEngine.Matches(w, q));
    }

    [Fact]
    public void Matches_GivenAllFieldsNull_ReturnsFalse()
    {
        var w = Win(100, "Notepad", "Untitled", "Notepad");
        Assert.False(UiaEngine.Matches(w, new AttachQuery()));
    }
}
