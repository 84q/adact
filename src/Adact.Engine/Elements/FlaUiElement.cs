using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;

namespace Adact.Engine.Elements;

/// <summary>FlaUI の <see cref="AutomationElement"/> を <see cref="IElement"/> でラップする production 実装。</summary>
internal sealed class FlaUiElement : IElement
{
    /// <summary>ラップしている FlaUI の UIA 要素。</summary>
    private readonly AutomationElement _el;
    /// <summary>
    /// <see cref="Children"/> の遅延キャッシュ。初回アクセス時に FlaUI で子要素を取得し、
    /// 2 回目以降は同じ list を返す (UIA 呼び出し量を抑えるため)。
    /// </summary>
    private IReadOnlyList<IElement>? _children;

    /// <summary>FlaUI の <see cref="AutomationElement"/> をラップする。</summary>
    /// <param name="el">ラップ対象の UIA 要素。</param>
    public FlaUiElement(AutomationElement el)
    {
        _el = el;
    }

    /// <summary>ラップしている FlaUI の <see cref="AutomationElement"/>。Engine 内部用 (主にテスト/診断目的)。</summary>
    public AutomationElement Inner => _el;

    /// <inheritdoc />
    public string? Name => Safe(() => NullIfEmpty(_el.Properties.Name.ValueOrDefault));

    /// <inheritdoc />
    public string? AutomationId => Safe(() => NullIfEmpty(_el.Properties.AutomationId.ValueOrDefault));

    /// <inheritdoc />
    public string ControlType => Safe(() => _el.ControlType.ToString()) ?? "Unknown";

    /// <inheritdoc />
    public string? ClassName => Safe(() => NullIfEmpty(_el.Properties.ClassName.ValueOrDefault));

    /// <inheritdoc />
    public bool IsEnabled => Safe(() => _el.Properties.IsEnabled.ValueOrDefault, true);

    /// <inheritdoc />
    public bool IsOffscreen => Safe(() => _el.Properties.IsOffscreen.ValueOrDefault, false);

    /// <inheritdoc />
    public string? Value => Safe(() =>
    {
        try
        {
            var pattern = _el.Patterns.Value.PatternOrDefault;
            if (pattern is not null) return NullIfEmpty(pattern.Value.ValueOrDefault);
        }
        catch { }
        return null;
    });

    /// <inheritdoc />
    public string? HelpText => Safe(() => NullIfEmpty(_el.Properties.HelpText.ValueOrDefault));

    /// <inheritdoc />
    public Rect BoundingRectangle => Safe(() =>
    {
        var r = _el.Properties.BoundingRectangle.ValueOrDefault;
        return new Rect((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
    }, default);

    /// <inheritdoc />
    public bool IsKeyboardFocusable => Safe(() => _el.Properties.IsKeyboardFocusable.ValueOrDefault, false);

    /// <inheritdoc />
    public bool HasKeyboardFocus => Safe(() => _el.Properties.HasKeyboardFocus.ValueOrDefault, false);

    /// <inheritdoc />
    public IReadOnlyList<int>? RuntimeId => Safe(() =>
    {
        if (_el.Properties.RuntimeId.TryGetValue(out var rid) && rid is not null && rid.Length > 0)
            return (IReadOnlyList<int>)rid;
        return null;
    });

    /// <inheritdoc />
    public IReadOnlyList<IElement> Children
    {
        get
        {
            if (_children is not null) return _children;
            try
            {
                AutomationElement[] raw;
                // UWP workaround: CoreWindow hides deep content from FindAllChildren
                if (_el.Properties.ClassName.ValueOrDefault == "Windows.UI.Core.CoreWindow")
                {
                    raw = _el.FindAllDescendants();
                    // Exclude self if present
                    raw = raw.Where(r => !r.Equals(_el)).ToArray();
                }
                else
                {
                    raw = _el.FindAllChildren();
                }
                // Deduplicate by RuntimeId to avoid duplicate entries from FindAllDescendants flat list
                var seenRuntimeIds = new HashSet<string>();
                var unique = new List<AutomationElement>(raw.Length);
                foreach (var r in raw)
                {
                    var rid = Safe(() => r.Properties.RuntimeId.ValueOrDefault);
                    var key = rid is not null && rid.Length > 0
                        ? string.Join(",", rid)
                        : $"fallback:{r.GetHashCode()}";
                    if (seenRuntimeIds.Add(key))
                    {
                        unique.Add(r);
                    }
                }
                raw = unique.ToArray();
                var list = new List<IElement>(raw.Length);
                foreach (var c in raw) list.Add(new FlaUiElement(c));
                _children = list;
            }
            catch
            {
                _children = Array.Empty<IElement>();
            }
            return _children;
        }
    }

    /// <inheritdoc />
    public void Click()
    {
        try
        {
            var btn = _el.AsButton();
            var invoke = btn.Patterns.Invoke.PatternOrDefault;
            if (invoke is not null)
            {
                invoke.Invoke();
                return;
            }
        }
        catch { }
        _el.Click();
    }

    /// <inheritdoc />
    public void Fill(string text)
    {
        var valuePattern = _el.Patterns.Value.PatternOrDefault;
        if (valuePattern is not null && !valuePattern.IsReadOnly.ValueOrDefault)
        {
            valuePattern.SetValue(text);
            return;
        }
        // Fallback: focus + 全選択 + 入力
        _el.Focus();
        Keyboard.Type(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL, FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
        Keyboard.Type(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);
        Keyboard.Type(text);
    }

    /// <inheritdoc />
    public void Focus()
    {
        try { _el.Focus(); } catch { /* best effort */ }
    }

    /// <inheritdoc />
    public void ClearChildrenCache()
    {
        _children = null;
    }

    /// <summary>空文字列を <c>null</c> に正規化する。</summary>
    /// <param name="s">入力文字列。</param>
    /// <returns><paramref name="s"/> が <c>null</c> または空文字列なら <c>null</c>、それ以外はそのまま返す。</returns>
    private static string? NullIfEmpty(string? s) => string.IsNullOrEmpty(s) ? null : s;

    /// <summary>
    /// 例外を握り潰して <paramref name="f"/> を実行するヘルパ。値型・非 null プリミティブや
    /// fallback 値を明示したいケースで使う。例外時は <paramref name="fallback"/> を返す。
    /// </summary>
    /// <typeparam name="T">返値の型。</typeparam>
    /// <param name="f">実行するデリゲート。</param>
    /// <param name="fallback">例外時に返す値。</param>
    private static T Safe<T>(Func<T> f, T fallback)
    {
        try { return f(); } catch { return fallback; }
    }

    /// <summary>
    /// 例外を握り潰して <paramref name="f"/> を実行するヘルパの参照型専用オーバーロード。
    /// fallback として常に <c>null</c> を返すため、Nullable string 等を返す UIA プロパティアクセサで使う。
    /// </summary>
    /// <typeparam name="T">参照型の返値型。</typeparam>
    /// <param name="f">実行するデリゲート。</param>
    /// <returns><paramref name="f"/> の戻り値。例外発生時は <c>null</c>。</returns>
    private static T? Safe<T>(Func<T?> f) where T : class
    {
        try { return f(); } catch { return null; }
    }
}
