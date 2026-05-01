# 029 Notepad++ の snapshot にエディタ内部テキスト構造が含まれない問題

> 関連: [028_UWPアプリsnapshot内部要素取得問題.md](028_UWPアプリsnapshot内部要素取得問題.md)
> 作成日: 2026-05-01 (JST)

---

## 1. 現象

Notepad++ に attach して `snapshot` を取得すると、エディタ領域は `Pane` として検出されるが、**テキスト行や個別の文字などの内部構造が UIA 要素として取得できない**。

### Snapshot 出力例

```
- Window "*新規1 - Notepad++ [Administrator]" [ref=s1e1]
  - Pane "aa\r\nb" [ref=s1e2]        ← エディタ領域（Scintilla）
    - Button "1 行上" [ref=s1e4]      ← 垂直スクロールバー
    - Thumb "表示位置" [ref=s1e5]
    - Button "1 行下" [ref=s1e7]
  - Tab "Tab" [ref=s1e8]
  - ToolBar [ref=s1e18]
  ...
```

`Pane "aa\r\nb"`（`ref=s1e2`）がエディタ本体だが、子要素はスクロールバー関連のみで、テキスト内容の構造化表現（行・段落・文字）がない。

---

## 2. 調査結果

### 2.1 UIA ツリー構造

| 項目 | 値 |
| --- | --- |
| ウィンドウ ClassName | `Notepad++` |
| エディタ ClassName | `Scintilla` |
| エディタ ControlType | `Pane` |
| エディタ AutomationId | `<not-supported>`（未取得） |

### 2.2 サポートされている UIA パターン

| パターン | 状態 | 備考 |
| --- | --- | --- |
| `LegacyIAccessible` | ✅ サポート | 旧来の MSAA 互換 |
| `Scroll` | ✅ サポート | スクロール操作 |
| `ValuePattern` | ❌ 未サポート | テキスト値の取得・設定が不可 |
| `TextPattern` | ❌ 未サポート | テキスト範囲・行・文字単位の操作が不可 |

### 2.3 子要素・子孫要素

- `FindAllChildren()` の結果: **1 要素**（垂直スクロールバー `ScrollBar`）
- `FindAllDescendants()` の結果: **5 要素**（スクロールバー + 上下ボタン + Thumb）
- テキスト行や文字要素: **0 要素**

### 2.4 比較: 標準 Edit コントロール（メモ帳）との違い

| 項目 | メモ帳（標準 Edit） | Notepad++（Scintilla） |
| --- | --- | --- |
| ControlType | `Edit` | `Pane` |
| ValuePattern | ✅ サポート | ❌ 未サポート |
| TextPattern | ✅ サポート | ❌ 未サポート |
| テキスト値の取得 | `ValuePattern.Value` で可能 | `LegacyIAccessible.Name` のみ（内容全体が文字列化される） |
| 内部構造の要素化 | 行・文字は要素化されないが、`TextPattern` で範囲操作が可能 | 要素化も `TextPattern` も不可 |

---

## 3. 原因

**Notepad++ が使用している Scintilla コントロールが、UI Automation のテキスト関連プロバイダーを実装していない**。

Scintilla は Win32 HWND ベースのネイティブリッチテキストエディタコンポーネントで、以下の UIA プロバイダーを持たない:

- `ValuePattern` プロバイダー → テキスト値の get/set が不可
- `TextPattern` プロバイダー → ドキュメント範囲・行・文字単位の操作が不可
- `TextChildPattern` プロバイダー → テキストコンテナ内の子テキスト要素列挙が不可

Scintilla は `LegacyIAccessible`（MSAA 互換）のみを提供しており、`Name` プロパティに現在のテキスト内容全体を文字列として格納している。これは UIA 的には「Pane 要素のラベル」として扱われ、構造化されたテキストツリーではない。

---

## 4. 影響範囲

### 4.1 操作面

| 操作 | 影響 |
| --- | --- |
| `click` | エディタ `Pane` へのクリックは可能（フォーカス移動） |
| `fill` | ❌ **不可** — `ValuePattern` がないため `SetValue` が使えない |
| `type` | ❌ **不可** — `ValuePattern` または `TextPattern` がないためテキスト注入が標準的にできない |
| `press` | エディタフォーカス時のキー操作は `LegacyIAccessible` 経由で一部可能な可能性あり（未検証） |

### 4.2 Snapshot 面

- エディタ領域は `Pane` として 1 要素に集約される
- テキスト内容は `Pane.Name` に格納される（例: `"aa\r\nb"`）
- 行単位での要素取得、キャレット位置の特定、選択範囲の検出が UIA では不可能

---

## 5. これは ADACT のバグか？

**いいえ。これは Scintilla コントロールの UIA 実装の制限であり、ADACT / FlaUI / UIA フレームワーク側の問題ではない。**

以下でも同様の結果になる:
- Microsoft Accessibility Insights
- Inspect.exe（Windows SDK）
- その他の UIA ベースツール

---

## 6. 対応方針

### 6.1 現状維持（推奨）

Notepad++ に対する `fill` / `type` 操作は、Scintilla の UIA 制約により標準的にはサポートしない。

代わりの操作手段:
- `press` でのキー入力（エディタフォーカス後）
- `type` が必要な場合は、Scintilla 特有の操作（`LegacyIAccessible` 経由の直接操作、または Win32 メッセージ送信）が必要だが、これは ADACT の標準スコープ外

### 6.2 将来的な拡張（非推奨・工数大）

Scintilla 固有のハンドリングを追加する場合:
- `LegacyIAccessible` 経由で `Value` を取得・設定するカスタムロジック
- Win32 `WM_SETTEXT` / `WM_GETTEXT` メッセージ送信
- ただし、これは Scintilla 固有の実装になり、汎用性がなく保守コストが高い

---

## 7. 関連ファイル

- `src/Adact.Engine/Elements/FlaUiElement.cs`: UIA 要素のラッパー
- `src/Adact.Engine/Snapshot/SnapshotBuilder.cs`: snapshot 生成
- `src/Adact.Engine/WindowSession.cs`: 要素操作の直列化

---

## 8. 結論

Notepad++ のエディタ内部テキスト構造が snapshot に含まれないのは、**Scintilla コントロールが UIA のテキスト関連パターン（`ValuePattern`, `TextPattern`）を実装していないため**。これは ADACT の制限ではなく、対象アプリケーション（Notepad++）の UIA プロバイダー実装の制約である。`Pane` 要素としてエディタ領域は検出されるが、`fill` / `type` によるテキスト操作は標準的な UIA 経路では不可能。
