using Adact.Engine;
using Adact.Engine.Exceptions;

using System.Diagnostics;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>
/// <see cref="WindowSession.ResizeAsync(int, int, CancellationToken)"/> の引数検証 (Phase 8 Step 5) を
/// 検証する Unit テスト。実 UIA / FlaUI には依存せず、<see cref="WindowSession.CreateForTest(int, WindowInfo)"/> で
/// 生成した最小セッションに対して引数検証段階の例外のみを確認する。
/// </summary>
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

    /// <summary>
    /// width が 0 以下の場合、ResizeAsync は <see cref="ArgumentOutOfRangeException"/> を投げる
    /// (UIA への到達前に弾く) ことを確認する。
    /// </summary>
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

    /// <summary>
    /// height が 0 以下の場合、ResizeAsync は <see cref="ArgumentOutOfRangeException"/> を投げることを確認する。
    /// </summary>
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

    /// <summary>
    /// width / height が共に 0 の場合、最初の引数 (width) で ArgumentOutOfRangeException が投げられることを確認する。
    /// </summary>
    [Fact]
    public async Task ResizeAsync_BothZero_ThrowsArgumentOutOfRangeForWidth()
    {
        var session = CreateSession();
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => session.ResizeAsync(0, 0));
        Assert.Equal("width", ex.ParamName);
    }

    /// <summary>
    /// attach 時点の ProcessStartTime が欠落している場合、KillAsync は安全側で拒否する。
    /// </summary>
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

        var ex = await Assert.ThrowsAsync<KillFailedException>(() => session.KillAsync());

        Assert.Contains("start time is unknown", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(process.HasExited);
        TryKill(process);
    }

    /// <summary>
    /// attach 時点の ProcessStartTime と現在 PID の start time が一致しない場合、KillAsync は別プロセス誤爆を防ぐため拒否する。
    /// </summary>
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

        var ex = await Assert.ThrowsAsync<KillFailedException>(() => session.KillAsync());

        Assert.Contains("different process", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(process.HasExited);
        TryKill(process);
    }

    /// <summary>
    /// attach 時点の ProcessStartTime と現在 PID が一致する場合のみ、KillAsync は対象プロセスを終了できる。
    /// </summary>
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

        await session.KillAsync();
        process.WaitForExit(5000);

        Assert.True(process.HasExited);
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
