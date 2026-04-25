using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;

namespace Adact.Engine.Elements;

/// <summary>FlaUI の <see cref="AutomationElement"/> を <see cref="IElement"/> でラップする production 実装。</summary>
internal sealed class FlaUiElement : IElement
{
  private readonly AutomationElement _el;
  private IReadOnlyList<IElement>? _children;

  public FlaUiElement(AutomationElement el)
  {
    _el = el;
  }

  public AutomationElement Inner => _el;

  public string? Name => Safe(() => NullIfEmpty(_el.Properties.Name.ValueOrDefault));
  public string? AutomationId => Safe(() => NullIfEmpty(_el.Properties.AutomationId.ValueOrDefault));
  public string ControlType => Safe(() => _el.ControlType.ToString()) ?? "Unknown";
  public string? ClassName => Safe(() => NullIfEmpty(_el.Properties.ClassName.ValueOrDefault));
  public bool IsEnabled => Safe(() => _el.Properties.IsEnabled.ValueOrDefault, true);
  public bool IsOffscreen => Safe(() => _el.Properties.IsOffscreen.ValueOrDefault, false);
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
  public string? HelpText => Safe(() => NullIfEmpty(_el.Properties.HelpText.ValueOrDefault));

  public Rect BoundingRectangle => Safe(() =>
  {
    var r = _el.Properties.BoundingRectangle.ValueOrDefault;
    return new Rect((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
  }, default);

  public bool IsKeyboardFocusable => Safe(() => _el.Properties.IsKeyboardFocusable.ValueOrDefault, false);
  public bool HasKeyboardFocus => Safe(() => _el.Properties.HasKeyboardFocus.ValueOrDefault, false);

  public IReadOnlyList<IElement> Children
  {
    get
    {
      if (_children is not null) return _children;
      try
      {
        var raw = _el.FindAllChildren();
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

  private static string? NullIfEmpty(string? s) => string.IsNullOrEmpty(s) ? null : s;

  private static T Safe<T>(Func<T> f, T fallback)
  {
    try { return f(); } catch { return fallback; }
  }

  private static T? Safe<T>(Func<T?> f) where T : class
  {
    try { return f(); } catch { return null; }
  }
}
