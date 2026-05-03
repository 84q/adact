using System.Diagnostics;

using Adact.Tests.Common;

using Xunit;

namespace Adact.Engine.Tests.IntegrationUia;

/// <summary>
/// 実電卓 (CalculatorApp) を起動し、UiaEngine.AttachByHandleAsync → SnapshotAsync の一連動作を検証する L3 テスト。
/// 実 UIA スタックとの結合退行を防ぐため、実アプリを必要とする。
/// </summary>
[Trait("Layer", "IntegrationUia")]
[Collection("UiaSerial")]
public class CalculatorSnapshotTests : IAsyncLifetime, IDisposable
{
    private Process? _process;
    private CalculatorMutex? _calcLock;

    /// <summary>
    /// 既存電卓を終了したうえで calc.exe を起動し、CalculatorApp.exe が現れるまで待機する。
    /// </summary>
    /// <returns>起動完了タスク。</returns>
    public async Task InitializeAsync()
    {
        InteractiveTestGuard.SkipIfNotInteractive();
        _calcLock = new CalculatorMutex();

        _process = await CalculatorTestHelper.StartFreshCalculatorAsync(TimeSpan.FromSeconds(10));
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
    public async Task DisposeAsync()
    {
        CalculatorTestHelper.KillCalculatorProcesses();
        if (_process is not null)
        {
            try { _process.Dispose(); } catch { }
        }
        await Task.CompletedTask;
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
        var snap = await session.SnapshotAsync();

        Assert.StartsWith("s", snap.SessionId);
        Assert.Equal("電卓", snap.WindowTitle);
        Assert.Contains("Button", snap.Json); // 何かしら Button が含まれる
    }
}
