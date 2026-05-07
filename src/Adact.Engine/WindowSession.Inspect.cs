using System.Linq;

using Adact.Engine.Elements;
using Adact.Engine.Exceptions;

using FlaUI.Core.AutomationElements;

namespace Adact.Engine;

public sealed partial class WindowSession
{
    /// <summary>
    /// 指定 Element Ref が指す UIA 要素の詳細プロパティを取得する (設計 022 §8)。auto-snapshot は発火しない。
    /// 子要素サマリは含めない (snapshot に任せる)。
    /// </summary>
    /// <param name="refId">対象 Element Ref ID。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>UIA プロパティと対応 Pattern の状態を含む <see cref="InspectResult"/>。</returns>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="RefNotFoundException"><paramref name="refId"/> が現セッションで解決できない場合。</exception>
    public Task<InspectResult> InspectAsync(string refId, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(refId);
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();
            var el = _registry.Resolve(refId);
            var inner = (el as FlaUiElement)?.Inner;

            var patterns = inner is not null ? CollectPatterns(inner) : new Dictionary<string, IReadOnlyDictionary<string, object?>>();

            // 安定セレクタ候補の算出
            SelectorSuggestion? selector = null;
            var allElements = _registry.EnumerateCurrent().Select(x => x.Element).ToList();
            if (allElements.Count > 0)
            {
                var ancestors = BuildAncestorChain(inner);
                selector = SelectorSuggester.Suggest(el, allElements, ancestors);
            }

            var result = new InspectResult(
                Ref: refId,
                Name: el.Name,
                ControlType: el.ControlType,
                AutomationId: el.AutomationId,
                ClassName: el.ClassName,
                HelpText: el.HelpText,
                Value: el.Value,
                BoundingRect: el.BoundingRectangle,
                IsEnabled: el.IsEnabled,
                IsOffscreen: el.IsOffscreen,
                IsKeyboardFocusable: el.IsKeyboardFocusable,
                HasKeyboardFocus: el.HasKeyboardFocus,
                Patterns: patterns,
                Selector: selector);
            return Task.FromResult(result);
        }, ct);
    }

    /// <summary>
    /// FlaUI の Parent を辿り、対象ウィンドウルートまでの祖先チェーンを構築する。
    /// </summary>
    private IReadOnlyList<AncestorInfo> BuildAncestorChain(AutomationElement? inner)
    {
        if (inner is null)
            return [];

        var ancestors = new List<AncestorInfo>();
        var windowHandle = _window.Properties.NativeWindowHandle.ValueOrDefault;

        try
        {
            var current = inner.Parent;
            while (current is not null)
            {
                // ウィンドウルートに到達したら停止
                if (current.Properties.NativeWindowHandle.ValueOrDefault == windowHandle)
                    break;

                var automationId = NullIfEmpty(current.Properties.AutomationId.ValueOrDefault);
                var name = NullIfEmpty(current.Properties.Name.ValueOrDefault);
                var controlType = current.Properties.ControlType.ValueOrDefault.ToString();
                ancestors.Add(new AncestorInfo(automationId, name, controlType));

                current = current.Parent;
            }
        }
        catch
        {
            // best effort: UIA Parent 呼び出し失敗時は途中までの祖先を返す
        }

        return ancestors;
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

    /// <summary>
    /// 対応する UIA Pattern を判定し、各 Pattern の状態を辞書で返す。
    /// 設計 022 §8 で列挙された Toggle / SelectionItem / ExpandCollapse / RangeValue / Window の 5 種を対象とする。
    /// </summary>
    /// <param name="el">対象 FlaUI 要素。</param>
    /// <returns>Pattern 名 → 状態辞書 のマップ。Pattern を持たない要素では空辞書。</returns>
    private static Dictionary<string, IReadOnlyDictionary<string, object?>> CollectPatterns(AutomationElement el)
    {
        var result = new Dictionary<string, IReadOnlyDictionary<string, object?>>(StringComparer.Ordinal);

        TryAdd(result, "Toggle", () =>
        {
            var p = el.Patterns.Toggle.PatternOrDefault;
            if (p is null) return null;
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["ToggleState"] = p.ToggleState.ValueOrDefault.ToString(),
            };
        });

        TryAdd(result, "SelectionItem", () =>
        {
            var p = el.Patterns.SelectionItem.PatternOrDefault;
            if (p is null) return null;
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["IsSelected"] = p.IsSelected.ValueOrDefault,
            };
        });

        TryAdd(result, "ExpandCollapse", () =>
        {
            var p = el.Patterns.ExpandCollapse.PatternOrDefault;
            if (p is null) return null;
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["ExpandCollapseState"] = p.ExpandCollapseState.ValueOrDefault.ToString(),
            };
        });

        TryAdd(result, "RangeValue", () =>
        {
            var p = el.Patterns.RangeValue.PatternOrDefault;
            if (p is null) return null;
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["Min"] = p.Minimum.ValueOrDefault,
                ["Max"] = p.Maximum.ValueOrDefault,
                ["Value"] = p.Value.ValueOrDefault,
                ["SmallChange"] = p.SmallChange.ValueOrDefault,
                ["LargeChange"] = p.LargeChange.ValueOrDefault,
                ["IsReadOnly"] = p.IsReadOnly.ValueOrDefault,
            };
        });

        TryAdd(result, "Value", () =>
        {
            var p = el.Patterns.Value.PatternOrDefault;
            if (p is null) return null;
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["IsReadOnly"] = p.IsReadOnly.ValueOrDefault,
            };
        });

        TryAdd(result, "Selection", () =>
        {
            var p = el.Patterns.Selection.PatternOrDefault;
            if (p is null) return null;
            var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
            dict["CanSelectMultiple"] = p.CanSelectMultiple.ValueOrDefault;
            dict["IsSelectionRequired"] = p.IsSelectionRequired.ValueOrDefault;
            try
            {
                var selected = p.Selection.ValueOrDefault;
                if (selected is { Length: > 0 })
                    dict["SelectedItems"] = selected.Select(e => e.Name ?? "").ToArray();
            }
            catch { /* best effort */ }
            return dict;
        });

        TryAdd(result, "Grid", () =>
        {
            var p = el.Patterns.Grid.PatternOrDefault;
            if (p is null) return null;
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["RowCount"] = p.RowCount.ValueOrDefault,
                ["ColumnCount"] = p.ColumnCount.ValueOrDefault,
            };
        });

        TryAdd(result, "GridItem", () =>
        {
            var p = el.Patterns.GridItem.PatternOrDefault;
            if (p is null) return null;
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["Row"] = p.Row.ValueOrDefault,
                ["Column"] = p.Column.ValueOrDefault,
                ["RowSpan"] = p.RowSpan.ValueOrDefault,
                ["ColumnSpan"] = p.ColumnSpan.ValueOrDefault,
            };
        });

        TryAdd(result, "Table", () =>
        {
            var p = el.Patterns.Table.PatternOrDefault;
            if (p is null) return null;
            var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
            dict["RowOrColumnMajor"] = p.RowOrColumnMajor.ValueOrDefault.ToString();
            try
            {
                var colHeaders = p.ColumnHeaders.ValueOrDefault;
                if (colHeaders is { Length: > 0 })
                    dict["ColumnHeaders"] = colHeaders.Select(e => e.Name ?? "").ToArray();
            }
            catch { /* best effort */ }
            try
            {
                var rowHeaders = p.RowHeaders.ValueOrDefault;
                if (rowHeaders is { Length: > 0 })
                    dict["RowHeaders"] = rowHeaders.Select(e => e.Name ?? "").ToArray();
            }
            catch { /* best effort */ }
            return dict;
        });

        TryAdd(result, "TableItem", () =>
        {
            var p = el.Patterns.TableItem.PatternOrDefault;
            if (p is null) return null;
            var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
            try
            {
                var colHeaders = p.ColumnHeaderItems.ValueOrDefault;
                if (colHeaders is { Length: > 0 })
                    dict["ColumnHeaders"] = colHeaders.Select(e => e.Name ?? "").ToArray();
            }
            catch { /* best effort */ }
            try
            {
                var rowHeaders = p.RowHeaderItems.ValueOrDefault;
                if (rowHeaders is { Length: > 0 })
                    dict["RowHeaders"] = rowHeaders.Select(e => e.Name ?? "").ToArray();
            }
            catch { /* best effort */ }
            return dict.Count > 0 ? dict : null;
        });

        TryAdd(result, "Scroll", () =>
        {
            var p = el.Patterns.Scroll.PatternOrDefault;
            if (p is null) return null;
            var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
            dict["HCanScroll"] = p.HorizontallyScrollable.ValueOrDefault;
            dict["VCanScroll"] = p.VerticallyScrollable.ValueOrDefault;
            dict["HPercent"] = p.HorizontalScrollPercent.ValueOrDefault;
            dict["VPercent"] = p.VerticalScrollPercent.ValueOrDefault;
            dict["HViewSize"] = p.HorizontalViewSize.ValueOrDefault;
            dict["VViewSize"] = p.VerticalViewSize.ValueOrDefault;
            return dict;
        });

        TryAdd(result, "Text", () =>
        {
            var p = el.Patterns.Text.PatternOrDefault;
            if (p is null) return null;
            var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
            try
            {
                var text = p.DocumentRange.GetText(-1);
                if (text is not null)
                {
                    dict["Length"] = text.Length;
                    dict["Preview"] = text.Length > 30 ? text[..30] + "..." : text;
                }
            }
            catch { /* best effort */ }
            return dict.Count > 0 ? dict : null;
        });

        TryAdd(result, "Window", () =>
        {
            var p = el.Patterns.Window.PatternOrDefault;
            if (p is null) return null;
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["VisualState"] = p.WindowVisualState.ValueOrDefault.ToString(),
                ["InteractionState"] = p.WindowInteractionState.ValueOrDefault.ToString(),
            };
        });

        return result;
    }

    /// <summary>
    /// Pattern 取得 (例外を握り潰し) して、戻り値が non-null なら <paramref name="map"/> に登録する。
    /// </summary>
    /// <param name="map">登録先マップ。</param>
    /// <param name="key">Pattern 名 (例: <c>"Toggle"</c>)。</param>
    /// <param name="getter">Pattern 状態を取得するデリゲート。Pattern 不在時は <c>null</c> を返す。</param>
    private static void TryAdd(
        Dictionary<string, IReadOnlyDictionary<string, object?>> map,
        string key,
        Func<IReadOnlyDictionary<string, object?>?> getter)
    {
        try
        {
            var v = getter();
            if (v is not null) map[key] = v;
        }
        catch
        {
            // best effort: pattern 取得失敗は単に「未対応」として扱う
        }
    }
}
