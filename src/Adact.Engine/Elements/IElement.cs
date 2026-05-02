namespace Adact.Engine.Elements;

/// <summary>
/// UIA 要素の Engine 内抽象。FlaUI 直接依存を避けて L2 テストで FakeElement を使えるようにする。
/// </summary>
public interface IElement
{
    /// <summary>UIA Name プロパティ (空文字列は <c>null</c>)。</summary>
    string? Name { get; }

    /// <summary>UIA AutomationId プロパティ (空文字列は <c>null</c>)。</summary>
    string? AutomationId { get; }

    /// <summary>UIA ControlType の文字列表現 (例: <c>"Button"</c>)。取得失敗時は <c>"Unknown"</c>。</summary>
    string ControlType { get; }

    /// <summary>Win32 ウィンドウクラス名 (空文字列は <c>null</c>)。</summary>
    string? ClassName { get; }

    /// <summary>UIA IsEnabled プロパティ (取得失敗時は安全側の true)。</summary>
    bool IsEnabled { get; }

    /// <summary>UIA SelectionItemPattern.IsSelected (パターンを持たない要素では false)。</summary>
    bool IsSelected { get; }

    /// <summary>UIA IsOffscreen プロパティ (取得失敗時は false)。</summary>
    bool IsOffscreen { get; }

    /// <summary>ValuePattern の Value (空文字列は <c>null</c>)。Pattern を持たない要素では <c>null</c>。</summary>
    string? Value { get; }

    /// <summary>UIA HelpText プロパティ (空文字列は <c>null</c>)。</summary>
    string? HelpText { get; }

    /// <summary>BoundingRectangle (スクリーン座標、取得失敗時は既定値)。</summary>
    Rect BoundingRectangle { get; }

    /// <summary>UIA IsKeyboardFocusable プロパティ (取得失敗時は false)。</summary>
    bool IsKeyboardFocusable { get; }

    /// <summary>UIA HasKeyboardFocus プロパティ (取得失敗時は false)。</summary>
    bool HasKeyboardFocus { get; }

    /// <summary>UIA RuntimeId 配列 (取得不能時は <c>null</c>)。RefRegistry の StableKey 計算に用いる。</summary>
    IReadOnlyList<int>? RuntimeId { get; }

    /// <summary>子要素の列挙 (FindAllChildren 相当)。失敗時は空配列。</summary>
    IReadOnlyList<IElement> Children { get; }

    /// <summary>子要素のキャッシュをクリアする。UIA ツリーが動的に変化する場合に、次回 Children アクセス時に再取得されるようにする。</summary>
    void ClearChildrenCache();

    /// <summary>InvokePattern が利用可能ならそれで、そうでなければ FlaUI の <c>Click()</c> でクリックする。</summary>
    void Click();

    /// <summary>テキストを入力する。ValuePattern が利用可能ならそれで、不可の場合は Ctrl+A→Delete→Type にフォールバック。</summary>
    /// <param name="text">入力するテキスト。</param>
    void Fill(string text);

    /// <summary>UIA <c>SetFocus</c> によりキーボードフォーカスを当てる。失敗時は best-effort。</summary>
    void Focus();
}

/// <summary>Toggle / checkbox 的な操作を fake 可能にする任意 capability。</summary>
public interface ICheckableElement
{
    /// <summary>現在 checked / selected 状態なら true。</summary>
    bool IsChecked { get; }

    /// <summary>checked / selected 状態を設定する。</summary>
    /// <param name="isChecked">設定する状態。</param>
    void SetChecked(bool isChecked);
}

/// <summary>List / ComboBox 的な選択操作を fake 可能にする任意 capability。</summary>
public interface ISelectableElement
{
    /// <summary>選択する。</summary>
    /// <param name="name">選択名。</param>
    /// <param name="index">選択 index。</param>
    /// <param name="item">選択 item。</param>
    void SelectItem(string? name, int? index, IElement? item);
}

/// <summary>ScrollIntoView 操作を fake 可能にする任意 capability。</summary>
public interface IScrollableElement
{
    /// <summary>要素を表示領域へ scroll する。</summary>
    void ScrollIntoView();
}
