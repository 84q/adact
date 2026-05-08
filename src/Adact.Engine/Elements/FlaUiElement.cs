using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Adact.Engine.Elements;

internal sealed class FlaUiElement : IElement
{
    private readonly AutomationElement _el;
    private readonly ILogger _logger;
    /// <summary>
    /// </summary>
    private IReadOnlyList<IElement>? _children;

    public FlaUiElement(AutomationElement el, ILogger? logger = null)
    {
        _el = el;
        _logger = logger ?? NullLogger<FlaUiElement>.Instance;
    }

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
    public bool IsSelected => Safe(() =>
    {
        var pattern = _el.Patterns.SelectionItem.PatternOrDefault;
        if (pattern is not null) return pattern.IsSelected.ValueOrDefault;
        return false;
    }, false);

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
        catch (Exception ex) { _logger.LogTrace(ex, "Failed to get Value pattern"); }
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
                }
                else
                {
                    raw = _el.FindAllChildren();
                }
                // Deduplicate by RuntimeId to avoid duplicate entries from FindAllDescendants flat list
                var seenRuntimeIds = new HashSet<string>();
                var list = new List<IElement>(raw.Length);
                foreach (var r in raw)
                {
                    if (r.Equals(_el))
                    {
                        continue;
                    }

                    var rid = Safe(() => r.Properties.RuntimeId.ValueOrDefault);
                    var key = rid is not null && rid.Length > 0
                        ? string.Join(",", rid)
                        : $"fallback:{r.GetHashCode()}";
                    if (seenRuntimeIds.Add(key))
                    {
                        list.Add(new FlaUiElement(r, _logger));
                    }
                }
                _children = list;
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Failed to enumerate children");
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
        catch (Exception ex) { _logger.LogTrace(ex, "Invoke pattern failed, falling back to Click"); }
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

        EnsureFocus();
        Keyboard.Type(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL, FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
        Keyboard.Type(FlaUI.Core.WindowsAPI.VirtualKeyShort.DELETE);
        Keyboard.Type(text);
    }

    private void EnsureFocus()
    {
        const int MaxRetries = 2;
        for (int i = 0; i <= MaxRetries; i++)
        {
            try
            {
                _el.Focus();
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Focus attempt failed (retry {Attempt})", i);
            }

            Wait.UntilInputIsProcessed();

            try
            {
                if (_el.Properties.HasKeyboardFocus.ValueOrDefault)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "HasKeyboardFocus check failed (retry {Attempt})", i);
            }
        }

        throw new InvalidOperationException("Failed to set keyboard focus to the target element.");
    }

    /// <inheritdoc />
    public void Focus()
    {
        try { _el.Focus(); } catch (Exception ex) { _logger.LogTrace(ex, "Focus attempt failed"); }
    }

    /// <inheritdoc />
    public void ClearChildrenCache()
    {
        _children = null;
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrEmpty(s) ? null : s;

    /// <summary>
    /// </summary>
    private T Safe<T>(Func<T> f, T fallback)
    {
        try { return f(); } catch (Exception ex) { _logger.LogTrace(ex, "Safe property access failed, using fallback"); return fallback; }
    }

    /// <summary>
    /// </summary>
    private T? Safe<T>(Func<T?> f) where T : class
    {
        try { return f(); } catch (Exception ex) { _logger.LogTrace(ex, "Safe property access failed, returning null"); return null; }
    }
}
