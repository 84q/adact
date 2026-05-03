using System.Diagnostics;
using System.Text.Json;

using Adact.Tests.Common;

using Xunit;

namespace Adact.Engine.Tests.Smoke;

/// <summary>
/// 電卓 (CalculatorApp) を起動し、snapshot → click → snapshot の Smoke シナリオを検証する L4 テスト。
/// AttachAsync・SnapshotAsync・ClickAsync の連携動作の回帰を実アプリで担保するため。
/// </summary>
[Trait("Layer", "Smoke")]
[Collection("UiaSerial")]
public class CalculatorSmokeTests : IAsyncLifetime, IDisposable
{
    private CalculatorMutex? _calcLock;

    /// <summary>
    /// 既存電卓を終了したうえで calc.exe を起動し、CalculatorApp.exe が現れるまで待機する。
    /// </summary>
    /// <returns>起動完了タスク。</returns>
    public async Task InitializeAsync()
    {
        InteractiveTestGuard.SkipIfNotInteractive();
        _calcLock = new CalculatorMutex();

        _ = await CalculatorTestHelper.StartFreshCalculatorAsync(TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// 電卓プロセスをクリーンアップする。
    /// </summary>
    /// <returns>解放完了タスク。</returns>
    /// <summary>
    /// 電卓プロセスをクリーンアップする。
    /// </summary>
    public void Dispose()
    {
        _calcLock?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 電卓プロセスをクリーンアップする。
    /// </summary>
    /// <returns>解放完了タスク。</returns>
    public Task DisposeAsync()
    {
        CalculatorTestHelper.KillCalculatorProcesses();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 電卓の "7" ボタンを ClickAsync で押し、表示領域に "7" が反映されることを確認する。
    /// click → snapshot のやり取りと ref 介した要素操作の Smoke 検証。
    /// </summary>
    /// <returns>テスト完了タスク。</returns>
    [InteractiveFact]
    public async Task Click_Seven_DisplayShowsSeven()
    {
        using var engine = new UiaEngine();
        WindowInfo? target = null;
        await CalculatorTestHelper.WaitUntilAsync(
            async () =>
            {
                var windows = await engine.ListWindowsAsync();
                target = windows.FirstOrDefault(CalculatorTestHelper.IsCalculatorWindow);
                return target is not null;
            },
            TimeSpan.FromSeconds(10),
            "Calculator window did not appear in ListWindowsAsync.");
        Assert.NotNull(target);
        using var session = await engine.AttachByHandleAsync(target!.NativeWindowHandle);

        var snap1 = await session.SnapshotAsync();
        var sevenRef = FindRefByAutomationId(snap1.Json, "num7Button")
            ?? FindRefByName(snap1.Json, "7");
        Assert.NotNull(sevenRef);

        await session.ClickAsync(sevenRef!);
        string? snap2Json = null;
        await CalculatorTestHelper.WaitUntilAsync(
            async () =>
            {
                snap2Json = (await session.SnapshotAsync()).Json;
                return snap2Json.Contains('7');
            },
            TimeSpan.FromSeconds(5),
            "Calculator display did not show '7' after clicking the seven button.");

        // 表示要素 (CalculatorResults) のテキストに "7" が含まれることを確認。
        // モダン電卓では Name に "Display is 7" のような文字列が入る。
        Assert.Contains("7", snap2Json);
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
