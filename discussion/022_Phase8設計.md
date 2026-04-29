# Phase 8 設計: 操作系コマンドの拡充

要件定義: [021_Phase8要件定義.md](021_Phase8要件定義.md)

本ドキュメントでは Phase 8 の技術的実現方法を確定する。

## 1. 実装バッチ方針

Phase 8 は **1 PR で一括実装** する。優先高 → 優先中の順に実装するが、PR は分けない。

## 2. auto-snapshot 発火ポリシー

CLI 側で成功時に自動 snapshot を発火する既存パターン (attach/click/fill) を踏襲する。新規コマンドは以下に分類:

| 分類 | コマンド | auto-snapshot |
| --- | --- | --- |
| 状態変化を伴う | `click`(拡張), `dblclick`, `hover`, `type`, `check`, `uncheck`, `select`, `clear`, `press`, `mouse-wheel`, `resize`, `minimize`, `maximize`, `restore` | あり |
| 低レベル補助 | `focus`, `scroll-into-view`, `mouse-move`, `mouse-down`, `mouse-up`, `key-down`, `key-up` | なし |
| 取得・同期 | `inspect`, `screenshot`, `wait-for`, `wait-for-window` | なし |

すべての auto-snapshot 対象コマンドは `--no-snapshot` 抑止オプションを持つ（既存と同じ）。

## 3. Engine 層の構造

`Adact.Engine.WindowSession` を **partial class** に分割する。公開 API はフラット (`session.Click(...)`, `session.MouseMove(...)` 等)、実装ファイルだけ機能別に分ける。

```
src/Adact.Engine/
  WindowSession.cs           // 既存。コア (ctor, Snapshot, Click, Fill, Detach 等)
  WindowSession.Mouse.cs     // mouse-move/down/up/wheel, click 拡張, dblclick, hover
  WindowSession.Keyboard.cs  // press, key-down/up, type
  WindowSession.Toggle.cs    // check, uncheck, select, focus, clear, scroll-into-view
  WindowSession.Window.cs    // resize, minimize, maximize, restore
  WindowSession.Inspect.cs   // inspect
  WindowSession.Wait.cs      // wait-for (要素), wait-for-window は WindowSession 外 (UiaEngine 側)
```

`wait-for-window` はセッション外（attach 前/別ウィンドウ対象）なので `Adact.Engine.UiaEngine` 側に追加する。

## 4. 共通 target 引数の解析

`Adact.Engine` に `MouseTarget` record 型を新設:

```
public abstract record MouseTarget
{
    public sealed record ByRef(string Ref) : MouseTarget;
    public sealed record ByPoint(int X, int Y) : MouseTarget;

    public static MouseTarget Parse(string input);
    // "^s\d+e\d+$"     -> ByRef
    // "^-?\d+,-?\d+$"  -> ByPoint (マルチモニタ対応で負値を許可)
    // それ以外          -> ArgumentException
}
```

`Parse` は CLI / MCP 両方から呼び出す。Engine の各メソッドは `MouseTarget` を引数として受ける（オーバーロード分岐ではなく型で表現）。

## 5. MCP ツール定義のグルーピング

`Adact.Mcp.Common.WindowsTools` を **partial class** に分割し、Engine 構造と揃える。

```
src/Adact.Mcp.Common/
  WindowsTools.cs            // 既存。コア (list_apps, attach, snapshot, click, fill, detach, close, kill, close_all, daemon_stop)
  WindowsTools.Mouse.cs      // windows_mouse_move/down/up/wheel, windows_dblclick, windows_hover (click 拡張は本体)
  WindowsTools.Keyboard.cs   // windows_press, windows_key_down/up, windows_type
  WindowsTools.Toggle.cs     // windows_check/uncheck/select/focus/clear/scroll_into_view
  WindowsTools.Window.cs     // windows_resize/minimize/maximize/restore
  WindowsTools.Inspect.cs    // windows_inspect
  WindowsTools.Wait.cs       // windows_wait_for, windows_wait_for_window
```

CLI ↔ MCP は 1:1 対応を維持（既存規約踏襲）。

## 6. コマンド引数仕様

CLI コマンドはすべて kebab-case、MCP は `windows_` prefix の snake_case。

### 共通オプション

- `--ref <ref>`: 操作対象の要素 ref (`s1e2` 等)
- `--no-snapshot`: auto-snapshot 抑止 (該当コマンドのみ)
- `--timeout <ms>`: 既存パターン踏襲

### 各コマンド

| コマンド | 引数・オプション |
| --- | --- |
| `click` (拡張) | `--ref <ref>`, `--button left/right/middle` (default: left), `--count N` (default: 1), `--modifier <key>` (繰り返し可、例: `--modifier Ctrl --modifier Shift`), `--position x,y` (要素相対、default: 中央)。`--count` は **汎用 N 連打**であり、OS ダブルクリック判定の保証はしない。ダブルクリック判定が必要な場合は `dblclick` を使用する |
| `dblclick` | `--ref`, `--button`, `--modifier`, `--position` |
| `hover` | `--ref`, `--modifier`, `--position` |
| `type` | `--ref`, text, `--delay <ms>` |
| `check` / `uncheck` | `--ref` |
| `select` | `--ref`, `--name <text>` または `--index <n>` または `--item-ref <ref>` のいずれか必須 |
| `focus` | `--ref` |
| `clear` | `--ref` |
| `scroll-into-view` | `--ref` |
| `press` | key (1 引数: `"Ctrl+Shift+E"` 形式), `--ref` (任意。指定時はその要素にフォーカス後送出) |
| `key-down` / `key-up` | key (単キー) |
| `mouse-move` | target (`s1e2` または `20,30`) |
| `mouse-down` / `mouse-up` | target, `--button` |
| `mouse-wheel` | target, `--delta-x <delta>`, `--delta-y <delta>` (Playwright/DOM 流: 正値=下/右、負値=上/左) |
| `inspect` | ref |
| `screenshot` | `--ref <ref>` (任意。未指定はウィンドウ全体), `--highlight` (対象 ref をハイライト)、その他 (`--out` 等) は既存 `snapshot` コマンドの該当オプションと揃える |
| `wait-for` | `--ref <ref>` または検索条件 (`--name`, `--control-type`, `--automation-id` 等)、`--state attached/detached/visible/hidden/enabled/disabled`, `--timeout <ms>` |
| `wait-for-window` | `--window-key`, `--title`, `--class-name`, `--process-name`, `--exe`（既存 attach クエリと互換）, `--timeout <ms>` |
| `resize` | `--width <w>`, `--height <h>` |
| `minimize` / `maximize` / `restore` | (引数なし。アタッチ済ウィンドウに対して操作) |

### キー指定 (Playwright 流)

- `press` は組合せ含めて 1 文字列: `"Ctrl+Shift+E"`, `"Control+o"`, `"Enter"`, `"F1"`
- 修飾キー名: `Shift`, `Control`, `Alt`, `Meta`, `ControlOrMeta`
- click / hover / dblclick の `--modifier` は **繰り返し可能オプション** (`--modifier Ctrl --modifier Shift`)。System.CommandLine 流に従い、修飾キー名は case-insensitive で受理 (`Shift` / `Control` / `Ctrl` / `Alt` / `Meta` / `ControlOrMeta`)

## 7. wait-for の動作モード

| モード | 入力 | 動作 |
| --- | --- | --- |
| ref モード | `--ref s1e2` | 指定 ref の状態 (attached/detached/visible/hidden/enabled/disabled) を待つ |
| 検索条件モード | `--name "保存"` 等 | snapshot を内部リトライしながら一致要素の出現を待つ |

`--state` のデフォルトは `visible`（Playwright と揃える）。

## 8. inspect の返却スキーマ

最低限以下を含む（実装結果を見て増減する前提）:

- `name`, `controlType`, `automationId`, `className`, `helpText`
- `value` (ValuePattern)
- `boundingRect`: { `x`, `y`, `width`, `height` }
- `isEnabled`, `isOffscreen`, `isKeyboardFocusable`, `hasKeyboardFocus`
- `patterns`: 対応 Pattern と状態
  - `Toggle`: `ToggleState`
  - `SelectionItem`: `IsSelected`
  - `ExpandCollapse`: `ExpandCollapseState`
  - `RangeValue`: { `Min`, `Max`, `Value` }
  - `Window`: { `VisualState`, `InteractionState` }

子要素サマリは含めない（snapshot に任せる）。

## 9. select の選択指定

`--name` / `--index` / `--item-ref` のいずれか **1 つ必須**。

| 指定 | 内部動作 |
| --- | --- |
| `--name <text>` | UIA で子 ListItem を name で検索し SelectionItemPattern.Select |
| `--index <n>` | 0-based で n 番目の子 ListItem を Select |
| `--item-ref <ref>` | snapshot で得られた子要素 ref を直接 Select |

ComboBox 等で選択肢が closed 状態の場合、必要に応じて事前に ExpandCollapsePattern.Expand を呼ぶ。

## 10. screenshot

- 既存 `snapshot` コマンドの保存先・命名規則を踏襲（デフォルト `.adact/` 配下）
- 画像形式は **PNG 固定**
- `--ref <ref>` 指定時は該当要素の bounding rect でクリップ
- `--highlight` フラグ指定時は、キャプチャ前に対象要素を一時的にハイライト描画

## 11. minimize 中の操作

要件定義の方針通り「操作を試みる」。座標非依存な UIA 操作 (Invoke / SelectionItem.Select / Toggle / SetValue 等) は最小化中でも成功し得る。座標を必要とする操作 (mouse-move 等) は失敗する可能性がある。失敗時の挙動 (自動 restore するか、エラーで返すか) は **実装結果を見てから決定**（暫定: エラーで返す）。

## 12. Skill 同期

新規コマンドごとに、本 PR 内で同一コミットとして以下を更新する:

1. `src/Adact.Cli/Skills/adact-cli/references/<cmd>.md` を新規作成 (英語、agentskills.io 仕様)
2. `src/Adact.Cli/Skills/adact-cli/SKILL.md` のコマンドリスト更新
3. `tests/Adact.Cli.Tests/Unit/InstallCommandTests.cs` の `ExpectedDocumentedCommands` を更新

## 13. テスト方針

[testing-strategy](../.github/skills/testing-strategy/SKILL.md) に従い、以下のレイヤーでテストを追加する:

| レイヤー | 内容 |
| --- | --- |
| Unit (Adact.Engine.Tests) | `MouseTarget.Parse` などのロジック単体 |
| Unit (Adact.Mcp.Common.Tests) | MCP ツール引数の正規化 |
| Unit (Adact.Cli.Tests) | `InstallCommandTests.ExpectedDocumentedCommands` 更新確認 |
| Integration (Adact.Engine.Tests, FlaUI 利用) | 各操作が実機 UIA に対して期待通り動くか |
| E2E (Adact.Cli.Tests) | CLI コマンドのスモーク（電卓 / Notepad 等） |
| E2E (Adact.Mcp.Stdio.Tests / Adact.Mcp.Http.Tests) | MCP ツール経由の動作確認 |

詳細な対象アプリ・ケース選定は実装フェーズで決定。

## 14. 実装順序の目安（参考）

実装フェーズで以下の順序で進める想定（1 PR 内で全部）:

1. `MouseTarget` 型と Parse の追加 + ユニットテスト
2. `WindowSession` の partial 化（既存コードの分割のみ、機能追加なし）
3. `WindowsTools` の partial 化（同上）
4. 操作系コマンド追加 (Mouse/Keyboard/Toggle 各カテゴリ)
5. ウィンドウ系コマンド追加
6. inspect / screenshot 追加
7. wait-for / wait-for-window 追加
8. CLI 側の auto-snapshot 統合
9. Skill 同期 (references / SKILL.md / InstallCommandTests)
10. ドキュメント更新 (README 等の必要箇所)

review-loop に従い、Implementation サブエージェントの完了ごとに get_errors → Research レビュー → 修正 → commit を行う。
