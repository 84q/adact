using System.Diagnostics;

using Xunit;

namespace Adact.Engine.Tests.IntegrationUia;

/// <summary>
/// 実電卓 (CalculatorApp) を起動し、UiaEngine.AttachByHandleAsync → SnapshotAsync の一連動作を検証する L3 テスト。
/// 実 UIA スタックとの結合退行を防ぐため、実アプリを必要とする。
/// </summary>
[Trait("Layer", "IntegrationUia")]
[Collection("UiaSerial")]
public class CalculatorSnapshotTests : IAsyncLifetime
{
    private Process? _process;

    /// <summary>
    /// 既存電卓を終了したうえで calc.exe を起動し、CalculatorApp.exe が現れるまで待機する。
    /// </summary>
    /// <returns>起動完了タスク。</returns>
    public async Task InitializeAsync()
    {
        InteractiveTestGuard.SkipIfNotInteractive();

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

        _process = Process.Start(new ProcessStartInfo
        {
            FileName = "calc.exe",
            UseShellExecute = true,
        });
        // 電卓は launcher 経由で別プロセスに置き換わる。CalculatorApp.exe が起動するのを待つ。
        await WaitForProcessAsync("CalculatorApp", TimeSpan.FromSeconds(10));
        await Task.Delay(800); // ウィンドウ描画安定待ち
    }

    /// <summary>
    /// 電卓プロセスをクリーンアップする。
    /// </summary>
    /// <returns>解放完了タスク。</returns>
    public async Task DisposeAsync()
    {
        foreach (var p in Process.GetProcessesByName("CalculatorApp"))
        {
            try { p.Kill(); p.WaitForExit(2000); } catch { }
        }
        if (_process is not null)
        {
            try { _process.Dispose(); } catch { }
        }
        await Task.CompletedTask;
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
    /// 電卓ウィンドウに attach して snapshot した際、sessionId 採番 / タイトル / Button ノードが含まれることを確認する。
    /// 実 UIA ツリーと RefRegistry の結合動作の回帰を L3 で検出するため。
    /// </summary>
    /// <returns>テスト完了タスク。</returns>
    [InteractiveFact]
    public async Task Snapshot_OnCalculator_ContainsExpectedNodes()
    {
        using var engine = new UiaEngine();
        // UWP 電卓は ApplicationFrameHost が見えるウィンドウを所有するため、ProcessName でなく
        // ウィンドウタイトル (日本語ロケール: "電卓") で window を特定し、HWND で attach する。
        var windows = await engine.ListWindowsAsync();
        var target = windows.FirstOrDefault(w =>
            string.Equals(w.Title, "電卓", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(target);
        using var session = await engine.AttachByHandleAsync(target!.NativeWindowHandle);
        var snap = await session.SnapshotAsync();

        Assert.StartsWith("s", snap.SessionId);
        Assert.Equal("電卓", snap.WindowTitle);
        Assert.Contains("Button", snap.Json); // 何かしら Button が含まれる
    }
}
