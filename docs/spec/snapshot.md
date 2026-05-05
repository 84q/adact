# Snapshot 仕様

ADACT の snapshot は、Engine/MCP 層では raw JSON、CLI 層では AI が読みやすい `.txt` 形式として扱います。Phase 7 でフィルタと整形の責務は Engine/MCP から CLI に移りました。

## 責務分担

| 層 | 入力 | 出力 | 責務 |
| --- | --- | --- | --- |
| Engine | UIA tree | raw JSON | UIA から取得できるプロパティと子要素を可能な限り欠落なく出す |
| MCP Common | Engine raw JSON | MCP response | raw JSON をそのまま返す。filter / text 化はしない |
| CLI parser | MCP raw JSON | DTO | `_meta` と `tree` を `SnapshotElement` に変換する |
| CLI filter | DTO | filtered DTO | `operable` / `raw` のツリー filter を適用する |
| CLI formatter | filtered DTO | Playwright Aria YAML 風 text | AI が ref を読み取りやすい `.txt` を作る |
| CLI writer | text | `.txt` file | `.adact/` または `--snapshot-dir` に保存する |

## Engine / MCP raw JSON

raw JSON は次の top-level 構造です。

| フィールド | 内容 |
| --- | --- |
| `_meta` | snapshot metadata |
| `tree` | root UIA element |

`_meta` の主なフィールド:

| フィールド | 内容 |
| --- | --- |
| `options.maxDepth` | Engine 側の走査深度上限 |
| `generatedAt` | UTC timestamp |
| `sessionId` | `s<n>` |
| `windowTitle` | attached window title |
| `processName` | process name |
| `processId` | process id |
| `modalDialog` | 検出された modal dialog の summary。なければ `null`、あれば `[{ ref, title }, ...]` の配列 |

`tree` node の主なフィールド:

| フィールド | 内容 |
| --- | --- |
| `ref` | `s<sid>e<eid>` |
| `role` | UIA ControlType |
| `name` | UIA Name |
| `automationId` | UIA AutomationId |
| `className` | class name |
| `isEnabled` | enabled 状態 |
| `isOffscreen` | offscreen 状態 |
| `value` | ValuePattern 等から得た値 |
| `helpText` | HelpText。無名 button 対策にも使う |
| `boundingRect` | `[x, y, width, height]` |
| `isKeyboardFocusable` | keyboard focus 可能か |
| `hasKeyboardFocus` | focus 中か |
| `isModalDialog` | modal dialog として注入された node か |
| `children` | child nodes |

## CLI `.txt` snapshot

CLI は raw JSON を受け取り、frontmatter + Playwright Aria YAML 風 tree に整形します。

```text
---
filter: operable
sessionId: s1
processName: ApplicationFrameHost
processId: 10392
generatedAt: "2026-04-28T01:00:54.4221919Z"
---
- Window "電卓" [ref=s1e1]
  - Window "電卓" [aid="TitleBar"] [value="電卓"] [ref=s1e2]
    - Button "電卓 を閉じる" [aid="Close"] [ref=s1e7]
```

### frontmatter

| フィールド | 内容 |
| --- | --- |
| `filter` | `operable` または `raw` |
| `sessionId` | `s<n>` |
| `processName` | process name。存在する場合のみ |
| `processId` | process id。存在する場合のみ |
| `generatedAt` | raw JSON metadata の timestamp |

frontmatter の scalar は、英数字・空白・`_`・`-` のみなら裸、それ以外は double quote で囲みます。

### 本体形式

| 要素 | 形式 |
| --- | --- |
| 行 | `- Role "Name" [attr=...]` |
| 階層 | 2 spaces indent |
| Name | あれば double quote 付きで出力 |
| AutomationId | `[aid="..."]` |
| Value | `[value="..."]` |
| 状態 | `[disabled]`, `[focused]`, `[modal]` |
| Ref | `[ref=s1e7]` |

属性順は `aid`、`value`、state flags、`ref` です。`className`、`helpText`、`boundingRect`、`isKeyboardFocusable`、`isOffscreen` は text 出力には出しません。

## `operable` / `raw` filter

| filter | 挙動 |
| --- | --- |
| `raw` | tree 構造を維持する。text 出力で出すフィールドは formatter の対象に限る |
| `operable` | 操作可能・意味のある ControlType を残し、無名の構造要素を flatten し、offscreen subtree を除外する |

`operable` で常に残す主な role:

| 種類 | Role 例 |
| --- | --- |
| Window / menu | `Window`, `Menu`, `MenuBar`, `MenuItem`, `TitleBar`, `ToolBar`, `StatusBar` |
| 入力・操作 | `Button`, `Edit`, `CheckBox`, `RadioButton`, `ComboBox`, `Slider`, `Spinner`, `SplitButton` |
| 選択・構造 | `Tab`, `TabItem`, `Tree`, `TreeItem`, `List`, `ListItem`, `DataGrid`, `DataItem`, `Table` |
| 表示 | `Document`, `Text`, `Hyperlink`, `Header`, `HeaderItem` |

`Pane`, `Group`, `Custom`, `Thumb`, `Image`, `Separator` は、Name または AutomationId があれば残し、なければ自身を省いて子を親へ昇格します。未知 role は安全側で flatten します。

## Unicode / escape

| 対象 | 方針 |
| --- | --- |
| 日本語などの非 ASCII | そのまま出力する |
| double quote | `\"` |
| backslash | `\\` |
| newline | `\n` |
| tab | `\t` |
| 制御文字 | `\uXXXX` |

ADACT は Windows アプリの UI text を扱うため、snapshot text では Unicode を読みやすさ優先で保持します。

## 出力ファイル

| 項目 | 内容 |
| --- | --- |
| 既定 directory | `.adact/` |
| 変更 | `--snapshot-dir <dir>` |
| 拡張子 | `.txt` |
| 出力する CLI | `attach`, `snapshot`, および auto-snapshot 対象の操作系: `click`, `fill`, `dblclick`, `hover`, `type`, `check`, `uncheck`, `select`, `clear`, `resize`, `minimize`, `maximize`, `restore` |
| 出力しない CLI | 低レベル補助 (`press`, `mouse-move`, `mouse-down`, `mouse-up`, `mouse-wheel`, `key-down`, `key-up`, `focus`, `scroll-into-view`) と取得・同期系 (`inspect`, `screenshot`, `wait-for`, `wait-for-window`, `launch`)。これらは成功時に snapshot を生成しない |
| 抑止 | `--no-snapshot` を持つコマンド (`attach`, `click`, `fill`, `dblclick`, `hover`, `type`, `check`, `uncheck`, `select`, `clear`, `resize`, `minimize`, `maximize`, `restore`) で自動 snapshot を抑止できる |

旧 Phase 5 では `.json` snapshot が使われていました。現行仕様では CLI snapshot file は `.txt` です。

## 参照

| 文書 | 内容 |
| --- | --- |
| [../../discussion/017_Phase7_完了.md](../../discussion/017_Phase7_完了.md) | `.txt` snapshot 化の完了記録 |
| [ref-ids.md](ref-ids.md) | `[ref=...]` の形式 |
| [cli.md](cli.md) | `snapshot` / `--filter` / `--snapshot-dir` |

## 2026-05 CLI 出力統一補足

- snapshot file 自体は従来どおり frontmatter + tree の `.txt` として保存する。
- `adact snapshot` の stdout は `result: true`、`snapshotPath`、`---`、`sessionId`、空行、tree を出す。
- `attach` と auto-snapshot 対象コマンドは snapshot 本文を stdout に出さず、メタの `snapshotPath: ... (changed|unchanged)` だけを返す。
- `--no-snapshot` 時は `snapshotPath` を出さない。
