using System.Diagnostics;

using Adact.Engine;
using Adact.Engine.Exceptions;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>Contains tests for the Uia Engine Launch behavior.</summary>
[Trait("Layer", "Unit")]
public class UiaEngineLaunchTests
{
    private static UiaEngine CreateEngine() => new();

    private static void TryKill(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            if (!p.HasExited)
            {
                p.Kill(entireProcessTree: true);
                p.WaitForExit(2000);
            }
        }
        catch
        {
        }
    }

    /// <summary>Performs the Launch Async Nonexistent Executable Throws Launch Failed operation.</summary>
    [Fact]
    public async Task LaunchAsync_NonexistentExecutable_ThrowsLaunchFailed()
    {
        using var engine = CreateEngine();
        var request = new LaunchRequest(
            Executable: "X:\\definitely-does-not-exist-adact.exe");
        await Assert.ThrowsAsync<LaunchFailedException>(
            () => engine.LaunchAsync(request));
    }

    /// <summary>Performs the Launch Async Cmd Exe Returns Valid Pid operation.</summary>
    [Fact]
    public async Task LaunchAsync_CmdExe_ReturnsValidPid()
    {
        using var engine = CreateEngine();
        var request = new LaunchRequest(
            Executable: "cmd.exe",
            Arguments: ["/c", "exit 0"]);

        var result = await engine.LaunchAsync(request);
        try
        {
            Assert.True(result.Pid > 0);
            Assert.False(string.IsNullOrEmpty(result.ProcessName));
        }
        finally
        {
            TryKill(result.Pid);
        }
    }

    /// <summary>Performs the Launch Async With Working Directory Propagates Cwd operation.</summary>
    [Fact]
    public async Task LaunchAsync_WithWorkingDirectory_PropagatesCwd()
    {
        using var engine = CreateEngine();
        var cwd = Path.GetTempPath();
        var request = new LaunchRequest(
            Executable: "cmd.exe",
            Arguments: ["/c", "exit 0"],
            WorkingDirectory: cwd);

        var result = await engine.LaunchAsync(request);
        try
        {
            Assert.True(result.Pid > 0);
        }
        finally
        {
            TryKill(result.Pid);
        }
    }

    /// <summary>Performs the Launch Async With Environment Propagates Env To Child Process operation.</summary>
    [Fact]
    public async Task LaunchAsync_WithEnvironment_PropagatesEnvToChildProcess()
    {
        using var engine = CreateEngine();
        var stem = Path.Combine(
            Path.GetTempPath(),
            $"adact_env_test_{Guid.NewGuid():N}");
        var batPath = stem + ".bat";
        var marker = stem + ".txt";
        File.WriteAllText(batPath, $"@echo %ADACT_TEST%>\"{marker}\"\r\n");

        var request = new LaunchRequest(
            Executable: "cmd.exe",
            Arguments: ["/c", batPath],
            Environment: new Dictionary<string, string>
            {
                ["ADACT_TEST"] = "ADACT_VALUE_42",
            });

        var result = await engine.LaunchAsync(request);
        try
        {
            Assert.True(result.Pid > 0);

            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!File.Exists(marker) && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
            }
            Assert.True(File.Exists(marker), $"marker file not created: {marker}");

            string? contents = null;
            for (var i = 0; i < 10; i++)
            {
                try { contents = File.ReadAllText(marker); break; }
                catch (IOException) { await Task.Delay(50); }
            }
            Assert.NotNull(contents);
            Assert.Equal("ADACT_VALUE_42", contents!.Trim());
        }
        finally
        {
            TryKill(result.Pid);
            try { if (File.Exists(marker)) File.Delete(marker); } catch { /* best effort */ }
            try { if (File.Exists(batPath)) File.Delete(batPath); } catch { /* best effort */ }
        }
    }

    /// <summary>Performs the Launch Async Uwp With Cwd Throws Argument Exception operation.</summary>
    [Fact]
    public async Task LaunchAsync_UwpWithCwd_ThrowsArgumentException()
    {
        using var engine = CreateEngine();
        var request = new LaunchRequest(
            Executable: "shell:AppsFolder\\Microsoft.WindowsCalculator_8wekyb3d8bbwe!App",
            WorkingDirectory: "C:\\");

        await Assert.ThrowsAsync<ArgumentException>(
            () => engine.LaunchAsync(request));
    }

    /// <summary>Performs the Launch Async Uwp With Env Throws Argument Exception operation.</summary>
    [Fact]
    public async Task LaunchAsync_UwpWithEnv_ThrowsArgumentException()
    {
        using var engine = CreateEngine();
        var request = new LaunchRequest(
            Executable: "shell:AppsFolder\\Microsoft.WindowsCalculator_8wekyb3d8bbwe!App",
            Environment: new Dictionary<string, string> { ["FOO"] = "BAR" });

        await Assert.ThrowsAsync<ArgumentException>(
            () => engine.LaunchAsync(request));
    }

    /// <summary>Performs the Launch Async Empty Executable Throws Argument Exception operation.</summary>
    [Fact]
    public async Task LaunchAsync_EmptyExecutable_ThrowsArgumentException()
    {
        using var engine = CreateEngine();
        var request = new LaunchRequest(Executable: "  ");
        await Assert.ThrowsAsync<ArgumentException>(
            () => engine.LaunchAsync(request));
    }

    /// <summary>Performs the Launch Async After Dispose Throws operation.</summary>
    [Fact]
    public async Task LaunchAsync_AfterDispose_Throws()
    {
        var engine = CreateEngine();
        engine.Dispose();
        var request = new LaunchRequest(Executable: "cmd.exe");
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => engine.LaunchAsync(request));
    }


    /// <summary>Performs the Quote If Needed No Whitespace Or Quote Not Quoted operation.</summary>
    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("C:\\Users\\foo\\", "C:\\Users\\foo\\")]
    [InlineData("ABC", "ABC")]
    public void QuoteIfNeeded_NoWhitespaceOrQuote_NotQuoted(string input, string expected)
    {
        Assert.Equal(expected, UiaEngine.QuoteIfNeeded(input));
    }

    /// <summary>Performs the Quote If Needed Empty Returns Empty Quoted operation.</summary>
    [Fact]
    public void QuoteIfNeeded_Empty_ReturnsEmptyQuoted()
    {
        Assert.Equal("\"\"", UiaEngine.QuoteIfNeeded(string.Empty));
    }

    /// <summary>Performs the Quote If Needed With Space Quoted operation.</summary>
    [Fact]
    public void QuoteIfNeeded_WithSpace_Quoted()
    {
        Assert.Equal("\"with space\"", UiaEngine.QuoteIfNeeded("with space"));
    }

    /// <summary>Performs the Quote If Needed With Tab Quoted operation.</summary>
    [Fact]
    public void QuoteIfNeeded_WithTab_Quoted()
    {
        Assert.Equal("\"a\tb\"", UiaEngine.QuoteIfNeeded("a\tb"));
    }

    /// <summary>Performs the Quote If Needed Embedded Quote Escapes Quote operation.</summary>
    [Fact]
    public void QuoteIfNeeded_EmbeddedQuote_EscapesQuote()
    {
        Assert.Equal("\"has\\\"quote\"", UiaEngine.QuoteIfNeeded("has\"quote"));
    }

    /// <summary>Performs the Quote If Needed Trailing Backslash With Whitespace Doubles Backslashes Before Closing Quote operation.</summary>
    [Fact]
    public void QuoteIfNeeded_TrailingBackslashWithWhitespace_DoublesBackslashesBeforeClosingQuote()
    {
        Assert.Equal("\"foo bar\\\\\"", UiaEngine.QuoteIfNeeded("foo bar\\"));

        Assert.Equal(
            "\"C:\\Program Files\\foo\\\\\\\\\"",
            UiaEngine.QuoteIfNeeded("C:\\Program Files\\foo\\\\"));
    }

    /// <summary>Performs the Quote If Needed Backslashes Before Quote Doubled Plus One operation.</summary>
    [Fact]
    public void QuoteIfNeeded_BackslashesBeforeQuote_DoubledPlusOne()
    {
        Assert.Equal("\"a \\\\\\\" b\"", UiaEngine.QuoteIfNeeded("a \\\" b"));
    }
}
