namespace Adact.Engine;

/// <summary>
/// <see cref="WindowSession.InspectAsync(string, CancellationToken)"/> の返却値。
/// 設計 022 §8 で定義された UIA 要素の詳細プロパティを保持する。
/// JSON シリアライズして MCP / CLI に返す前提のデータ転送オブジェクト。
/// </summary>
/// <param name="Ref">対象 Element Ref ID (例: <c>s1e7</c>)。</param>
/// <param name="Name">UIA Name プロパティ。空文字列は <c>null</c>。</param>
/// <param name="ControlType">UIA ControlType の文字列表現 (例: <c>"Button"</c>)。</param>
/// <param name="AutomationId">UIA AutomationId プロパティ。空文字列は <c>null</c>。</param>
/// <param name="ClassName">Win32 ウィンドウクラス名。空文字列は <c>null</c>。</param>
/// <param name="HelpText">UIA HelpText プロパティ。空文字列は <c>null</c>。</param>
/// <param name="Value">ValuePattern.Value。Pattern を持たない / 取得失敗時は <c>null</c>。</param>
/// <param name="BoundingRect">UIA BoundingRectangle (スクリーン座標)。</param>
/// <param name="IsEnabled">UIA IsEnabled プロパティ。</param>
/// <param name="IsOffscreen">UIA IsOffscreen プロパティ。</param>
/// <param name="IsKeyboardFocusable">UIA IsKeyboardFocusable プロパティ。</param>
/// <param name="HasKeyboardFocus">UIA HasKeyboardFocus プロパティ。</param>
/// <param name="Patterns">対応 Pattern とその状態。Pattern を持たない要素では空辞書。</param>
/// <param name="Selector">安定セレクタ候補。算出不能時は <c>null</c>。</param>
public sealed record InspectResult(
    string Ref,
    string? Name,
    string ControlType,
    string? AutomationId,
    string? ClassName,
    string? HelpText,
    string? Value,
    Rect BoundingRect,
    bool IsEnabled,
    bool IsOffscreen,
    bool IsKeyboardFocusable,
    bool HasKeyboardFocus,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> Patterns,
    SelectorSuggestion? Selector = null);
