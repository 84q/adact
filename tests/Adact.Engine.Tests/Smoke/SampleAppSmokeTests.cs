using System.Diagnostics;
using System.Text.Json;

using Adact.Tests.Common;

using Xunit;

namespace Adact.Engine.Tests.Smoke;

/// <summary>
/// SampleApp を起動し、snapshot → click → snapshot の Smoke シナリオを検証する L4 テスト。
/// AttachAsync・SnapshotAsync・ClickAsync の連携動作の回帰を実アプリで担保するため。
/// </summary>
[Trait("Layer", "Smoke")]
[Collection("UiaSerial")]
public class SampleAppSmokeTests : IAsyncLifetime, IDisposable
{
    private SampleAppMutex? _appLock;

    /// <summary>
    /// 既存 SampleApp を終了したうえで SampleApp を起動し、メインウィンドウが現れるまで待機する。
    /// </summary>
    /// <returns>起動完了タスク。</returns>
    public async Task InitializeAsync()
    {
        InteractiveTestGuard.SkipIfNotInteractive();
        _appLock = new SampleAppMutex();

        _ = await SampleAppTestHelper.StartFreshSampleAppAsync(TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// SampleApp プロセスをクリーンアップする。
    /// </summary>
    public void Dispose()
    {
        _appLock?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// SampleApp プロセスをクリーンアップする。
    /// </summary>
    /// <returns>解放完了タスク。</returns>
    public Task DisposeAsync()
    {
        SampleAppTestHelper.KillSampleAppProcesses();
        return Task.CompletedTask;
    }

    /// <summary>
    /// SampleApp の Submit ボタンを ClickAsync で押し、StatusLabel に "Submitted" が反映されることを確認する。
    /// click → snapshot のやり取りと ref 介した要素操作の Smoke 検証。
    /// </summary>
    /// <returns>テスト完了タスク。</returns>
    [InteractiveFact]
    public async Task Click_Submit_StatusLabelShowsSubmitted()
    {
        using var engine = new UiaEngine();
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
        using var session = await engine.AttachByHandleAsync(target!.NativeWindowHandle);

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
                // StatusLabel (BasicControls_Label_Status) のテキストが "Submitted" を含むことを確認
                return snap2Json.Contains("Submitted", StringComparison.Ordinal);
            },
            TimeSpan.FromSeconds(5),
            "StatusLabel did not show 'Submitted' after clicking the Submit button.");

        Assert.Contains("Submitted", snap2Json);
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
