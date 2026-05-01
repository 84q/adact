using System.Diagnostics;

using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

using Microsoft.Extensions.Logging;

using EngineMouseButton = Adact.Engine.MouseButton;
using FlaUiMouseButton = FlaUI.Core.Input.MouseButton;

namespace Adact.Engine;

/// <summary>
/// WindowSession の keyboard / mouse / auto-wait 操作境界。
/// Production は FlaUI / Process を使い、テストでは fake driver を差し込む。
/// </summary>
internal interface IWindowInteractionDriver
{
    void FocusWindow();
    void TypeKey(VirtualKeyShort key);
    void TypeChar(char ch);
    void TypeText(string text);
    void PressKey(VirtualKeyShort key);
    void ReleaseKey(VirtualKeyShort key);
    void MoveTo(int x, int y);
    void MouseDown(EngineMouseButton button);
    void MouseUp(EngineMouseButton button);
    void MouseClick(EngineMouseButton button);
    void MouseDoubleClick(EngineMouseButton button);
    void Scroll(int amount);
    void HorizontalScroll(int amount);
    Task WaitAfterInteractionAsync(CancellationToken ct);
}

internal sealed class FlaUiWindowInteractionDriver : IWindowInteractionDriver
{
    private readonly Window _window;
    private readonly int _processId;
    private readonly ILogger _logger;

    public FlaUiWindowInteractionDriver(Window window, int processId, ILogger logger)
    {
        _window = window;
        _processId = processId;
        _logger = logger;
    }

    public void FocusWindow()
    {
        try { _window.Focus(); } catch { }
    }

    public void TypeKey(VirtualKeyShort key) => Keyboard.Type(key);
    public void TypeChar(char ch) => Keyboard.Type(ch);
    public void TypeText(string text) => Keyboard.Type(text);
    public void PressKey(VirtualKeyShort key) => Keyboard.Press(key);
    public void ReleaseKey(VirtualKeyShort key) => Keyboard.Release(key);
    public void MoveTo(int x, int y) => Mouse.MoveTo(x, y);
    public void MouseDown(EngineMouseButton button) => Mouse.Down(MapButton(button));
    public void MouseUp(EngineMouseButton button) => Mouse.Up(MapButton(button));
    public void MouseClick(EngineMouseButton button) => Mouse.Click(MapButton(button));
    public void MouseDoubleClick(EngineMouseButton button) => Mouse.DoubleClick(MapButton(button));
    public void Scroll(int amount) => Mouse.Scroll(amount);
    public void HorizontalScroll(int amount) => Mouse.HorizontalScroll(amount);

    public async Task WaitAfterInteractionAsync(CancellationToken ct)
    {
        try
        {
            using var p = Process.GetProcessById(_processId);
            try { p.WaitForInputIdle(1000); }
            catch (Exception ex) { _logger.LogDebug(ex, "WaitForInputIdle failed (ignored, best effort)"); }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "GetProcessById failed during auto-wait (ignored)");
        }
        await Task.Delay(50, ct).ConfigureAwait(false);
    }

    private static FlaUiMouseButton MapButton(EngineMouseButton button)
    {
        return button switch
        {
            EngineMouseButton.Right => FlaUiMouseButton.Right,
            EngineMouseButton.Middle => FlaUiMouseButton.Middle,
            _ => FlaUiMouseButton.Left,
        };
    }
}

internal sealed class NoopWindowInteractionDriver : IWindowInteractionDriver
{
    public List<string> Calls { get; } = [];
    public void FocusWindow() => Calls.Add("focus-window");
    public void TypeKey(VirtualKeyShort key) => Calls.Add($"type-key:{key}");
    public void TypeChar(char ch) => Calls.Add($"type-char:{ch}");
    public void TypeText(string text) => Calls.Add($"type-text:{text}");
    public void PressKey(VirtualKeyShort key) => Calls.Add($"press-key:{key}");
    public void ReleaseKey(VirtualKeyShort key) => Calls.Add($"release-key:{key}");
    public void MoveTo(int x, int y) => Calls.Add($"move:{x},{y}");
    public void MouseDown(EngineMouseButton button) => Calls.Add($"mouse-down:{button}");
    public void MouseUp(EngineMouseButton button) => Calls.Add($"mouse-up:{button}");
    public void MouseClick(EngineMouseButton button) => Calls.Add($"mouse-click:{button}");
    public void MouseDoubleClick(EngineMouseButton button) => Calls.Add($"mouse-dblclick:{button}");
    public void Scroll(int amount) => Calls.Add($"scroll:{amount}");
    public void HorizontalScroll(int amount) => Calls.Add($"hscroll:{amount}");
    public Task WaitAfterInteractionAsync(CancellationToken ct)
    {
        Calls.Add("wait");
        return Task.CompletedTask;
    }
}
