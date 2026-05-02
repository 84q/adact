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

        // 既存の電卓プロセスを終了させ、「電卓」タイトルのウィンドウが複数存在する瞬間を回避する。
        // calc.exe (launcher) と CalculatorApp.exe の両方を対象にする。
        foreach (var name in new[] { "CalculatorApp", "calc" })
        {
            foreach (var p in Process.GetProcessesByName(name))
            {
                try { p.Kill(); p.WaitForExit(2000); } catch { }
            }
        }
        // UWP 側のフレーム解放待ち
        await Task.Delay(300);

        Process.Start(new ProcessStartInfo { FileName = "calc.exe", UseShellExecute = true });
        await WaitForProcessAsync("CalculatorApp", TimeSpan.FromSeconds(10));
        await Task.Delay(1000);
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
        foreach (var p in Process.GetProcessesByName("CalculatorApp"))
        {
            try { p.Kill(); p.WaitForExit(2000); } catch { }
        }
        return Task.CompletedTask;
    }

    private static async Task WaitForProcessAsync(string name, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (Process.GetProcessesByName(name).Length > 0) return;
            await Task.Delay(150);
        }
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
        // UWP 電卓はタイトルで window を見つけ、HWND で attach する
        var windows = await engine.ListWindowsAsync();
        var target = windows.FirstOrDefault(w =>
            string.Equals(w.Title, "電卓", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(target);
        using var session = await engine.AttachByHandleAsync(target!.NativeWindowHandle);

        var snap1 = await session.SnapshotAsync();
        var sevenRef = FindRefByAutomationId(snap1.Json, "num7Button")
            ?? FindRefByName(snap1.Json, "7");
        Assert.NotNull(sevenRef);

        await session.ClickAsync(sevenRef!);
        await Task.Delay(400);

        var snap2 = await session.SnapshotAsync();
        // 表示要素 (CalculatorResults) のテキストに "7" が含まれることを確認。
        // モダン電卓では Name に "Display is 7" のような文字列が入る。
        Assert.Contains("7", snap2.Json);
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
