# Errors and Output

ADACT は CLI と MCP の両方で、成功データとエラーを明確に分けます。CLI の stdout は成功時の機械可読データ、stderr はエラーとログです。MCP の業務エラーは `isError: true` の tool result として返します。

## Exit codes

| Code | 名前 | 用途 |
| ---: | --- | --- |
| 0 | Success | 正常終了 |
| 1 | CommandFailed | daemon が応答した tool error、操作失敗、内部失敗 |
| 2 | UserError | CLI 段階の入力エラー、URL/config 不正、remote `daemon-stop` など |
| 3 | ConnectionFailed | daemon への接続失敗 |
| 4 | EnvironmentNotSupported | daemon 起動環境が不適切。現行では `NO_INTERACTIVE_SESSION` |

## CLI stderr

CLI エラーは stderr に key-value 形式で出します。

```text
error <CODE>
message <human-readable message>
hint <optional recovery hint>
```

| 行 | 必須 | 内容 |
| --- | --- | --- |
| `error` | 必須 | error code |
| `message` | 必須 | 人間向け説明 |
| `hint` | 任意 | 復旧手順 |

`adact serve` / `adact local` の起動時 `NO_INTERACTIVE_SESSION` もこの形式です。`serve` と `local` は成功時にも stderr に `info interactive session ok ...` を出します。

## CLI stdout

### key-value

多くの client command は `key value` を 1 行ずつ出します。

| コマンド | 例 |
| --- | --- |
| `attach` | `sessionId s1`, `windowRef w1`, `snapshot .adact/session-1-...txt` |
| `snapshot` | `sessionId s1`, `snapshot .adact/session-1-...txt` |
| `click` / `fill` / その他 auto-snapshot 対象 (`dblclick`, `hover`, `type`, `press`, `check`, `uncheck`, `select`, `clear`, `mouse-wheel`, `resize`, `minimize`, `maximize`, `restore`) | `sessionId s1`, `snapshot .adact/session-1-...txt` |
| `detach` | `sessionId s1`, `detached` |
| `close` | `sessionId s1`, `closed`, `detached` |
| `kill` | `sessionId s1`, `killed`, `detached` |

### TSV

| コマンド | 形式 |
| --- | --- |
| `list-apps` | header あり。`windowRef`, `sessionId`, `processName`, `processId`, `className`, `windowTitle` |
| `close-all` | header なし。`sessionId`, `result`, optional `error` |

### literal line

| コマンド | 出力 |
| --- | --- |
| `daemon-stop` | `stopped` |

### JSON 1 行

`inspect`, `screenshot`, `wait-for`, `wait-for-window`, `launch` は `Console.WriteLine(JsonSerializer.Serialize(...))` で 1 行の JSON を stdout に出します。

| コマンド | 形 |
| --- | --- |
| `inspect` | `{"ref":"s1e7","name":"...","controlType":"Button","automationId":"...","className":"...","helpText":"...","value":"...","boundingRect":{"x":0,"y":0,"width":0,"height":0},"isEnabled":true,"isOffscreen":false,"isKeyboardFocusable":true,"hasKeyboardFocus":false,"patterns":{...}}` |
| `screenshot` | `{"path":".adact/screenshot-s1-....png","width":800,"height":600}` |
| `wait-for` | `{"ref":"s1e7","state":"visible"}` (state は `attached` / `detached` / `visible` / `hidden` / `enabled` / `disabled` のいずれか) |
| `wait-for-window` | `{"processId":1234,"processName":"notepad","windowTitle":"Untitled - Notepad","controlType":"Window","className":"Notepad","nativeWindowHandle":...}` |
| `launch` | `{"pid":1234,"processName":"notepad.exe","executablePath":"C:\\Windows\\System32\\notepad.exe"}` |

### inspect の JSON スキーマ

`inspect` が返す JSON は `windows_inspect` のシリアライズと同形式で、以下のフィールドを 1 オブジェクトに含みます。

| フィールド | 型 | 内容 |
| --- | --- | --- |
| `ref` | string | 入力と同じ element ref |
| `name` | string? | UIA Name |
| `controlType` | string | UIA ControlType |
| `automationId` | string? | UIA AutomationId |
| `className` | string? | Win32 ClassName |
| `helpText` | string? | UIA HelpText |
| `value` | string? | ValuePattern.Value |
| `boundingRect` | object | `{ "x", "y", "width", "height" }` |
| `isEnabled` | bool | UIA IsEnabled |
| `isOffscreen` | bool | UIA IsOffscreen |
| `isKeyboardFocusable` | bool | UIA IsKeyboardFocusable |
| `hasKeyboardFocus` | bool | UIA HasKeyboardFocus |
| `patterns` | object | 対応 Pattern と状態。`Toggle: { ToggleState }`、`SelectionItem: { IsSelected }`、`ExpandCollapse: { ExpandCollapseState }`、`RangeValue: { Min, Max, Value }`、`Window: { VisualState, InteractionState }` のうち取得できたもの |

子要素サマリは含まれません (構造は `snapshot` で取得します)。

### PNG 出力 (`screenshot`)

| 項目 | 内容 |
| --- | --- |
| 形式 | PNG 固定。`--out` を指定する場合は拡張子 `.png` 必須 (異なれば CLI 段階で `INVALID_ARGUMENT` exit 2) |
| 既定保存先 | `.adact/screenshot-<sid>-<UTC ts>.png` |
| クリップ | `--ref` 指定時は要素の bounding rect、未指定はアタッチ済みウィンドウ全体 |
| 出力 | stdout に `{ "path", "width", "height" }` JSON 1 行。`path` は CWD からの相対パス |

## MCP tool error

MCP tool の業務エラーは JSON-RPC error ではなく tool result として返します。

| フィールド | 内容 |
| --- | --- |
| `isError` | `true` |
| `content[0].text` | `<CODE>: <message>` |
| `structuredContent.code` | error code |
| `structuredContent.message` | message |
| `structuredContent.details` | optional details |

CLI client は `isError: true` を受けると stderr の `error` / `message` / `hint` 形式に変換し、通常は exit code `1` を返します。CLI 入力段階で検出できる不正は daemon に投げず exit code `2` になります。

## 代表エラーコード

| Code | 層 | 典型原因 | 典型 exit |
| --- | --- | --- | ---: |
| `INVALID_ARGUMENT` | CLI / MCP | 引数不足、未知 filter、sessionId 不明 | 2 または 1 |
| `INVALID_REF_FORMAT` | CLI | Element Ref が `s<sid>e<eid>` 形式ではない | 2 |
| `INVALID_WINDOW_REF` | MCP | `w<n>` が unknown / retired | 1 |
| `WINDOW_NOT_FOUND` | MCP | `windowRef` 解決後の HWND attach が失敗 | 1 |
| `REF_NOT_FOUND` | MCP | Element Ref が malformed、session 不一致、現 snapshot にない | 1 |
| `ELEMENT_INTERACTION_FAILED` | MCP | click/fill 等の UIA 操作が失敗 | 1 |
| `SNAPSHOT_FAILED` | MCP | snapshot 構築失敗 | 1 |
| `NO_ACTIVE_SESSION` | MCP | active session がない | 1 |
| `NOT_FOUND` | MCP | lifecycle / wait-for 等の対象 session がない | 1 |
| `CLOSE_FAILED` | MCP | window close 失敗 | 1 |
| `KILL_FAILED` | MCP | process kill 失敗 | 1 |
| `LAUNCH_FAILED` | Engine→MCP→CLI | `launch` が失敗 (実行ファイル不在、Win32Exception、UWP COM 失敗等) | 1 |
| `WAIT_TIMEOUT` | Engine→MCP→CLI | `wait-for` / `wait-for-window` が timeout | 1 |
| `CONNECTION_FAILED` | CLI | HTTP daemon に接続できない | 3 |
| `LOCAL_ONLY` | CLI / MCP | remote target で `daemon-stop`、または stdio mode で `daemon_stop` | 2 または 1 |
| `OPERATION_BLOCKED` | Engine→MCP→CLI | デスクトップがロック / UAC / ウィンドウ非アクティブなどで操作がブロックされた | 1 |
| `NO_INTERACTIVE_SESSION` | daemon 起動 | `serve` / `local` が非対話 desktop で起動された | 4 |
| `INTERNAL_ERROR` | CLI / MCP | 予期しない内部失敗 | 1 |

## よくある対応

| エラー | 対応 |
| --- | --- |
| `CONNECTION_FAILED` | `adact serve` が起動しているか、`--server` / `.adact/config.json` が `/mcp` を指しているか確認する |
| `NO_INTERACTIVE_SESSION` | 対象 GUI が動く対話ログオン session 側で `adact serve` または `adact local` を起動する |
| `REF_NOT_FOUND` | `adact snapshot` を再取得し、新しい `[ref=...]` を使う |
| `INVALID_WINDOW_REF` | `adact list-apps` で `w<n>` を取り直して `adact attach <w<n>>` を使う |
| `WINDOW_NOT_FOUND` | `windowRef` に対応する window が表示されているか確認し、必要なら `list-apps` を再実行する |
| `OPERATION_BLOCKED` | 画面ロックを解除する、UAC プロンプトを閉じる、対象ウィンドウがアクティブで表示されていることを確認する |
| `WAIT_TIMEOUT` | `--timeout` を伸ばす、待機条件 (`--state` や検索条件) を見直す、対象 UI が想定通り遷移するか確認する |
| `LAUNCH_FAILED` | 実行ファイルパスを確認する。PATH が通っているか、Win32 / .NET は権限と実行ビットが揃っているか、UWP は `shell:AppsFolder\<AUMID>` の AUMID が正しいかを確認する |

## 参照

| 文書 | 内容 |
| --- | --- |
| [cli.md](cli.md) | CLI コマンドごとの出力 |
| [mcp-tools.md](mcp-tools.md) | MCP error の構造 |
| [../development/troubleshooting.md](../development/troubleshooting.md) | 復旧手順 |
| [../../discussion/018_対話セッション判定.md](../../discussion/018_対話セッション判定.md) | exit 4 / `NO_INTERACTIVE_SESSION` の設計 |
