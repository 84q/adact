using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA2;
using FlaUI.UIA3;

namespace Adact.Spike.FlaUI;

internal static class Program
{
  public static int Main(string[] args)
  {
    if (args.Length == 0)
    {
      return PrintUsage();
    }

    var useUia2 = args.Contains("--uia2");
    var positional = args.Where(a => !a.StartsWith("--")).ToArray();
    if (positional.Length == 0) return PrintUsage();
    var cmd = positional[0].ToLowerInvariant();

    try
    {
      return cmd switch
      {
        "list" => CmdList(useUia2),
        "snapshot" => CmdSnapshot(positional.Skip(1).ToArray(), useUia2),
        "click" => CmdClick(positional.Skip(1).ToArray(), useUia2),
        "measure" => CmdMeasure(positional.Skip(1).ToArray(), useUia2),
        _ => PrintUsage(),
      };
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"ERROR: {ex.GetType().Name}: {ex.Message}");
      Console.Error.WriteLine(ex.StackTrace);
      return 2;
    }
  }

  private static int PrintUsage()
  {
    Console.WriteLine("Usage:");
    Console.WriteLine("  Adact.Spike.FlaUI list [--uia2]");
    Console.WriteLine("  Adact.Spike.FlaUI snapshot <processNameOrTitle> [--uia2]");
    Console.WriteLine("  Adact.Spike.FlaUI click <processNameOrTitle> <targetNameOrAutomationId> [--uia2]");
    Console.WriteLine("  Adact.Spike.FlaUI measure <processNameOrTitle> [--uia2] [--out <dir>]");
    return 1;
  }

  private static AutomationBase CreateAutomation(bool useUia2)
      => useUia2 ? (AutomationBase)new UIA2Automation() : new UIA3Automation();

  private static int CmdList(bool useUia2)
  {
    using var automation = CreateAutomation(useUia2);
    var desktop = automation.GetDesktop();
    var windows = desktop.FindAllChildren(cf => cf.ByControlType(ControlType.Window));
    Console.WriteLine($"Backend: {(useUia2 ? "UIA2" : "UIA3")}, top-level windows: {windows.Length}");
    foreach (var w in windows)
    {
      string proc = "?";
      try
      {
        var p = Process.GetProcessById(w.Properties.ProcessId.ValueOrDefault);
        proc = p.ProcessName;
      }
      catch { }
      var title = w.Properties.Name.ValueOrDefault ?? "";
      Console.WriteLine($"  pid={w.Properties.ProcessId.ValueOrDefault,-6} proc={proc,-20} ctrl={w.ControlType,-12} title=\"{title}\"");
    }
    return 0;
  }

  private static int CmdSnapshot(string[] args, bool useUia2)
  {
    if (args.Length < 1) return PrintUsage();
    var key = args[0];
    using var automation = CreateAutomation(useUia2);
    var window = FindWindow(automation, key);
    if (window == null)
    {
      Console.Error.WriteLine($"Window not found for key: {key}");
      return 3;
    }

    var sw = Stopwatch.StartNew();
    int counter = 0;
    var node = BuildNode(window, ref counter, depth: 0, maxDepth: 64);
    sw.Stop();

    var json = JsonSerializer.Serialize(node, new JsonSerializerOptions
    {
      WriteIndented = true,
      DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    });

    Console.WriteLine(json);
    var bytes = Encoding.UTF8.GetByteCount(json);
    Console.Error.WriteLine();
    Console.Error.WriteLine($"# nodes={counter} bytes={bytes} ({bytes / 1024.0:F1} KB) elapsedMs={sw.ElapsedMilliseconds} backend={(useUia2 ? "UIA2" : "UIA3")}");
    return 0;
  }

  private static SnapshotNode BuildNode(AutomationElement el, ref int counter, int depth, int maxDepth)
  {
    counter++;
    var node = new SnapshotNode
    {
      Ref = $"w{counter}",
      Role = SafeStr(() => el.ControlType.ToString()),
      Name = SafeStr(() => el.Properties.Name.ValueOrDefault),
      AutomationId = SafeStr(() => el.Properties.AutomationId.ValueOrDefault),
      ClassName = SafeStr(() => el.Properties.ClassName.ValueOrDefault),
    };
    try
    {
      var r = el.Properties.BoundingRectangle.ValueOrDefault;
      node.Rect = new[] { (int)r.X, (int)r.Y, (int)r.Width, (int)r.Height };
    }
    catch { }

    if (depth >= maxDepth) return node;

    AutomationElement[] children;
    try { children = el.FindAllChildren(); }
    catch { return node; }

    if (children.Length == 0) return node;
    node.Children = new List<SnapshotNode>(children.Length);
    foreach (var c in children)
    {
      try { node.Children.Add(BuildNode(c, ref counter, depth + 1, maxDepth)); }
      catch { }
    }
    return node;
  }

  private static string? SafeStr(Func<string?> f)
  {
    try { var s = f(); return string.IsNullOrEmpty(s) ? null : s; } catch { return null; }
  }

  private static int CmdClick(string[] args, bool useUia2)
  {
    if (args.Length < 2) return PrintUsage();
    var key = args[0];
    var target = args[1];
    using var automation = CreateAutomation(useUia2);
    var window = FindWindow(automation, key);
    if (window == null)
    {
      Console.Error.WriteLine($"Window not found: {key}");
      return 3;
    }

    var byAid = window.FindFirstDescendant(cf => cf.ByAutomationId(target));
    var found = byAid ?? window.FindFirstDescendant(cf => cf.ByName(target));
    if (found == null)
    {
      Console.Error.WriteLine($"Element not found in window. target={target}");
      return 4;
    }

    Console.WriteLine($"Found: ctrl={found.ControlType} name=\"{SafeStr(() => found.Properties.Name.ValueOrDefault)}\" aid=\"{SafeStr(() => found.Properties.AutomationId.ValueOrDefault)}\"");
    try { window.Focus(); } catch { }

    try
    {
      var btn = found.AsButton();
      btn.Invoke();
      Console.WriteLine("Invoked via Button.Invoke()");
      return 0;
    }
    catch { }

    try
    {
      found.Click();
      Console.WriteLine("Clicked via mouse Click()");
      return 0;
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"Click failed: {ex.Message}");
      return 5;
    }
  }

  private static Window? FindWindow(AutomationBase automation, string key)
  {
    var desktop = automation.GetDesktop();
    var children = desktop.FindAllChildren(cf => cf.ByControlType(ControlType.Window));
    foreach (var w in children)
    {
      try
      {
        var p = Process.GetProcessById(w.Properties.ProcessId.ValueOrDefault);
        if (string.Equals(p.ProcessName, key, StringComparison.OrdinalIgnoreCase))
          return w.AsWindow();
      }
      catch { }
    }
    foreach (var w in children)
    {
      var t = w.Properties.Name.ValueOrDefault ?? "";
      if (t.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
        return w.AsWindow();
    }
    return null;
  }

  private sealed class SnapshotNode
  {
    public string Ref { get; set; } = "";
    public string? Role { get; set; }
    public string? Name { get; set; }
    public string? AutomationId { get; set; }
    public string? ClassName { get; set; }
    public string? RuntimeId { get; set; }
    public int[]? Rect { get; set; }
    public List<SnapshotNode>? Children { get; set; }
  }

  private sealed class MeasureStats
  {
    public int Total;
    public int WithRuntimeId;
    public int EmptyRuntimeId;
    public int ExceptionRuntimeId;
  }

  private enum FilterLevel { L0, L1, L2 }

  // L1 ホワイトリスト: 仕様で指定された「操作可能 ControlType」のみ。
  // Pane/Group/ToolBar/MenuBar/StatusBar/TitleBar 等の構造保持型はあえて含めない
  // （仕様の文字通り。L2 の「無名 Pane/Group 除外」と整合する派生案は L1.5 として
  //  Phase 1-C レポートで別途提示する）。
  private static readonly HashSet<ControlType> L1Allowed = new()
    {
        ControlType.Button, ControlType.Edit, ControlType.ListItem, ControlType.Window,
        ControlType.Menu, ControlType.MenuItem, ControlType.CheckBox, ControlType.ComboBox,
        ControlType.Tab, ControlType.TabItem, ControlType.Hyperlink, ControlType.Text,
        ControlType.Document, ControlType.TreeItem,
        // 操作可能の補強（仕様の「等」に含まれると判断）
        ControlType.RadioButton, ControlType.SplitButton, ControlType.Slider, ControlType.Spinner,
    };

  // L1.5 (実用フィルタ案): L1 + 構造保持型。L2 で無名 Pane/Group を除外することで
  // 「ラベル付きの構造（ToolBar/MenuBar/StatusBar など）」を残しつつ無意味な
  // 無名コンテナを削れるようにする現実的な案。
  private static readonly HashSet<ControlType> L15ExtraStructural = new()
    {
        ControlType.Pane, ControlType.Group, ControlType.ToolBar, ControlType.MenuBar,
        ControlType.StatusBar, ControlType.TitleBar, ControlType.Header, ControlType.HeaderItem,
        ControlType.List, ControlType.Tree, ControlType.DataGrid, ControlType.DataItem,
        ControlType.Table, ControlType.Custom,
    };

  private static int CmdMeasure(string[] args, bool useUia2)
  {
    if (args.Length < 1) return PrintUsage();
    var key = args[0];

    string outDir = ".";
    for (int i = 0; i < args.Length - 1; i++)
    {
      if (args[i] == "--out") outDir = args[i + 1];
    }
    System.IO.Directory.CreateDirectory(outDir);

    using var automation = CreateAutomation(useUia2);
    var window = FindWindow(automation, key);
    if (window == null)
    {
      Console.Error.WriteLine($"Window not found for key: {key}");
      return 3;
    }

    var safe = SanitizeFile(key);
    var jsonOpts = new JsonSerializerOptions
    {
      WriteIndented = true,
      DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    Console.WriteLine($"target: {key} backend={(useUia2 ? "UIA2" : "UIA3")}");
    Console.WriteLine($"{"level",-5} {"nodes",6} {"bytes",8} {"KB",6}  ms");

    var levels = new (string label, Func<AutomationElement, bool> include)[]
    {
            ("L0",   _ => true),
            ("L1",   el => IsL1(el)),
            ("L1_5", el => IsL15(el)),
            ("L2",   el => IsL2(el)),
    };

    var report = new StringBuilder();
    report.AppendLine($"# RuntimeId / AutomationId duplication report for: {key}");
    report.AppendLine();

    foreach (var (label, include) in levels)
    {
      var sw = Stopwatch.StartNew();
      int counter = 0;
      var stats = new MeasureStats();
      var roots = BuildFiltered(window, include, ref counter, depth: 0, maxDepth: 64, isRoot: true, stats: stats);
      sw.Stop();
      var root = roots.Count == 1 ? roots[0] : new SnapshotNode { Ref = "w0", Role = "Root", Children = roots };
      int nodes = CountNodes(root);
      var json = JsonSerializer.Serialize(root, jsonOpts);
      int bytes = Encoding.UTF8.GetByteCount(json);
      var path = System.IO.Path.Combine(outDir, $"measure-{safe}-{label}.json");
      System.IO.File.WriteAllText(path, json, new UTF8Encoding(false));
      Console.WriteLine($"{label,-5} {nodes,6} {bytes,8} {bytes / 1024.0,6:F1}  {sw.ElapsedMilliseconds}  -> {path}");

      // RuntimeId stats
      double pct = stats.Total == 0 ? 0 : 100.0 * stats.WithRuntimeId / stats.Total;
      int without = stats.Total - stats.WithRuntimeId;
      var line1 = $"{safe}-{label}: total={stats.Total}, withRuntimeId={stats.WithRuntimeId} ({pct:F1}%), withoutRuntimeId={without}";
      var line2 = $"           empty={stats.EmptyRuntimeId}, exception={stats.ExceptionRuntimeId}";
      Console.WriteLine(line1);
      Console.WriteLine(line2);
      report.AppendLine(line1);
      report.AppendLine(line2);

      // AutomationId duplicate detection
      var dupResult = DetectAidDuplicates(root);
      if (dupResult.cases == 0)
      {
        var line3 = $"{safe}-{label}: AutomationId duplicates within same parent: duplicates: none";
        Console.WriteLine(line3);
        report.AppendLine(line3);
      }
      else
      {
        var line3 = $"{safe}-{label}: AutomationId duplicates within same parent: {dupResult.cases} cases";
        var line4 = $"           ex: {dupResult.example}";
        Console.WriteLine(line3);
        Console.WriteLine(line4);
        report.AppendLine(line3);
        report.AppendLine(line4);
      }
      report.AppendLine();
    }

    var reportPath = System.IO.Path.Combine(outDir, $"measure-{safe}-report.txt");
    System.IO.File.WriteAllText(reportPath, report.ToString(), new UTF8Encoding(false));
    Console.WriteLine($"report -> {reportPath}");
    return 0;
  }

  private static (int cases, string example) DetectAidDuplicates(SnapshotNode root)
  {
    int cases = 0;
    string? firstExample = null;
    void Walk(SnapshotNode n)
    {
      if (n.Children != null && n.Children.Count > 0)
      {
        var groups = n.Children
          .Where(c => !string.IsNullOrEmpty(c.AutomationId))
          .GroupBy(c => (c.Role ?? "", c.AutomationId ?? ""))
          .Where(g => g.Count() > 1)
          .ToList();
        if (groups.Count > 0)
        {
          cases += groups.Count;
          if (firstExample == null)
          {
            var parts = groups.Select(g => $"{g.Key.Item1}:{g.Key.Item2}\u00d7{g.Count()}");
            firstExample = $"parent={n.Ref} children=[{string.Join(", ", parts)}]";
          }
        }
        foreach (var c in n.Children) Walk(c);
      }
    }
    Walk(root);
    return (cases, firstExample ?? "");
  }

  private static int CountNodes(SnapshotNode n)
  {
    int c = 1;
    if (n.Children != null)
      foreach (var ch in n.Children) c += CountNodes(ch);
    return c;
  }

  private static string SanitizeFile(string s)
  {
    var sb = new StringBuilder();
    foreach (var ch in s)
    {
      if (char.IsLetterOrDigit(ch)) sb.Append(ch);
      else sb.Append('_');
    }
    return sb.ToString();
  }

  private static List<SnapshotNode> BuildFiltered(
      AutomationElement el, Func<AutomationElement, bool> include, ref int counter,
      int depth, int maxDepth, bool isRoot, MeasureStats stats)
  {
    var childNodes = new List<SnapshotNode>();
    if (depth < maxDepth)
    {
      AutomationElement[] children;
      try { children = el.FindAllChildren(); }
      catch { children = Array.Empty<AutomationElement>(); }
      foreach (var c in children)
      {
        try { childNodes.AddRange(BuildFiltered(c, include, ref counter, depth + 1, maxDepth, false, stats)); }
        catch { }
      }
    }

    bool keep = isRoot || SafeInclude(el, include);
    if (!keep) return childNodes;

    counter++;
    var node = new SnapshotNode
    {
      Ref = $"w{counter}",
      Role = SafeStr(() => el.ControlType.ToString()),
      Name = SafeStr(() => el.Properties.Name.ValueOrDefault),
      AutomationId = SafeStr(() => el.Properties.AutomationId.ValueOrDefault),
      ClassName = SafeStr(() => el.Properties.ClassName.ValueOrDefault),
    };

    // RuntimeId acquisition + stats
    stats.Total++;
    try
    {
      var rid = el.Properties.RuntimeId.Value;
      if (rid == null || rid.Length == 0)
      {
        stats.EmptyRuntimeId++;
      }
      else
      {
        node.RuntimeId = string.Join("-", rid);
        stats.WithRuntimeId++;
      }
    }
    catch
    {
      stats.ExceptionRuntimeId++;
    }

    try
    {
      var r = el.Properties.BoundingRectangle.ValueOrDefault;
      node.Rect = new[] { (int)r.X, (int)r.Y, (int)r.Width, (int)r.Height };
    }
    catch { }
    if (childNodes.Count > 0) node.Children = childNodes;
    return new List<SnapshotNode> { node };
  }

  private static bool SafeInclude(AutomationElement el, Func<AutomationElement, bool> include)
  {
    try { return include(el); } catch { return true; }
  }

  private static bool IsL1(AutomationElement el)
  {
    ControlType ct;
    try { ct = el.ControlType; } catch { return true; }
    return L1Allowed.Contains(ct);
  }

  private static bool IsL15(AutomationElement el)
  {
    ControlType ct;
    try { ct = el.ControlType; } catch { return true; }
    if (L1Allowed.Contains(ct)) return true;
    if (!L15ExtraStructural.Contains(ct)) return false;
    // 構造保持型は「Name か AutomationId のいずれかを持つ」場合のみ残す
    var name = SafeStr(() => el.Properties.Name.ValueOrDefault);
    var aid = SafeStr(() => el.Properties.AutomationId.ValueOrDefault);
    return !(string.IsNullOrEmpty(name) && string.IsNullOrEmpty(aid));
  }

  private static bool IsL2(AutomationElement el)
  {
    // 仕様の文字通り: L1 + 「無名 Pane/Group を除外」
    ControlType ct;
    try { ct = el.ControlType; } catch { return true; }
    if (!L1Allowed.Contains(ct))
    {
      // L1 で除外されてるが、Pane/Group の判定だけ別途実施するため評価
      return false;
    }
    if (ct == ControlType.Pane || ct == ControlType.Group)
    {
      var name = SafeStr(() => el.Properties.Name.ValueOrDefault);
      var aid = SafeStr(() => el.Properties.AutomationId.ValueOrDefault);
      if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(aid)) return false;
    }
    return true;
  }
}
