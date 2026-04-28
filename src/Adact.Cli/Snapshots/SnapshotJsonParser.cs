using System.Text.Json;

namespace Adact.Cli.Snapshots;

/// <summary>
/// MCP server が返す raw snapshot JSON (Engine.SnapshotBuilder の出力形式) を
/// <see cref="SnapshotElement"/> ツリーと <see cref="SnapshotMeta"/> に変換する。
/// 設計 016 §2 (CLI 側に snapshot 処理層を新設)。
/// </summary>
internal static class SnapshotJsonParser
{
  /// <summary>JSON 文字列からメタ情報とルート要素を抽出する。</summary>
  public static (SnapshotMeta Meta, SnapshotElement Root) Parse(string json)
  {
    ArgumentNullException.ThrowIfNull(json);
    using var doc = JsonDocument.Parse(json);
    var rootElement = doc.RootElement;

    var meta = ParseMeta(rootElement);
    var tree = rootElement.GetProperty("tree");
    var root = ParseElement(tree);
    return (meta, root);
  }

  private static SnapshotMeta ParseMeta(JsonElement root)
  {
    var sessionId = "";
    string? processName = null;
    int? processId = null;
    var generatedAt = "";

    if (root.TryGetProperty("_meta", out var meta) && meta.ValueKind == JsonValueKind.Object)
    {
      if (meta.TryGetProperty("sessionId", out var s) && s.ValueKind == JsonValueKind.String)
        sessionId = s.GetString() ?? "";
      if (meta.TryGetProperty("processName", out var pn) && pn.ValueKind == JsonValueKind.String)
        processName = pn.GetString();
      if (meta.TryGetProperty("processId", out var pid) && pid.ValueKind == JsonValueKind.Number
        && pid.TryGetInt32(out var pidVal))
        processId = pidVal;
      if (meta.TryGetProperty("generatedAt", out var g) && g.ValueKind == JsonValueKind.String)
        generatedAt = g.GetString() ?? "";
    }

    return new SnapshotMeta(sessionId, processName, processId, generatedAt);
  }

  private static SnapshotElement ParseElement(JsonElement node)
  {
    var role = GetStringOrNull(node, "role") ?? "";
    var name = GetStringOrNull(node, "name");
    var automationId = GetStringOrNull(node, "automationId");
    var value = GetStringOrNull(node, "value");
    var isEnabled = GetBoolOrDefault(node, "isEnabled", defaultValue: true);
    var isOffscreen = GetBoolOrDefault(node, "isOffscreen", defaultValue: false);
    var hasKeyboardFocus = GetBoolOrDefault(node, "hasKeyboardFocus", defaultValue: false);
    var isModalDialog = GetBoolOrDefault(node, "isModalDialog", defaultValue: false);
    var refId = GetStringOrNull(node, "ref") ?? "";

    var children = new List<SnapshotElement>();
    if (node.TryGetProperty("children", out var childArr)
      && childArr.ValueKind == JsonValueKind.Array)
    {
      foreach (var c in childArr.EnumerateArray())
      {
        children.Add(ParseElement(c));
      }
    }

    return new SnapshotElement(
      Role: role,
      Name: name,
      AutomationId: automationId,
      Value: value,
      IsEnabled: isEnabled,
      IsOffscreen: isOffscreen,
      HasKeyboardFocus: hasKeyboardFocus,
      IsModalDialog: isModalDialog,
      Ref: refId,
      Children: children);
  }

  private static string? GetStringOrNull(JsonElement node, string name)
  {
    if (node.ValueKind != JsonValueKind.Object) return null;
    if (!node.TryGetProperty(name, out var v)) return null;
    return v.ValueKind == JsonValueKind.String ? v.GetString() : null;
  }

  private static bool GetBoolOrDefault(JsonElement node, string name, bool defaultValue)
  {
    if (node.ValueKind != JsonValueKind.Object) return defaultValue;
    if (!node.TryGetProperty(name, out var v)) return defaultValue;
    return v.ValueKind switch
    {
      JsonValueKind.True => true,
      JsonValueKind.False => false,
      _ => defaultValue,
    };
  }
}
