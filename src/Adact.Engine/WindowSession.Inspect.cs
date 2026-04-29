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
                Patterns: patterns);
            return Task.FromResult(result);
        }, ct);
    }

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
            };
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
