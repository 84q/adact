using System.Diagnostics;
using System.Text.Json;

using Adact.Tests.Common;

using Xunit;

namespace Adact.Engine.Tests.Smoke;

/// <summary>Contains tests for the Notepadpp Smoke behavior.</summary>
[Trait("Layer", "Smoke")]
[Collection("UiaSerial")]
public class NotepadppSmokeTests : IAsyncLifetime
{
    private const string ProcessName = "notepad++";
    private static readonly string[] CandidatePaths =
    {
        @"C:\Program Files\Notepad++\notepad++.exe",
        @"C:\Program Files (x86)\Notepad++\notepad++.exe",
    };

    private Process? _process;
    private string? _exePath;

    /// <summary>Initializes the fixture.</summary>
    public async Task InitializeAsync()
    {
        InteractiveTestGuard.SkipIfNotInteractive();

        foreach (var p in CandidatePaths)
        {
            if (File.Exists(p)) { _exePath = p; break; }
        }
        if (_exePath is null) return; // skip-able via guard in test

        _process = Process.Start(new ProcessStartInfo
        {
            FileName = _exePath,
            UseShellExecute = true,
        });

        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromSeconds(10))
        {
            if (Process.GetProcessesByName(ProcessName).Any(p => p.MainWindowHandle != IntPtr.Zero)) break;
            await Task.Delay(200);
        }
        await Task.Delay(800);
    }

    /// <summary>Releases resources.</summary>
    public Task DisposeAsync()
    {
        foreach (var p in Process.GetProcessesByName(ProcessName))
        {
            try { p.CloseMainWindow(); p.WaitForExit(2000); } catch { }
            try { if (!p.HasExited) p.Kill(); } catch { }
        }
        return Task.CompletedTask;
    }

    /// <summary>Performs the Snapshot On Notepadpp Contains Menu Bar Or File Menu operation.</summary>
    [InteractiveFact]
    public async Task Snapshot_OnNotepadpp_ContainsMenuBarOrFileMenu()
    {

        using var engine = new UiaEngine();
        var windows = await engine.ListWindowsAsync();
        var target = windows.FirstOrDefault(w =>
            string.Equals(w.ProcessName, ProcessName, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(target);
        using var session = await engine.AttachByHandleAsync(target!.NativeWindowHandle);
        var snap = await session.SnapshotAsync();

        using var doc = JsonDocument.Parse(snap.Json);
        var hasFileMenu = ContainsValue(doc.RootElement.GetProperty("tree"), "name", "File")
                       || ContainsRole(doc.RootElement.GetProperty("tree"), "MenuBar");
        Assert.True(hasFileMenu, "Expected to find a MenuBar or a File menu in Notepad++ snapshot.");
    }

    private static bool ContainsValue(JsonElement node, string key, string val)
    {
        if (node.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String
            && string.Equals(v.GetString(), val, StringComparison.OrdinalIgnoreCase))
            return true;
        if (node.TryGetProperty("children", out var c))
            foreach (var ch in c.EnumerateArray())
                if (ContainsValue(ch, key, val)) return true;
        return false;
    }

    private static bool ContainsRole(JsonElement node, string role)
        => ContainsValue(node, "role", role);
}
