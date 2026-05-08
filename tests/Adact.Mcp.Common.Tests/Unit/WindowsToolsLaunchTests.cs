using System.Diagnostics;

using Adact.Engine;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Mcp.Common.Tests.Unit;

/// <summary>Contains tests for the Windows Tools Launch behavior.</summary>
[Trait("Layer", "Unit")]
public class WindowsToolsLaunchTests
{
    private sealed class FakeDaemonControl : IDaemonControl
    {
        /// <summary>Gets a value indicating whether Is Supported.</summary>
        public bool IsSupported { get; init; } = true;
        /// <summary>Performs the Stop Async operation.</summary>
        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private static (WindowsTools tools, SessionStore store) CreateTools()
    {
        var engine = new UiaEngine();
        var store = new SessionStore(engine);
        var refStore = new WindowRefStore();
        var daemon = new FakeDaemonControl();
        var tools = new WindowsTools(store, refStore, daemon);
        return (tools, store);
    }

    private static (string code, string message) ReadError(CallToolResult result)
    {
        Assert.True(result.IsError == true, "Expected IsError=true");
        Assert.NotNull(result.StructuredContent);
        var doc = result.StructuredContent.Value;
        return (
            doc.GetProperty("code").GetString()!,
            doc.GetProperty("message").GetString()!);
    }

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
        catch { }
    }

    /// <summary>Performs the Launch Uwp With Cwd Returns Invalid Argument operation.</summary>
    [Fact]
    public async Task Launch_UwpWithCwd_ReturnsInvalidArgument()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.LaunchAsync(
                executable: "shell:AppsFolder\\Microsoft.WindowsCalculator_8wekyb3d8bbwe!App",
                cwd: "C:\\");
            var (code, msg) = ReadError(result);
            Assert.Equal(ToolErrors.InvalidArgument, code);
            Assert.Contains("cwd", msg, StringComparison.Ordinal);
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Launch Uwp With Env Returns Invalid Argument operation.</summary>
    [Fact]
    public async Task Launch_UwpWithEnv_ReturnsInvalidArgument()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.LaunchAsync(
                executable: "shell:AppsFolder\\Microsoft.WindowsCalculator_8wekyb3d8bbwe!App",
                env: new Dictionary<string, string> { ["FOO"] = "BAR" });
            var (code, msg) = ReadError(result);
            Assert.Equal(ToolErrors.InvalidArgument, code);
            Assert.Contains("env", msg, StringComparison.Ordinal);
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Launch Empty Executable Returns Invalid Argument operation.</summary>
    [Fact]
    public async Task Launch_EmptyExecutable_ReturnsInvalidArgument()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.LaunchAsync(executable: "");
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.InvalidArgument, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Launch Nonexistent Executable Returns Launch Failed operation.</summary>
    [Fact]
    public async Task Launch_NonexistentExecutable_ReturnsLaunchFailed()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.LaunchAsync(
                executable: "X:\\definitely-does-not-exist-adact.exe");
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.LaunchFailed, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Launch Cmd Exe Returns Pid Json operation.</summary>
    [Fact]
    public async Task Launch_CmdExe_ReturnsPidJson()
    {
        var (tools, store) = CreateTools();
        int pid = 0;
        try
        {
            var result = await tools.LaunchAsync(
                executable: "cmd.exe",
                args: ["/c", "exit 0"]);

            Assert.False(result.IsError == true);
            Assert.NotNull(result.StructuredContent);
            var doc = result.StructuredContent.Value;
            pid = doc.GetProperty("pid").GetInt32();
            Assert.True(pid > 0);
            var name = doc.GetProperty("processName").GetString();
            Assert.False(string.IsNullOrEmpty(name));
        }
        finally
        {
            if (pid > 0) TryKill(pid);
            store.Dispose();
        }
    }
}
