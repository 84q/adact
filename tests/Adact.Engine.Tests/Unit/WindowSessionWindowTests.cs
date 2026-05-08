using Adact.Engine;
using Adact.Engine.Exceptions;

using System.Diagnostics;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>Contains tests for the Window Session Window behavior.</summary>
[Trait("Layer", "Unit")]
public class WindowSessionWindowTests
{
    private static WindowSession CreateSession()
    {
        var info = new WindowInfo(
            ProcessId: 12345,
            ProcessName: "fake",
            Title: "Fake",
            ControlType: "Window",
            ClassName: null,
            NativeWindowHandle: 0x1234);
        return WindowSession.CreateForTest(1, info);
    }

    /// <summary>Performs the Resize Async Non Positive Width Throws Argument Out Of Range operation.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task ResizeAsync_NonPositiveWidth_ThrowsArgumentOutOfRange(int width)
    {
        var session = CreateSession();
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => session.ResizeAsync(width, 100));
        Assert.Equal("width", ex.ParamName);
    }

    /// <summary>Performs the Resize Async Non Positive Height Throws Argument Out Of Range operation.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task ResizeAsync_NonPositiveHeight_ThrowsArgumentOutOfRange(int height)
    {
        var session = CreateSession();
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => session.ResizeAsync(100, height));
        Assert.Equal("height", ex.ParamName);
    }

    /// <summary>Performs the Resize Async Both Zero Throws Argument Out Of Range For Width operation.</summary>
    [Fact]
    public async Task ResizeAsync_BothZero_ThrowsArgumentOutOfRangeForWidth()
    {
        var session = CreateSession();
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => session.ResizeAsync(0, 0));
        Assert.Equal("width", ex.ParamName);
    }

    /// <summary>Performs the Kill Async Without Original Process Start Time Throws Kill Failed operation.</summary>
    [Fact]
    public async Task KillAsync_WithoutOriginalProcessStartTime_ThrowsKillFailed()
    {
        using var process = StartSleeperProcess();
        var session = WindowSession.CreateForTest(1, new WindowInfo(
            ProcessId: process.Id,
            ProcessName: process.ProcessName,
            Title: "Fake",
            ControlType: "Window",
            ClassName: null,
            NativeWindowHandle: 0,
            ProcessStartTimeUtc: null));

        var ex = await Assert.ThrowsAsync<KillFailedException>(() => session.KillAsync(force: true));

        Assert.Contains("start time is unknown", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(process.HasExited);
        TryKill(process);
    }

    /// <summary>Performs the Kill Async With Mismatched Process Start Time Throws Kill Failed operation.</summary>
    [Fact]
    public async Task KillAsync_WithMismatchedProcessStartTime_ThrowsKillFailed()
    {
        using var process = StartSleeperProcess();
        var session = WindowSession.CreateForTest(1, new WindowInfo(
            ProcessId: process.Id,
            ProcessName: process.ProcessName,
            Title: "Fake",
            ControlType: "Window",
            ClassName: null,
            NativeWindowHandle: 0,
            ProcessStartTimeUtc: process.StartTime.ToUniversalTime().AddSeconds(-1)));

        var ex = await Assert.ThrowsAsync<KillFailedException>(() => session.KillAsync(force: true));

        Assert.Contains("different process", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(process.HasExited);
        TryKill(process);
    }

    /// <summary>Performs the Kill Async With Matching Process Start Time Kills Process operation.</summary>
    [Fact]
    public async Task KillAsync_WithMatchingProcessStartTime_KillsProcess()
    {
        using var process = StartSleeperProcess();
        var session = WindowSession.CreateForTest(1, new WindowInfo(
            ProcessId: process.Id,
            ProcessName: process.ProcessName,
            Title: "Fake",
            ControlType: "Window",
            ClassName: null,
            NativeWindowHandle: 0,
            ProcessStartTimeUtc: process.StartTime.ToUniversalTime()));

        var result = await session.KillAsync(force: true);
        process.WaitForExit(5000);

        Assert.True(process.HasExited);
        Assert.Equal(KillMethod.Forced, result);
    }

    private static Process StartSleeperProcess()
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = "-NoProfile -Command Start-Sleep -Seconds 30",
            UseShellExecute = false,
            CreateNoWindow = true,
        });

        Assert.NotNull(process);
        return process!;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(2000);
            }
        }
        catch
        {
        }
    }
}
