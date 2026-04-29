using System.Text.Json;
using System.Text.Json.Nodes;

using Adact.Engine.Elements;

namespace Adact.Engine.Snapshot;

/// <summary>
/// IElement のツリーを raw 全要素・全フィールドの JSON に変換するビルダ。
///
/// Phase 7 でフィルタ (operable/raw 切替・フィールド選別) は CLI 側へ移譲したため、
/// ここでは原則すべての子要素を含め、UIA から取得できるプロパティを欠落なく出力する。
/// モーダル兄弟ノード ( <c>isModalDialog: true</c> ) は引き続き root window の追加子として
/// 挿入する (Architecture §6.5)。
/// </summary>
public sealed class SnapshotBuilder
{
    /// <summary>SnapshotOptions.MaxDepth が 0 以下や未指定相当の時に使う既定の再帰深度上限。</summary>
    private const int DefaultMaxDepth = 64;

    /// <summary>snapshot 中の Ref ID 採番に使うセッション固有レジストリ。</summary>
    private readonly RefRegistry _registry;

    /// <summary>新しいビルダーを <see cref="RefRegistry"/> 紐付けで初期化する。</summary>
    /// <param name="registry">snapshot 中の Ref ID 採番に使用するセッション固有レジストリ。</param>
    public SnapshotBuilder(RefRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>UIA ツリーを走査し、JSON snapshot を構築する。</summary>
    /// <param name="input">root ウィンドウ・モーダル兄弟・オプションおよびメタ情報。</param>
    /// <returns>構築された snapshot JSON とセッション ID 文字列。</returns>
    public SnapshotBuildResult Build(SnapshotBuildInput input)
    {
        _registry.BeginSnapshot();

        var maxDepth = input.Options.MaxDepth > 0 ? input.Options.MaxDepth : DefaultMaxDepth;

        // DFS 出現順カウンタ。RuntimeId 取得不可な要素の StableKey フォールバックに用いる。
        var positionalIndex = 0;

        var rootNode = BuildNode(input.RootWindow, depth: 0, maxDepth, isModalDialog: false, ref positionalIndex);

        var modalSummaries = new JsonArray();
        if (input.ModalSiblings.Count > 0)
        {
            var children = (rootNode["children"] as JsonArray) ?? new JsonArray();
            foreach (var modal in input.ModalSiblings)
            {
                var modalNode = BuildNode(modal, depth: 0, maxDepth, isModalDialog: true, ref positionalIndex);
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

    /// <summary>raw 全フィールドを JSON ノードとして出力する (フィルタなし、すべての子を再帰)。</summary>
    private JsonObject BuildNode(
        IElement el, int depth, int maxDepth, bool isModalDialog, ref int positionalIndex)
    {
        var refId = _registry.Register(el, positionalIndex);
        positionalIndex++;

        var node = new JsonObject
        {
            ["ref"] = refId,
            ["role"] = el.ControlType,
        };

        if (!string.IsNullOrEmpty(el.Name)) node["name"] = el.Name;
        if (!string.IsNullOrEmpty(el.AutomationId)) node["automationId"] = el.AutomationId;
        if (!string.IsNullOrEmpty(el.ClassName)) node["className"] = el.ClassName;
        node["isEnabled"] = el.IsEnabled;
        node["isOffscreen"] = el.IsOffscreen;
        if (!string.IsNullOrEmpty(el.Value)) node["value"] = el.Value;
        if (!string.IsNullOrEmpty(el.HelpText)) node["helpText"] = el.HelpText;
        var r = el.BoundingRectangle;
        node["boundingRect"] = new JsonArray(
            JsonValue.Create(r.X), JsonValue.Create(r.Y),
            JsonValue.Create(r.Width), JsonValue.Create(r.Height));
        node["isKeyboardFocusable"] = el.IsKeyboardFocusable;
        node["hasKeyboardFocus"] = el.HasKeyboardFocus;
        if (isModalDialog) node["isModalDialog"] = true;

        var childNodes = new JsonArray();
        if (depth < maxDepth)
        {
            foreach (var child in el.Children)
            {
                childNodes.Add(BuildNode(child, depth + 1, maxDepth, isModalDialog: false, ref positionalIndex));
            }
        }
        if (childNodes.Count > 0) node["children"] = childNodes;
        return node;
    }
}
