# Phase 8 要件定義: 操作系コマンドの拡充

## 背景・目的

Phase 7 までで attach / snapshot / click / fill / detach / close / kill / close-all / list-apps / daemon-stop の最小コアが揃った。
しかし実際の Windows GUI 自動化では、これだけでは表現しきれない操作が多数ある（ダブルクリック、右クリック、キー送出、要素単独の値取得、ウィンドウ操作、出現待ち等）。

Phase 8 ではこれらを段階的に拡充する。基本方針として **Playwright Locator API** を参考とし、Web 専用機能（DOM 評価系・iframe 系・ARIA 系）は除外、UIA / Win32 で意味を持つものに限定する。

参考: [Playwright Locator class](https://playwright.dev/docs/api/class-locator)

## 採用コマンド一覧

### 操作系

| コマンド | 概要 | 主な引数・オプション |
| --- | --- | --- |
| `click` (拡張) | 既存 click をオプション拡張 | `--button left/right/middle`, `--count N`, `--modifier <keys>`, `--position x,y` (要素相対) |
| `dblclick` | ダブルクリック | click と同オプション |
| `hover` | マウスホバー | `--modifier`, `--position` |
| `type` | 逐次タイピング (Playwright `pressSequentially` 相当) | text, `--delay` |
| `check` / `uncheck` | TogglePattern を明示操作 | ref |
| `select` | ComboBox / List の選択肢を選ぶ | ref, value/label/index |
| `focus` | 要素にフォーカス | ref |
| `clear` | 入力欄を空にする | ref |
| `scroll-into-view` | 要素を可視範囲にスクロール | ref |
| `press` | キー押下 (組合せ含む 1 引数) | `"Ctrl+Shift+E"` のような Playwright 形式文字列 |
| `key-down` / `key-up` | 単キーの押下 / 解放 | 単キーのみ。組合せは `press` で表現 |
| `mouse-move` | マウスカーソル移動 | target は `s1e2` (ref) または `20,30` (座標) |
| `mouse-down` / `mouse-up` | マウスボタン押下 / 解放 | target 同上, `--button` |
| `mouse-wheel` | ホイールスクロール | `--x N --y N`、正値 = 下/右、負値 = 上/左 (Playwright/DOM 流) |

### 取得系

| コマンド | 概要 | 備考 |
| --- | --- | --- |
| `inspect <ref>` | 要素単独の詳細情報を取得 | snapshot より詳細な単一要素の情報取得用。最低限以下を含む: name / controlType / value / boundingRect / isEnabled / isOffscreen / automationId / className / helpText / 各 Pattern の状態 (Toggle/Selection/ExpandCollapse 等)。子要素サマリは含めない (snapshot に任せる)。詳細項目は設計・実装後に増減を判断。 |
| `screenshot` | 画面キャプチャ保存 | window 全体 / 要素指定の両対応。`--highlight` オプションで対象要素をハイライトしてキャプチャ。 |

### 同期系

| コマンド | 概要 |
| --- | --- |
| `wait-for` | 要素の出現 / 消失 / 有効化を待つ |
| `wait-for-window` | 指定条件のウィンドウが出現するのを待つ専用コマンド |

### ウィンドウ操作系

| コマンド | 概要 |
| --- | --- |
| `resize` | ウィンドウサイズ変更 |
| `minimize` | 最小化 |
| `maximize` | 最大化 |
| `restore` | 通常状態へ復帰 |

最小化中もユーザー操作は試行する（UIA 操作は座標非依存なものもあるため）。失敗時の挙動は設計フェーズで詰める。

## 共通方針

### 引数判別ルール (target)

`mouse-move` 等で要素 ref と座標の両方を受け取る場合、1 つの文字列引数で受け取り内容により判別する:

- `^s\d+e\d+$` → 要素 ref として解釈
- `^\d+,\d+$` → 絶対座標として解釈
- それ以外 → エラー

### 命名規則

- CLI: kebab-case (`mouse-wheel`, `wait-for-window`, `scroll-into-view`)
- MCP: snake_case + `windows_` prefix (`windows_mouse_wheel`, `windows_wait_for_window`)
- CLI ↔ MCP は基本 1:1 対応

### キー指定 (Playwright 流)

- `press` の引数は組合せ含めて 1 文字列: `"Ctrl+Shift+E"`, `"Control+o"`, `"Enter"`, `"F1"`
- 修飾キー名: `Shift`, `Control`, `Alt`, `Meta`, `ControlOrMeta`
- click / hover などの「動作中に押下しっぱなしにする修飾キー」は別途 `--modifier` オプションで指定

## 不採用 / 見送り

| 項目 | 理由 |
| --- | --- |
| `blur` | 別要素に focus すれば実質達成可能 |
| `drag` / `drop` / `drag-drop` | 実装難度が高く Phase 8 では見送り。後続フェーズで再検討 |
| `menu-select` | UIA での実装難度次第。Phase 8 では見送り、必要なら後続で検討 |
| `highlight` (単独コマンド) | `screenshot --highlight` オプションに統合 |
| `dispatchEvent` / `evaluate` 系 / `frameLocator` / `getBy*` | Web 専用、UIA に対応概念がない |
| `setInputFiles` | ネイティブダイアログ操作が必要で UIA 標準操作で吸収困難 |
| `tap` | タッチ前提で本プロジェクトのスコープ外 |

## 設計フェーズへの引き継ぎ事項

要件定義レベルで決まらず、設計フェーズで詰める論点:

- 各 CLI 操作で attach/click/fill 同様、成功時に自動 snapshot を発火するか、`--no-snapshot` 抑止するか
- minimize 中の操作で失敗した場合の挙動（自動 restore か、エラーかを実装結果を見て判断）
- `inspect` の返却項目の最終確定（実装結果を見て増減）
- `wait-for` / `wait-for-window` のクエリ条件の表現方法
- `select` の選択指定方式（value / label / index 表現）
- `screenshot --highlight` の見た目仕様
- 各コマンドの WindowSession / Engine への追加メソッド方針
- MCP ツール定義（パラメータスキーマ）
- Skill 同期 (`src/Adact.Cli/Skills/adact-cli/references/<cmd>.md` および `tests/Adact.Cli.Tests/Unit/InstallCommandTests.cs` の `ExpectedDocumentedCommands`) の更新範囲

## 実装スコープの段階分け（参考）

実装は一括ではなく、優先度順にバッチ分割する想定。最終配分は設計フェーズで決定。

- 優先高: `dblclick`, `press`, `select`, `screenshot`, `wait-for`, `wait-for-window`, `inspect`, `mouse-wheel`
- 優先中: `hover`, `type`, `check` / `uncheck`, `focus`, `clear`, `scroll-into-view`, `click` 拡張, ウィンドウ系 (`resize` / `minimize` / `maximize` / `restore`), `mouse-move` / `mouse-down` / `mouse-up`, `key-down` / `key-up`
