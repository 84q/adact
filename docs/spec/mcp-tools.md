# MCP Tools 仕様

ADACT の MCP tools は `src/Adact.Mcp.Common/WindowsTools.cs` および `WindowsTools.{Mouse,Keyboard,Toggle,Window,Inspect,Wait,Launch,Screenshot}.cs` (partial class) に集約され、HTTP daemon (`adact serve`) から使われます。現行の主利用経路は CLI client ですが、MCP client から直接呼ぶこともできます。

CLI はこれらの MCP レスポンスをそのまま表示せず、`discussion/042_CLI出力形式統一設計.md` に従って yaml風 / TSV風 / snapshot 形式へ再整形します。

## Tool 一覧

| カテゴリ | Tool | 役割 | 主な引数 | 主な戻り値 |
| --- | --- | --- | --- | --- |
| Discovery | `windows_list_apps` | 現在の desktop の top-level window を列挙する | なし | `windows[]`。各要素に `windowRef`, `sessionId?`, `processName`, `processId`, `className?`, `windowTitle` |
| Session | `windows_attach` | 1 つの window に attach する | `windowRef` (required) | `sessionId`, `windowRef`, `windowInfo` |
| Session | `windows_snapshot` | attached window の raw UIA snapshot を返す | `sessionId?` | raw JSON (`_meta`, `tree`) |
| Mouse | `windows_click` | Element Ref の要素を click する | `ref`, `button?`, `count?`, `modifiers[]?`, `positionX?`, `positionY?` | 成功時 content 空 |
| Mouse | `windows_dblclick` | Element Ref の要素をダブルクリックする | `ref`, `button?`, `modifiers[]?`, `positionX?`, `positionY?` | 成功時 content 空 |
| Mouse | `windows_hover` | Element Ref の要素へ hover する | `ref`, `modifiers[]?`, `positionX?`, `positionY?` | 成功時 content 空 |
| Mouse | `windows_mouse_move` | element ref または `x,y` 座標へカーソルを移動する | `target` | 成功時 content 空 |
| Mouse | `windows_mouse_down` / `windows_mouse_up` | element ref または `x,y` でボタンを press / release | `target`, `button?` | 成功時 content 空 |
| Mouse | `windows_mouse_wheel` | element ref または `x,y` でスクロールする | `target`, `deltaX?`, `deltaY?` | 成功時 content 空 |
| Keyboard | `windows_fill` | Element Ref の要素に値を入力する | `ref`, `value` | 成功時 content 空 |
| Keyboard | `windows_type` | Element Ref の要素にテキストを 1 文字ずつ送出する | `ref`, `text`, `delayMs?` | 成功時 content 空 |
| Keyboard | `windows_press` | キーコンボを送出する | `key`, `ref?` | 成功時 content 空 |
| Keyboard | `windows_key_down` / `windows_key_up` | 単キーを press / release | `key` | 成功時 content 空 |
| Toggle | `windows_check` / `windows_uncheck` | チェック/トグル要素を On/Off にする | `ref` | 成功時 content 空 |
| Toggle | `windows_select` | リスト/コンボボックスの項目を選択する | `ref`, `name?` / `index?` / `itemRef?` のいずれか | 成功時 content 空 |
| Toggle | `windows_focus` | キーボードフォーカスを移す | `ref` | 成功時 content 空 |
| Toggle | `windows_clear` | 入力要素の値を空にする | `ref` | 成功時 content 空 |
| Toggle | `windows_scroll_into_view` | 要素が見える位置までスクロールさせる | `ref` | 成功時 content 空 |
| Window | `windows_resize` | アタッチ済みウィンドウのサイズを変更 | `width`, `height`, `sessionId?` | 成功時 content 空 |
| Window | `windows_minimize` / `windows_maximize` / `windows_restore` | アタッチ済みウィンドウの状態変更 | `sessionId?` | 成功時 content 空 |
| Inspect | `windows_inspect` | UIA プロパティ詳細を返す | `ref` | inspect JSON (詳細は後述) |
| Inspect | `windows_screenshot` | PNG を保存する | `ref?`, `out?`, `sessionId?` | `{ sessionId, path, width, height }` |
| Wait | `windows_wait_for` | 要素の state を待つ | `ref?` または `name?`/`controlType?`/`automationId?`/`className?`、`state?`, `timeoutMs?`, `sessionId?` | `{ sessionId, ref, state }` |
| Wait | `windows_wait_for_window` | top-level window の出現を待つ (attach なし) | `title?`, `className?`, `processName?`, `executable?`, `timeoutMs?` | `{ processId, processName, windowTitle, controlType, className, nativeWindowHandle }` |
| Lifecycle | `windows_launch` | Win32 / .NET / UWP プロセスを起動する (attach なし) | `executable`, `args?`, `cwd?`, `env?` | `{ pid, processName, executablePath }` |
| Lifecycle | `windows_detach` | session record を解放する | `sessionId?` | `sessionId`, `detached: true` |
| Lifecycle | `windows_close` | attached window を閉じ、session を解放する | `sessionId?` | `sessionId`, `closed: true`, `detached: true` |
| Lifecycle | `windows_kill` | attached window の process を強制終了し、session を解放する | `sessionId?` | `sessionId`, `killed: true`, `detached: true` |
| Lifecycle | `windows_close_all` | 全 attached window を close する | なし | `results[]`。session ごとに `ok` / `fail` |
| Daemon | `daemon_stop` | HTTP daemon を停止する | なし | `stopped: true` |

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
| 入力 | `windowRef` (required、`list-apps` で得た `w<n>`) |
| 処理 | `WindowRefStore.TryResolve` で HWND を確定し、`UiaEngine.AttachByHandleAsync` で attach する |
| 戻り値 | `sessionId` (`s<n>`), `windowRef` (`w<n>`), `windowInfo: { processName, windowTitle, processId }` |
| 代表エラー | `INVALID_ARGUMENT`, `INVALID_WINDOW_REF`, `WINDOW_NOT_FOUND` |

同じ window に既に session が紐づいている場合は idempotent に既存 session を返します。

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
| `windows_click` | `ref`, `button?`, `count?`, `modifiers[]?`, `positionX?`, `positionY?` | ref prefix から session を解決し、対象要素を click。`count` は汎用 N 連打であり OS のダブルクリック判定は保証しない (必要時は `windows_dblclick`) | `INVALID_ARGUMENT`, `INVALID_REF_FORMAT`, `REF_NOT_FOUND`, `ELEMENT_INTERACTION_FAILED` |
| `windows_fill` | `ref`, `value` | ref prefix から session を解決し、対象要素へ value を入力 | `INVALID_ARGUMENT`, `INVALID_REF_FORMAT`, `REF_NOT_FOUND`, `ELEMENT_INTERACTION_FAILED` |

両 tool とも sessionId 引数は取りません。`s<sid>e<eid>` の `sid` から `SessionStore` が session を解決します。

### Mouse / Keyboard / Toggle / Window tools (Phase 8)

| Tool | 入力 | 処理概要 |
| --- | --- | --- |
| `windows_dblclick` / `windows_hover` | `ref`, `modifiers[]?`, `positionX?`, `positionY?` (`dblclick` のみ `button?`) | element を解決して dblclick / hover |
| `windows_mouse_move` / `windows_mouse_down` / `windows_mouse_up` | `target` (element ref `s<sid>e<eid>` または `x,y` 文字列)、`button?` | `MouseTarget.Parse` で ByRef / ByPoint を分岐し低レベルマウス操作を実行 |
| `windows_mouse_wheel` | `target`, `deltaX?`, `deltaY?` (Playwright/DOM 流: 正値=下/右、負値=上/左) | 指定座標または要素中央でホイール操作 |
| `windows_type` | `ref`, `text`, `delayMs?` | フォーカス後にテキストを 1 文字ずつ送出する |
| `windows_press` | `key`, `ref?` | `KeyParser` で組合せキー (`Ctrl+Shift+E` 等) を解析し送出。`ref` 指定時は事前にフォーカス |
| `windows_key_down` / `windows_key_up` | `key` | 単キーを press / release。修飾キーの保持にも使う |
| `windows_check` / `windows_uncheck` | `ref` | TogglePattern または SelectionItemPattern で On/Off |
| `windows_select` | `ref`, `name?` / `index?` / `itemRef?` | リスト要素を `name` exact match、0-based index、または直接 `itemRef` で選択。必要に応じて ExpandCollapse を Expand |
| `windows_focus` / `windows_clear` / `windows_scroll_into_view` | `ref` | フォーカス、ValuePattern による空文字代入、ScrollItemPattern によるスクロール |
| `windows_resize` | `width`, `height`, `sessionId?` | TransformPattern または Win32 でウィンドウサイズを変更 |
| `windows_minimize` / `windows_maximize` / `windows_restore` | `sessionId?` | WindowPattern.SetWindowVisualState を呼ぶ |

代表エラー: `INVALID_ARGUMENT`, `INVALID_REF_FORMAT`, `REF_NOT_FOUND`, `ELEMENT_INTERACTION_FAILED`, `NO_ACTIVE_SESSION`, `NOT_FOUND`。

### `windows_inspect`

| 項目 | 内容 |
| --- | --- |
| 入力 | `ref` |
| 処理 | session を解決し、現 snapshot 内の要素から UIA プロパティと対応 Pattern の状態を読む |
| 戻り値 | `{ ref, name, controlType, automationId, className, helpText, value, boundingRect{x,y,width,height}, isEnabled, isOffscreen, isKeyboardFocusable, hasKeyboardFocus, patterns: { Toggle, SelectionItem, ExpandCollapse, RangeValue, Window } }` |
| 代表エラー | `INVALID_ARGUMENT`, `INVALID_REF_FORMAT`, `REF_NOT_FOUND` |

子要素のサマリは含めません (snapshot に任せます)。

### `windows_screenshot`

| 項目 | 内容 |
| --- | --- |
| 入力 | `ref?` (省略でウィンドウ全体)、`out?` (PNG パス、省略で `.adact/screenshot-<sid>-<UTC ts>.png`)、`sessionId?` |
| 処理 | `ref` 指定時は要素の bounding rect でクリップして PNG 保存。形式は PNG のみ |
| 戻り値 | `{ sessionId, path, width, height }` |
| 代表エラー | `INVALID_ARGUMENT` (拡張子が `.png` 以外、ref/sessionId 組合せ不正、未知 session / ref を含む)、`NO_ACTIVE_SESSION` |

### `windows_wait_for`

| 項目 | 内容 |
| --- | --- |
| 入力 | `ref?` または検索条件 `name?`/`controlType?`/`automationId?`/`className?` (排他必須、すべて case-insensitive exact match)、`state?` (`attached`/`detached`/`visible`/`hidden`/`enabled`/`disabled`、default `visible`)、`timeoutMs?` (default 5000)、`sessionId?` (検索条件モード時のみ。ref モードでは ref から自動解決) |
| 処理 | ref モードでは `WaitForRefAsync`、検索条件モードでは `WaitForQueryAsync` をポーリング呼び出し。`detached` state は ref モード専用 |
| 戻り値 | `{ sessionId, ref, state }` |
| 代表エラー | `INVALID_ARGUMENT`, `REF_NOT_FOUND`, `NO_ACTIVE_SESSION`, `NOT_FOUND`, `WAIT_TIMEOUT` |

### `windows_wait_for_window`

| 項目 | 内容 |
| --- | --- |
| 入力 | `title?`, `className?`, `processName?`, `executable?` のいずれか必須 (case-insensitive 正規表現)、`timeoutMs?` (default 5000) |
| 処理 | `UiaEngine.WaitForWindowAsync` が条件を満たす top-level window をポーリング検出する。attach は行わない |
| 戻り値 | `{ processId, processName, windowTitle, controlType, className, nativeWindowHandle }` |
| 代表エラー | `INVALID_ARGUMENT`, `WAIT_TIMEOUT` |

### `windows_launch`

| 項目 | 内容 |
| --- | --- |
| 入力 | `executable` (実行ファイルパス / PATH 名 / `shell:AppsFolder\<AUMID>`)、`args?`, `cwd?`, `env?` |
| 処理 | UWP モード (`shell:AppsFolder\` で始まる入力) は `IApplicationActivationManager.ActivateApplication` で起動。それ以外は `Process.Start` (`UseShellExecute=false`)。UWP モードで `cwd` / `env` 指定時は `INVALID_ARGUMENT` |
| 戻り値 | `{ pid, processName, executablePath }` (UWP では `executablePath` は AUMID または null) |
| 代表エラー | `INVALID_ARGUMENT`, `LAUNCH_FAILED` |

attach は行いません。起動した window に対して操作するには `windows_wait_for_window` で出現を待ち、`windows_list_apps` -> `windows_attach` の手順で attach します。

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
| `INVALID_REF_FORMAT` | element ref が `s<sid>e<eid>` 形式でない |
| `INVALID_WINDOW_REF` | `w<n>` が未登録または retired |
| `WINDOW_NOT_FOUND` | `windowRef` 解決後の HWND attach が失敗した |
| `REF_NOT_FOUND` | Element Ref が malformed、session 不一致、現 snapshot に存在しない |
| `ELEMENT_INTERACTION_FAILED` | click/fill 等の UIA 操作が失敗した |
| `SNAPSHOT_FAILED` | snapshot 構築に失敗した |
| `NO_ACTIVE_SESSION` | active session がないのに sessionId を省略した |
| `NOT_FOUND` | 指定 sessionId が見つからない (lifecycle / wait-for 等) |
| `CLOSE_FAILED` | close が失敗した |
| `KILL_FAILED` | kill が失敗した |
| `LAUNCH_FAILED` | `windows_launch` が失敗した (実行ファイル不在、Win32Exception、UWP COM 失敗等) |
| `WAIT_TIMEOUT` | `windows_wait_for` / `windows_wait_for_window` がタイムアウトした |
| `LOCAL_ONLY` | HTTP daemon 専用操作を remote target で実行した |
| `INTERNAL_ERROR` | daemon stop 等の内部失敗 |

## 参照

| 文書 | 内容 |
| --- | --- |
| [cli.md](cli.md) | CLI からの利用方法 |
| [ref-ids.md](ref-ids.md) | ref / session の形式 |
| [errors-and-output.md](errors-and-output.md) | MCP error と CLI error の対応 |

## 2026-05 CLI 出力統一補足

CLI は本書の MCP レスポンスをそのまま表示せず、`discussion/042_CLI出力形式統一設計.md` に従って yaml風 / TSV風 / snapshot 形式へ再整形する。
