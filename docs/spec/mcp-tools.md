# MCP Tools 仕様

ADACT の MCP tools は `src/Adact.Mcp.Common/WindowsTools.cs` に集約され、HTTP daemon (`adact serve`) と stdio local (`adact local`) の両方から使われます。現行の主利用経路は CLI client ですが、MCP client から直接呼ぶこともできます。

## Tool 一覧

| Tool | 役割 | 主な引数 | 主な戻り値 |
| --- | --- | --- | --- |
| `windows_list_apps` | 現在の desktop の top-level window を列挙する | なし | `windows[]`。各要素に `windowRef`, `sessionId?`, `processName`, `processId`, `className?`, `windowTitle` |
| `windows_attach` | 1 つの window に attach する | `windowRef?`, `processName?`, `windowTitle?`, `className?`, `processId?` | `sessionId`, `windowRef`, `windowInfo` |
| `windows_snapshot` | attached window の raw UIA snapshot を返す | `sessionId?` | raw JSON (`_meta`, `tree`) |
| `windows_click` | Element Ref の要素を click する | `ref` | 成功時 content 空 |
| `windows_fill` | Element Ref の要素に値を入力する | `ref`, `value` | 成功時 content 空 |
| `windows_detach` | session record を解放する | `sessionId?` | `sessionId`, `detached: true` |
| `windows_close` | attached window を閉じ、session を解放する | `sessionId?` | `sessionId`, `closed: true`, `detached: true` |
| `windows_kill` | attached window の process を強制終了し、session を解放する | `sessionId?` | `sessionId`, `killed: true`, `detached: true` |
| `windows_close_all` | 全 attached window を close する | なし | `results[]`。session ごとに `ok` / `fail` |
| `daemon_stop` | HTTP daemon を停止する | なし | `stopped: true` |

## Tool 詳細

### `windows_list_apps`

| 項目 | 内容 |
| --- | --- |
| 入力 | なし |
| 処理 | `UiaEngine.ListWindowsAsync` で可視 top-level window を列挙し、`WindowRefStore` と同期する |
| 戻り値 | structured content `{ "windows": [...] }`。text content は raw array JSON |
| 注意 | list から消えた window は retired 扱いになり、既存 `windowRef` は解決できなくなる |

### `windows_attach`

| 項目 | 内容 |
| --- | --- |
| 入力 | `windowRef`、または `processName` / `windowTitle` / `className` / `processId` の任意組み合わせ |
| matching | 指定された項目は厳密一致。`processName` / `windowTitle` / `className` は case-insensitive |
| 戻り値 | `sessionId` (`s<n>`), `windowRef` (`w<n>`), `windowInfo` |
| 代表エラー | `INVALID_ARGUMENT`, `INVALID_WINDOW_REF`, `WINDOW_NOT_FOUND`, `AMBIGUOUS_ATTACH` |

`windowRef` が指定された場合、他の matching 条件は不要です。既に同じ window に session が紐づいている場合は idempotent に既存 session を返します。

### `windows_snapshot`

| 項目 | 内容 |
| --- | --- |
| 入力 | `sessionId`。省略時は active session |
| 戻り値 | Engine raw JSON。MCP 側では filter や text 化を行わない |
| 代表エラー | `NO_ACTIVE_SESSION`, `INVALID_ARGUMENT`, `SNAPSHOT_FAILED` |

CLI の `adact snapshot` はこの raw JSON を受け取り、CLI 側で `operable` / `raw` filter と text formatting を行います。

### `windows_click` / `windows_fill`

| Tool | 入力 | 処理 | 代表エラー |
| --- | --- | --- | --- |
| `windows_click` | `ref` | ref prefix から session を解決し、対象要素を click | `INVALID_ARGUMENT`, `REF_NOT_FOUND`, `ELEMENT_INTERACTION_FAILED` |
| `windows_fill` | `ref`, `value` | ref prefix から session を解決し、対象要素へ value を入力 | `INVALID_ARGUMENT`, `REF_NOT_FOUND`, `ELEMENT_INTERACTION_FAILED` |

両 tool とも sessionId 引数は取りません。`s<sid>e<eid>` の `sid` から `SessionStore` が session を解決します。

### Lifecycle tools

| Tool | 省略時 session | 対象 window | session 処理 |
| --- | --- | --- | --- |
| `windows_detach` | active session | 何もしない | session record を削除 |
| `windows_close` | active session | UIA `WindowPattern.Close()`、または `WM_CLOSE` fallback | 成功時 session record を削除 |
| `windows_kill` | active session | `Process.Kill(entireProcessTree: true)` | 成功時 session record を削除 |
| `windows_close_all` | すべて | session ごとに close | 成功した session は削除。失敗は result に残す |

`windows_close_all` は一部失敗を tool error として throw せず、`results[]` に `fail` と error code を入れます。

### `daemon_stop`

| 項目 | 内容 |
| --- | --- |
| 対象 | HTTP daemon mode のみ |
| 処理 | 全 session を detach してから daemon stop を要求する |
| stdio local | `LOCAL_ONLY` error を返す |
| CLI 側制約 | `adact daemon-stop` は localhost target 以外では CLI 段階で `LOCAL_ONLY` になる |

## エラー応答

業務エラーや入力エラーは JSON-RPC error ではなく、MCP tool result として返します。

| フィールド | 内容 |
| --- | --- |
| `isError` | `true` |
| text content | `<CODE>: <message>` |
| structured content | `{ "code": "...", "message": "...", "details": ... }` |

transport/protocol/systemic な例外は SDK により JSON-RPC error として扱われます。

## 代表エラーコード

| Code | 典型原因 |
| --- | --- |
| `INVALID_ARGUMENT` | 引数不足、sessionId 不明、形式不正 |
| `INVALID_WINDOW_REF` | `w<n>` が未登録または retired |
| `WINDOW_NOT_FOUND` | matching 条件に一致する window がない |
| `AMBIGUOUS_ATTACH` | matching 条件に複数 window が一致した |
| `REF_NOT_FOUND` | Element Ref が malformed、session 不一致、現 snapshot に存在しない |
| `ELEMENT_INTERACTION_FAILED` | click/fill が UIA 操作として失敗した |
| `SNAPSHOT_FAILED` | snapshot 構築に失敗した |
| `NO_ACTIVE_SESSION` | active session がないのに sessionId を省略した |
| `CLOSE_FAILED` | close が失敗した |
| `KILL_FAILED` | kill が失敗した |
| `LOCAL_ONLY` | HTTP daemon 専用操作を stdio mode や remote target で実行した |
| `INTERNAL_ERROR` | daemon stop 等の内部失敗 |

## 参照

| 文書 | 内容 |
| --- | --- |
| [cli.md](cli.md) | CLI からの利用方法 |
| [ref-ids.md](ref-ids.md) | ref / session の形式 |
| [errors-and-output.md](errors-and-output.md) | MCP error と CLI error の対応 |
