using System.Text.Json;
using System.Text.Json.Nodes;

using Adact.Engine.Elements;
using Adact.Engine.Filters;

namespace Adact.Engine.Snapshot;

/// <summary>
/// IElement のツリーを Snapshot JSON へ変換するビルダ。
/// FilterStrategy.Decide の 3 値 (Include/Flatten/Exclude) を尊重し、
/// モーダル兄弟ノード ( <c>isModalDialog: true</c> ) を root window の追加子として挿入する。
/// </summary>
public sealed class SnapshotBuilder
{
    /// <summary>SnapshotOptions.MaxDepth が 0 以下や未指定相当の時に使う既定の再帰深度上限。</summary>
    private const int DefaultMaxDepth = 64;

    private readonly RefRegistry _registry;

    public SnapshotBuilder(RefRegistry registry)
    {
        _registry = registry;
    }

    public SnapshotBuildResult Build(SnapshotBuildInput input)
    {
        _registry.BeginSnapshot();

        // SnapshotOptions.MaxDepth を実際の再帰深度ガードとして用いる。0 以下の場合は既定値にフォールバック。
        var maxDepth = input.Options.MaxDepth > 0 ? input.Options.MaxDepth : DefaultMaxDepth;

        // DFS 出現順カウンタ。RuntimeId 取得不可な要素の StableKey フォールバックに用いる。
        var positionalIndex = 0;

        // tree のルートは常にメインウィンドウを Include 強制 (filter の判定に依らない)
        var rootNode = BuildIncludeNode(input.RootWindow, input.Filter, depth: 0, maxDepth, isModalDialog: false, ref positionalIndex);

        // モーダル兄弟は root window の追加子として isModalDialog: true 付きで挿入する。
        // (Architecture §6.5 の「Snapshot tree に兄弟ノードとして」を、単一 tree 構造に展開した実装)
        var modalSummaries = new JsonArray();
        if (input.ModalSiblings.Count > 0)
        {
            var children = (rootNode["children"] as JsonArray) ?? new JsonArray();
            foreach (var modal in input.ModalSiblings)
            {
                var modalNode = BuildIncludeNode(modal, input.Filter, depth: 0, maxDepth, isModalDialog: true, ref positionalIndex);
                children.Add(modalNode);
                modalSummaries.Add(new JsonObject
                {
                    ["ref"] = modalNode["ref"]?.GetValue<string>(),
                    ["title"] = modal.Name,
                });
            }
            rootNode["children"] = children;
        }

        var meta = new JsonObject
        {
            ["filter"] = input.Filter.Name,
            ["options"] = new JsonObject { ["maxDepth"] = input.Options.MaxDepth },
            ["generatedAt"] = input.GeneratedAt.UtcDateTime.ToString("O"),
            ["sessionId"] = $"s{_registry.SessionId}",
            ["windowTitle"] = input.WindowTitle,
            ["processName"] = input.ProcessName,
            ["processId"] = input.ProcessId,
            ["modalDialog"] = modalSummaries.Count == 0 ? null : modalSummaries,
        };

        var root = new JsonObject
        {
            ["_meta"] = meta,
            ["tree"] = rootNode,
        };

        var json = root.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = false,
        });

        return new SnapshotBuildResult(json, $"s{_registry.SessionId}");
    }

    /// <summary>Include 確定の要素から JSON ノードを構築する (root / 通常子 共通)。</summary>
    private JsonObject BuildIncludeNode(
        IElement el, IFilterStrategy filter, int depth, int maxDepth, bool isModalDialog, ref int positionalIndex)
    {
        var refId = _registry.Register(el, positionalIndex);
        positionalIndex++;
        var node = new JsonObject
        {
            ["ref"] = refId,
            ["role"] = el.ControlType,
        };

        var props = filter.ExtractProperties(el);
        foreach (var kv in props)
        {
            if (kv.Key is "ref" or "role" or "children" or "isModalDialog") continue;
            if (kv.Value is null) continue;
            node[kv.Key] = ToJsonNode(kv.Value);
        }

        if (isModalDialog) node["isModalDialog"] = true;

        var childNodes = new JsonArray();
        if (depth < maxDepth)
        {
            foreach (var child in el.Children)
            {
                BuildChildInto(child, filter, depth + 1, maxDepth, childNodes, ref positionalIndex);
            }
        }
        if (childNodes.Count > 0) node["children"] = childNodes;
        return node;
    }

    /// <summary>子要素を再帰処理し、Include なら単独ノード、Flatten なら子群を、親 children に追加する。</summary>
    private void BuildChildInto(
        IElement child, IFilterStrategy filter, int depth, int maxDepth, JsonArray parentChildren, ref int positionalIndex)
    {
        var decision = filter.Decide(child, new FilterContext(depth));

        switch (decision)
        {
            case NodeDecision.Exclude:
                return;
            case NodeDecision.Include:
                parentChildren.Add(BuildIncludeNode(child, filter, depth, maxDepth, isModalDialog: false, ref positionalIndex));
                return;
            case NodeDecision.Flatten:
                if (depth < maxDepth)
                {
                    foreach (var grand in child.Children)
                    {
                        BuildChildInto(grand, filter, depth + 1, maxDepth, parentChildren, ref positionalIndex);
                    }
                }
                return;
        }
    }

    private static JsonNode ToJsonNode(object value)
    {
        return value switch
        {
            JsonNode n => n,
            string s => JsonValue.Create(s)!,
            int i => JsonValue.Create(i)!,
            long l => JsonValue.Create(l)!,
            bool b => JsonValue.Create(b)!,
            int[] ints => new JsonArray(ints.Select(x => (JsonNode?)JsonValue.Create(x)).ToArray()),
            _ => JsonSerializer.SerializeToNode(value)!,
        };
    }
}
