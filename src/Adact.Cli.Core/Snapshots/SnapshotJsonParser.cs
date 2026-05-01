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
    /// <param name="json">snapshot 生 JSON 文字列。</param>
    /// <returns>(メタ, ルート要素) タプル。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> が null。</exception>
    /// <exception cref="JsonException">JSON parse に失敗した場合。</exception>
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

    /// <summary>メタ情報 (<c>_meta</c>) をパースして <see cref="SnapshotMeta"/> を返す。不在フィールドは null/空とする。</summary>
    /// <param name="root">snapshot JSON のルート要素。</param>
    /// <returns>パース済みメタ情報。</returns>
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

    /// <summary>1 要素を再帰的に <see cref="SnapshotElement"/> に変換する。</summary>
    /// <param name="node">対象の JSON 要素 (1 tree node)。</param>
    /// <returns>生成された <see cref="SnapshotElement"/>。</returns>
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

    /// <summary>オブジェクト上の文字列プロパティを取得する。不在、型不一致、null はいずれも null を返す。</summary>
    /// <param name="node">対象の JSON 要素。</param>
    /// <param name="name">プロパティ名。</param>
    /// <returns>文字列プロパティの値、もしくは null。</returns>
    private static string? GetStringOrNull(JsonElement node, string name)
    {
        if (node.ValueKind != JsonValueKind.Object) return null;
        if (!node.TryGetProperty(name, out var v)) return null;
        return v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    }

    /// <summary>オブジェクト上のブールプロパティを取得する。不在/型不一致のときは <paramref name="defaultValue"/>。</summary>
    /// <param name="node">対象の JSON 要素。</param>
    /// <param name="name">プロパティ名。</param>
    /// <param name="defaultValue">不在・型不一致時のフォールバック値。</param>
    /// <returns>ブール値、または <paramref name="defaultValue"/>。</returns>
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
