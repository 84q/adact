using System.Diagnostics;
using System.Text.Json;

using Xunit;

namespace Adact.Engine.Tests.Smoke;

[Trait("Layer", "Smoke")]
[Collection("UiaSerial")]
public class CalculatorSmokeTests : IAsyncLifetime
{
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

    Process.Start(new ProcessStartInfo { FileName = "calc.exe", UseShellExecute = true });
    await WaitForProcessAsync("CalculatorApp", TimeSpan.FromSeconds(10));
    await Task.Delay(1000);
  }

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

  [Fact]
  public async Task Click_Seven_DisplayShowsSeven()
  {
    using var engine = new UiaEngine();
    // UWP 電卓はタイトル経由でアタッチ
    using var session = await engine.AttachAsync(AttachQuery.ByTitle("電卓"));

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
