using System.Diagnostics;

using Adact.Tests.Common;

using Xunit;

namespace Adact.Engine.Tests.IntegrationUia;

/// <summary>
/// SampleApp を起動し、UiaEngine.AttachByHandleAsync → SnapshotAsync の一連動作を検証する L3 テスト。
/// 実 UIA スタックとの結合退行を防ぐため、実アプリを必要とする。
/// </summary>
[Trait("Layer", "IntegrationUia")]
[Collection("UiaSerial")]
public class SampleAppSnapshotTests : IAsyncLifetime, IDisposable
{
    private Process? _process;
    private SampleAppMutex? _appLock;

    /// <summary>
    /// 既存 SampleApp を終了したうえで SampleApp を起動し、メインウィンドウが現れるまで待機する。
    /// </summary>
    /// <returns>起動完了タスク。</returns>
    public async Task InitializeAsync()
    {
        InteractiveTestGuard.SkipIfNotInteractive();
        _appLock = new SampleAppMutex();

        _process = await SampleAppTestHelper.StartFreshSampleAppAsync(TimeSpan.FromSeconds(10));
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
    public async Task DisposeAsync()
    {
        SampleAppTestHelper.KillSampleAppProcesses();
        if (_process is not null)
        {
            try { _process.Dispose(); } catch { }
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// SampleApp ウィンドウに attach して snapshot した際、sessionId 採番 / タイトル / Button ノードが含まれることを確認する。
    /// 実 UIA ツリーと RefRegistry の結合動作の回帰を L3 で検出するため。
    /// </summary>
    /// <returns>テスト完了タスク。</returns>
    [InteractiveFact]
    public async Task Snapshot_OnSampleApp_ContainsExpectedNodes()
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
        var snap = await session.SnapshotAsync();

        Assert.StartsWith("s", snap.SessionId);
        Assert.Equal("ADACT SampleApp", snap.WindowTitle);
        Assert.Contains("Button", snap.Json); // BasicControls 等に Button が含まれる
    }
}
