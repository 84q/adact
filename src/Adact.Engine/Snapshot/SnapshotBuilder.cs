using System.Text.Json;
using System.Text.Json.Nodes;

using Adact.Engine.Elements;

namespace Adact.Engine.Snapshot;

/// <summary>
/// Builds JSON snapshots from UIA elements.
/// </summary>
public sealed class SnapshotBuilder
{
    private const int DefaultMaxDepth = 64;

    private readonly RefRegistry _registry;
    private readonly HashSet<string> _emittedRefs = new();
    private HashSet<string>? _modalRuntimeIds;

    /// <summary>
    /// Creates a new snapshot builder.
    /// </summary>
    public SnapshotBuilder(RefRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Builds a snapshot from the supplied input.
    /// </summary>
    public SnapshotBuildResult Build(SnapshotBuildInput input)
    {
        _registry.BeginSnapshot();
        _emittedRefs.Clear();

        _modalRuntimeIds = null;
        if (input.ModalSiblings.Count > 0)
        {
            var rids = new HashSet<string>();
            foreach (var modal in input.ModalSiblings)
            {
                var rid = modal.RuntimeId;
                if (rid is { Count: > 0 })
                    rids.Add(string.Join("-", rid));
            }
            if (rids.Count > 0)
                _modalRuntimeIds = rids;
        }

        var maxDepth = input.Options.MaxDepth > 0 ? input.Options.MaxDepth : DefaultMaxDepth;

        var positionalIndex = 0;

        var rootNode = BuildNode(input.RootWindow, depth: 0, maxDepth, isModalDialog: false, isPopup: false, ref positionalIndex);

        var children = (rootNode?["children"] as JsonArray) ?? new JsonArray();

        var modalSummaries = new JsonArray();
        if (input.ModalSiblings.Count > 0)
        {
            foreach (var modal in input.ModalSiblings)
            {
                var modalNode = BuildNode(modal, depth: 0, maxDepth, isModalDialog: true, isPopup: false, ref positionalIndex);
                if (modalNode is not null)
                {
                    children.Add(modalNode);
                    modalSummaries.Add(new JsonObject
                    {
                        ["ref"] = modalNode["ref"]?.GetValue<string>(),
                        ["title"] = modal.Name,
                    });
                }
                else
                {
                    var refId = _registry.Register(modal, positionalIndex);
                    positionalIndex++;
                    modalSummaries.Add(new JsonObject
                    {
                        ["ref"] = refId,
                        ["title"] = modal.Name,
                    });
                }
            }
        }

        if (input.PopupSiblings.Count > 0)
        {
            foreach (var popup in input.PopupSiblings)
            {
                var popupNode = BuildNode(popup, depth: 0, maxDepth, isModalDialog: false, isPopup: true, ref positionalIndex);
                if (popupNode is not null)
                {
                    children.Add(popupNode);
                }
            }
        }

        if (rootNode is not null && children.Count > 0)
        {
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

    private JsonObject? BuildNode(
        IElement el, int depth, int maxDepth, bool isModalDialog, bool isPopup, ref int positionalIndex)
    {
        var refId = _registry.Register(el, positionalIndex);
        positionalIndex++;

        // Skip if this ref was already emitted in the current snapshot
        // (can happen with UWP FindAllDescendants flat lists where the same element
        // appears both as a direct child and nested inside another branch)
        if (!_emittedRefs.Add(refId))
            return null;

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
        if (el.IsSelected) node["isSelected"] = true;

        if (!isModalDialog && _modalRuntimeIds is not null)
        {
            var rid = el.RuntimeId;
            if (rid is { Count: > 0 } && _modalRuntimeIds.Contains(string.Join("-", rid)))
            {
                isModalDialog = true;
            }
        }
        if (isModalDialog) node["isModalDialog"] = true;
        if (isPopup) node["isPopup"] = true;

        var childNodes = new JsonArray();
        if (depth < maxDepth)
        {
            foreach (var child in el.Children)
            {
                var childNode = BuildNode(child, depth + 1, maxDepth, isModalDialog: false, isPopup: false, ref positionalIndex);
                if (childNode is not null)
                    childNodes.Add(childNode);
            }
        }
        if (childNodes.Count > 0) node["children"] = childNodes;
        return node;
    }
}
