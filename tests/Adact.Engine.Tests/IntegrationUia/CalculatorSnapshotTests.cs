using System.Diagnostics;

using Xunit;

namespace Adact.Engine.Tests.IntegrationUia;

[Trait("Layer", "IntegrationUia")]
[Collection("UiaSerial")]
public class CalculatorSnapshotTests : IAsyncLifetime
{
    private Process? _process;

    public async Task InitializeAsync()
    {
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

    [Fact]
    public async Task Snapshot_OnCalculator_ContainsExpectedNodes()
    {
        using var engine = new UiaEngine();
        // UWP 電卓は ApplicationFrameHost が見えるウィンドウを所有するため、ProcessName でなく
        // ウィンドウタイトル (日本語ロケール: "電卓") でアタッチする。
        using var session = await engine.AttachAsync(AttachQuery.ByTitle("電卓"));
        var snap = await session.SnapshotAsync();

        Assert.Equal(1, snap.Generation);
        Assert.StartsWith("s", snap.SessionId);
        Assert.Equal("電卓", snap.WindowTitle);
        Assert.Contains("Button", snap.Json); // 何かしら Button が含まれる
    }
}
