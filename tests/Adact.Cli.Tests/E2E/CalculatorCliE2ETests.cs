using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

using Xunit;

namespace Adact.Cli.Tests.E2E;

/// <summary>
/// L5 E2E: <c>adact.exe</c> サブプロセスを直接起動して daemon に接続し、実 calc.exe を操作する通しフロー。
/// 設計 009 §9.2 (E2E シナリオ): list-apps → attach → snapshot → click → close。
/// </summary>
[Trait("Layer", "E2E")]
[Collection("AdactCli")]
public class CalculatorCliE2ETests
{
  private readonly AdactDaemonFixture _fixture;

  public CalculatorCliE2ETests(AdactDaemonFixture fixture)
  {
    _fixture = fixture;
  }

  [Fact]
  public void ListAttachSnapshotClickCloseFlow_OnCalculator_Succeeds()
  {
    using var _calcLock = new CalculatorMutex();

    // snapshot を一時ディレクトリに書き出すため、cwd = 専用 temp dir。
    var tempDir = Path.Combine(Path.GetTempPath(), "adact-cli-e2e-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tempDir);

    var calculator = StartCalculator();
    try
    {
      // (1) list-apps → 電卓行から windowRef を抽出
      var listResult = CliProcess.RunWithServer("list-apps", _fixture.BaseUrl, tempDir);
      Assert.True(listResult.ExitCode == 0,
          $"list-apps exit={listResult.ExitCode}\nstdout: {listResult.Stdout}\nstderr: {listResult.Stderr}");

      var windowRef = ExtractCalculatorWindowRef(listResult.Stdout);
      Assert.False(string.IsNullOrEmpty(windowRef),
          $"Calculator windowRef not found in list-apps output:\n{listResult.Stdout}");

      // (2) attach <windowRef>
      var attachResult = CliProcess.RunWithServer($"attach {windowRef}", _fixture.BaseUrl, tempDir);
      Assert.True(attachResult.ExitCode == 0,
          $"attach exit={attachResult.ExitCode}\nstdout: {attachResult.Stdout}\nstderr: {attachResult.Stderr}");

      // 設計 §5.2 (011 §4.5): stdout に sessionId / windowRef / snapshot 行が出る (generation は廃止)。
      var sessionId = ExtractKeyValue(attachResult.Stdout, "sessionId");
      Assert.False(string.IsNullOrEmpty(sessionId),
          $"sessionId not found in attach stdout:\n{attachResult.Stdout}");

      Assert.Null(ExtractKeyValue(attachResult.Stdout, "generation"));

      var snapshotPath = ExtractKeyValue(attachResult.Stdout, "snapshot");
      Assert.False(string.IsNullOrEmpty(snapshotPath),
          $"snapshot path not found in attach stdout:\n{attachResult.Stdout}");

      // snapshot ファイルは cwd (tempDir) の .adact/ 下に出力される (設計 §4.4)。
      var resolvedSnapshot = Path.IsPathRooted(snapshotPath!)
          ? snapshotPath!
          : Path.Combine(tempDir, snapshotPath!);
      Assert.True(File.Exists(resolvedSnapshot),
          $"snapshot file not found: {resolvedSnapshot}");

      // (3) snapshot JSON から電卓ボタンの ref を抽出 → click
      var buttonRef = FindCalculatorButtonRef(resolvedSnapshot);
      Assert.False(string.IsNullOrEmpty(buttonRef),
          $"Calculator Button ref not found in snapshot file: {resolvedSnapshot}");
      // 後で同一 button の ref 比較に使うため、Name / AutomationId も取得しておく。
      var (buttonName, buttonAutomationId) = FindNodeIdentity(resolvedSnapshot, buttonRef!);

      var clickResult = CliProcess.RunWithServer($"click {buttonRef}", _fixture.BaseUrl, tempDir);
      Assert.True(clickResult.ExitCode == 0,
          $"click exit={clickResult.ExitCode}\nstdout: {clickResult.Stdout}\nstderr: {clickResult.Stderr}");

      // generation 行は廃止された。
      Assert.Null(ExtractKeyValue(clickResult.Stdout, "generation"));

      var clickSnapshotPath = ExtractKeyValue(clickResult.Stdout, "snapshot");
      Assert.False(string.IsNullOrEmpty(clickSnapshotPath),
          $"snapshot path not found in click stdout:\n{clickResult.Stdout}");
      var resolvedClickSnapshot = Path.IsPathRooted(clickSnapshotPath!)
          ? clickSnapshotPath!
          : Path.Combine(tempDir, clickSnapshotPath!);
      Assert.True(File.Exists(resolvedClickSnapshot),
          $"click snapshot file not found: {resolvedClickSnapshot}");
      Assert.NotEqual(resolvedSnapshot, resolvedClickSnapshot);

      // ref 安定化 (011 §4): click 後の自動 snapshot でも、同じボタン
      // (Name / AutomationId 一致) は同じ ref を返す。
      var refAfterClick = FindRefByIdentity(resolvedClickSnapshot, buttonName, buttonAutomationId);
      Assert.False(string.IsNullOrEmpty(refAfterClick),
          $"button not found in post-click snapshot: {resolvedClickSnapshot}");
      Assert.Equal(buttonRef, refAfterClick);

      // (4) close --sid <sid>
      var closeResult = CliProcess.RunWithServer(
          $"close --sid {sessionId}", _fixture.BaseUrl, tempDir);
      Assert.True(closeResult.ExitCode == 0,
          $"close exit={closeResult.ExitCode}\nstdout: {closeResult.Stdout}\nstderr: {closeResult.Stderr}");
      Assert.Contains("closed", closeResult.Stdout, StringComparison.Ordinal);
      Assert.Contains("detached", closeResult.Stdout, StringComparison.Ordinal);
    }
    finally
    {
      foreach (var p in Process.GetProcessesByName("CalculatorApp"))
      {
        try { p.Kill(); p.WaitForExit(2000); } catch { }
      }
      try { calculator?.Dispose(); } catch { }
      try { Directory.Delete(tempDir, recursive: true); } catch { }
    }
  }

  /// <summary>
  /// UWP 電卓 (calc.exe → CalculatorApp.exe) を起動し、UIA で見える状態になるまで待機する。
  /// 10 秒以内に CalculatorApp プロセスが見つからなければ <see cref="InvalidOperationException"/> で fast-fail する。
  /// </summary>
  private static Process? StartCalculator()
  {
    var p = Process.Start(new ProcessStartInfo
    {
      FileName = "calc.exe",
      UseShellExecute = true,
    });
    var sw = Stopwatch.StartNew();
    while (sw.Elapsed < TimeSpan.FromSeconds(10))
    {
      if (Process.GetProcessesByName("CalculatorApp").Length > 0)
      {
        // UWP 特有の安定化待ち: ウィンドウが UIA ツリーに登録されるまでの猶予。
        Thread.Sleep(1000);
        return p;
      }
      Thread.Sleep(150);
    }
    throw new InvalidOperationException("CalculatorApp did not start within 10s");
  }

  /// <summary>
  /// list-apps の TSV 出力から processName が CalculatorApp あるいは windowTitle が "電卓"
  /// を含む行の windowRef (列 0) を返す。
  /// </summary>
  private static string? ExtractCalculatorWindowRef(string stdout)
  {
    var lines = stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
    // 0 行目はヘッダ。
    foreach (var line in lines.Skip(1))
    {
      var cols = line.Split('\t');
      if (cols.Length < 6) continue;

      var processName = cols[2];
      var windowTitle = cols[5];

      if (processName.Contains("Calculator", StringComparison.OrdinalIgnoreCase)
          || windowTitle.Contains("電卓", StringComparison.Ordinal)
          || windowTitle.Contains("Calculator", StringComparison.OrdinalIgnoreCase))
      {
        return cols[0];
      }
    }
    return null;
  }

  /// <summary>
  /// key-value 形式 stdout (設計 §5.1) から指定 key の値を抽出する。
  /// 区切りは空白 1 個。値に空白を含む場合 (snapshot path 等) も最初の空白以降全てを返す。
  /// </summary>
  private static string? ExtractKeyValue(string stdout, string key)
  {
    var lines = stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
    foreach (var line in lines)
    {
      var idx = line.IndexOf(' ');
      if (idx <= 0) continue;
      if (!string.Equals(line[..idx], key, StringComparison.Ordinal)) continue;
      return line[(idx + 1)..];
    }
    return null;
  }

  /// <summary>
  /// snapshot JSON ファイルから電卓のボタン要素の ref を探す。
  /// 優先度: AutomationId == "num1Button" > role == "Button" の最初。
  /// </summary>
  private static string? FindCalculatorButtonRef(string snapshotFilePath)
  {
    using var doc = JsonDocument.Parse(File.ReadAllText(snapshotFilePath));
    if (!doc.RootElement.TryGetProperty("tree", out var tree))
    {
      return null;
    }

    // まず AutomationId 一致を優先。
    var byAutomationId = FindRefRecursive(tree, n =>
        n.TryGetProperty("automationId", out var aid)
        && aid.ValueKind == JsonValueKind.String
        && aid.GetString() == "num1Button");
    if (byAutomationId is not null) return byAutomationId;

    // フォールバック: role == "Button" の最初。
    return FindRefRecursive(tree, n =>
        n.TryGetProperty("role", out var r)
        && r.ValueKind == JsonValueKind.String
        && r.GetString() == "Button");
  }

  private static string? FindRefRecursive(JsonElement node, Func<JsonElement, bool> predicate)
  {
    if (node.ValueKind != JsonValueKind.Object) return null;

    if (predicate(node) && node.TryGetProperty("ref", out var refProp)
        && refProp.ValueKind == JsonValueKind.String)
    {
      return refProp.GetString();
    }

    if (node.TryGetProperty("children", out var children)
        && children.ValueKind == JsonValueKind.Array)
    {
      foreach (var child in children.EnumerateArray())
      {
        var found = FindRefRecursive(child, predicate);
        if (found is not null) return found;
      }
    }
    return null;
  }

  /// <summary>snapshot から ref に対応するノードの (Name, AutomationId) を抽出する。</summary>
  private static (string? name, string? automationId) FindNodeIdentity(string snapshotFilePath, string targetRef)
  {
    using var doc = JsonDocument.Parse(File.ReadAllText(snapshotFilePath));
    if (!doc.RootElement.TryGetProperty("tree", out var tree)) return (null, null);

    string? foundName = null;
    string? foundAutomationId = null;
    FindRefRecursive(tree, n =>
    {
      if (n.TryGetProperty("ref", out var r)
          && r.ValueKind == JsonValueKind.String
          && r.GetString() == targetRef)
      {
        foundName = n.TryGetProperty("name", out var na) && na.ValueKind == JsonValueKind.String ? na.GetString() : null;
        foundAutomationId = n.TryGetProperty("automationId", out var aid) && aid.ValueKind == JsonValueKind.String ? aid.GetString() : null;
        return true;
      }
      return false;
    });
    return (foundName, foundAutomationId);
  }

  /// <summary>(Name, AutomationId) 一致するノードの ref を snapshot から返す。</summary>
  private static string? FindRefByIdentity(string snapshotFilePath, string? name, string? automationId)
  {
    using var doc = JsonDocument.Parse(File.ReadAllText(snapshotFilePath));
    if (!doc.RootElement.TryGetProperty("tree", out var tree)) return null;
    return FindRefRecursive(tree, n =>
    {
      var nName = n.TryGetProperty("name", out var na) && na.ValueKind == JsonValueKind.String ? na.GetString() : null;
      var nAid = n.TryGetProperty("automationId", out var aid) && aid.ValueKind == JsonValueKind.String ? aid.GetString() : null;
      return string.Equals(nName, name, StringComparison.Ordinal)
          && string.Equals(nAid, automationId, StringComparison.Ordinal);
    });
  }
}
