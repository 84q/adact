using System.Diagnostics;
using System.Text.Json;

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
        using var session = await AttachToSampleAppAsync(engine);
        var snap = await session.SnapshotAsync();

        Assert.StartsWith("s", snap.SessionId);
        Assert.Equal("ADACT SampleApp", snap.WindowTitle);
        Assert.Contains("Button", snap.Json); // BasicControls 等に Button が含まれる
    }

    /// <summary>
    /// View &gt; Layout &gt; Navigation Pane を順に開いた snapshot に深い階層の Favorites 項目が現れることを確認する。
    /// Popup メニューが UIA/snapshot 上で辿れることを L3 で退行検出するため。
    /// </summary>
    /// <returns>テスト完了タスク。</returns>
    [InteractiveFact]
    public async Task Snapshot_AfterOpeningNestedViewMenu_ContainsFavoritesItem()
    {
        using var engine = new UiaEngine();
        using var session = await AttachToSampleAppAsync(engine);

        await ClickMenuAsync(session, "MainWindow_MenuItem_View");
        var layoutRef = await WaitForRefByAutomationIdAsync(
            session,
            "MainWindow_MenuItem_View_Layout",
            TimeSpan.FromSeconds(5),
            "Layout menu item did not appear after opening View.");

        await session.ClickAsync(layoutRef);
        var navigationPaneRef = await WaitForRefByAutomationIdAsync(
            session,
            "MainWindow_MenuItem_View_Layout_NavigationPane",
            TimeSpan.FromSeconds(5),
            "Navigation Pane menu item did not appear after opening Layout.");

        await session.ClickAsync(navigationPaneRef);
        var favoritesRef = await WaitForRefByAutomationIdAsync(
            session,
            "MainWindow_MenuItem_View_Layout_NavigationPane_Favorites",
            TimeSpan.FromSeconds(5),
            "Favorites menu item did not appear after opening Navigation Pane.");

        Assert.False(string.IsNullOrWhiteSpace(favoritesRef));
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

    private static async Task ClickMenuAsync(IWindowSession session, string automationId)
    {
        var menuRef = await WaitForRefByAutomationIdAsync(
            session,
            automationId,
            TimeSpan.FromSeconds(5),
            $"Menu '{automationId}' was not found in SampleApp snapshot.");
        await session.ClickAsync(menuRef);
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

    private static string? FindRefByAutomationId(string json, string automationId)
    {
        using var doc = JsonDocument.Parse(json);
        return Walk(doc.RootElement.GetProperty("tree"), automationId);
    }

    private static string? Walk(JsonElement node, string automationId)
    {
        if (node.TryGetProperty("automationId", out var value)
            && value.ValueKind == JsonValueKind.String
            && value.GetString() == automationId)
        {
            return node.GetProperty("ref").GetString();
        }

        if (!node.TryGetProperty("children", out var children))
        {
            return null;
        }

        foreach (var child in children.EnumerateArray())
        {
            var found = Walk(child, automationId);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }
}
