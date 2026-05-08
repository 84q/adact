using System.Diagnostics;
using System.Text.Json;

using Adact.Tests.Common;

using Xunit;

namespace Adact.Engine.Tests.Smoke;

/// <summary>Contains tests for the Sample App Smoke behavior.</summary>
[Trait("Layer", "Smoke")]
[Collection("UiaSerial")]
public class SampleAppSmokeTests : IAsyncLifetime, IDisposable
{
    private SampleAppMutex? _appLock;

    /// <summary>Initializes the fixture.</summary>
    public async Task InitializeAsync()
    {
        InteractiveTestGuard.SkipIfNotInteractive();
        _appLock = new SampleAppMutex();

        _ = await SampleAppTestHelper.StartFreshSampleAppAsync(TimeSpan.FromSeconds(10));
    }

    /// <summary>Releases resources.</summary>
    public void Dispose()
    {
        _appLock?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases resources.</summary>
    public Task DisposeAsync()
    {
        SampleAppTestHelper.KillSampleAppProcesses();
        return Task.CompletedTask;
    }

    /// <summary>Performs the Click Submit Status Label Shows Submitted operation.</summary>
    [InteractiveFact]
    public async Task Click_Submit_StatusLabelShowsSubmitted()
    {
        using var engine = new UiaEngine();
        using var session = await AttachToSampleAppAsync(engine);

        var snap1 = await session.SnapshotAsync();
        var submitRef = FindRefByAutomationId(snap1.Json, "BasicControls_Button_Submit")
            ?? FindRefByName(snap1.Json, "Submit Button");
        Assert.NotNull(submitRef);

        await session.ClickAsync(submitRef!);
        string? snap2Json = null;
        await SampleAppTestHelper.WaitUntilAsync(
            async () =>
            {
                snap2Json = (await session.SnapshotAsync()).Json;
                return snap2Json.Contains("Submitted", StringComparison.Ordinal);
            },
            TimeSpan.FromSeconds(5),
            "StatusLabel did not show 'Submitted' after clicking the Submit button.");

        Assert.Contains("Submitted", snap2Json);
    }

    /// <summary>Performs the Close On Block Close Toggle Respects Checked State operation.</summary>
    [InteractiveFact]
    public async Task Close_OnBlockCloseToggle_RespectsCheckedState()
    {
        using var engine = new UiaEngine();
        using var session = await AttachToSampleAppAsync(engine);

        await ClickMenuItemAsync(
            session,
            "MainWindow_MenuItem_File",
            "MainWindow_MenuItem_File_BlockClose");

        await session.CloseAsync();

        await SampleAppTestHelper.WaitUntilAsync(
            async () => await IsSampleAppWindowOpenAsync(engine),
            TimeSpan.FromSeconds(3),
            "SampleApp window disappeared even though Block Close was enabled.");

        await ClickMenuItemAsync(
            session,
            "MainWindow_MenuItem_File",
            "MainWindow_MenuItem_File_BlockClose");

        await session.CloseAsync();

        await SampleAppTestHelper.WaitUntilAsync(
            async () => !await IsSampleAppWindowOpenAsync(engine),
            TimeSpan.FromSeconds(5),
            "SampleApp window did not close after Block Close was disabled.");
    }

    private static async Task<IWindowSession> AttachToSampleAppAsync(UiaEngine engine)
    {
        WindowInfo? target = null;
        await SampleAppTestHelper.WaitUntilAsync(
            async () =>
            {
                var windows = await engine.ListWindowsAsync();
                target = windows.FirstOrDefault(SampleAppTestHelper.IsSampleAppWindow);
                return target is not null;
            },
            TimeSpan.FromSeconds(10),
            "SampleApp window did not appear in ListWindowsAsync.");

        Assert.NotNull(target);
        return await engine.AttachByHandleAsync(target!.NativeWindowHandle);
    }

    private static async Task ClickMenuItemAsync(
        IWindowSession session,
        string rootMenuAutomationId,
        string childMenuAutomationId)
    {
        var fileMenuRef = await WaitForRefByAutomationIdAsync(
            session,
            rootMenuAutomationId,
            TimeSpan.FromSeconds(5),
            $"Menu '{rootMenuAutomationId}' was not found in SampleApp snapshot.");
        await session.ClickAsync(fileMenuRef);

        var childRef = await WaitForRefByAutomationIdAsync(
            session,
            childMenuAutomationId,
            TimeSpan.FromSeconds(5),
            $"Menu item '{childMenuAutomationId}' did not appear after opening '{rootMenuAutomationId}'.");
        await session.ClickAsync(childRef);
    }

    private static async Task<string> WaitForRefByAutomationIdAsync(
        IWindowSession session,
        string automationId,
        TimeSpan timeout,
        string failureMessage)
    {
        string? foundRef = null;
        await SampleAppTestHelper.WaitUntilAsync(
            async () =>
            {
                var snapshot = await session.SnapshotAsync();
                foundRef = FindRefByAutomationId(snapshot.Json, automationId);
                return foundRef is not null;
            },
            timeout,
            failureMessage);

        return foundRef!;
    }

    private static async Task<bool> IsSampleAppWindowOpenAsync(UiaEngine engine)
    {
        var windows = await engine.ListWindowsAsync();
        return windows.Any(SampleAppTestHelper.IsSampleAppWindow);
    }

    private static string? FindRefByAutomationId(string json, string automationId)
        => Find(json, "automationId", automationId);

    private static string? FindRefByName(string json, string name)
        => Find(json, "name", name);

    private static string? Find(string json, string keyName, string keyValue)
    {
        using var doc = JsonDocument.Parse(json);
        return Walk(doc.RootElement.GetProperty("tree"), keyName, keyValue);
    }

    private static string? Walk(JsonElement node, string keyName, string keyValue)
    {
        if (node.TryGetProperty(keyName, out var v) && v.ValueKind == JsonValueKind.String && v.GetString() == keyValue)
            return node.GetProperty("ref").GetString();
        if (node.TryGetProperty("children", out var children))
        {
            foreach (var ch in children.EnumerateArray())
            {
                var found = Walk(ch, keyName, keyValue);
                if (found is not null) return found;
            }
        }
        return null;
    }
}
