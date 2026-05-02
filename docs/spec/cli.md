# CLI 仕様

ADACT の主インターフェースは `adact <subcommand>` CLI です。CLI は短命プロセスとして起動し、既定では `http://127.0.0.1:41300/mcp` の HTTP MCP daemon (`adact serve`) に接続します。

## サブコマンド一覧

| カテゴリ | コマンド | 役割 |
| --- | --- | --- |
| Runtime | `serve` | HTTP MCP daemon を起動する |
| Runtime | `local` | stdio MCP server を起動する |
| Discovery | `list-apps` | top-level window 一覧を TSV で出力する |
| Session | `attach` | window に attach し、`sessionId` と `windowRef` を得る |
| Snapshot | `snapshot` | active session または指定 session の snapshot を `.txt` ファイルに保存する |
| Mouse | `click` | Element Ref の要素をクリックする (button / count / modifier / position) |
| Mouse | `dblclick` | Element Ref の要素をダブルクリックする |
| Mouse | `hover` | Element Ref の要素へ hover する |
| Mouse | `mouse-move` | element ref または `x,y` 座標へカーソルを移動する |
| Mouse | `mouse-down` | element ref または `x,y` でマウスボタンを押下する |
| Mouse | `mouse-up` | element ref または `x,y` でマウスボタンを離す |
| Mouse | `mouse-wheel` | element ref または `x,y` でマウスホイールをスクロールする |
| Keyboard | `type` | Element Ref の要素にテキストを 1 文字ずつ送出する |
| Keyboard | `fill` | Element Ref の要素へ文字列を上書き入力する |
| Keyboard | `press` | キーコンボ (例 `Ctrl+Shift+E`) を送出する |
| Keyboard | `key-down` | 単一キーを押下する |
| Keyboard | `key-up` | 単一キーを離す |
| Toggle | `check` / `uncheck` | チェック/トグル要素を On/Off にする |
| Toggle | `select` | リスト/コンボボックスの項目を選択する (`--name` / `--index` / `--item-ref`) |
| Toggle | `focus` | Element Ref にキーボードフォーカスを移す |
| Toggle | `clear` | 入力要素の値を空にする |
| Toggle | `scroll-into-view` | 要素が見える位置までスクロールさせる |
| Window | `resize` | アタッチ済みウィンドウのサイズを変更する |
| Window | `minimize` / `maximize` / `restore` | アタッチ済みウィンドウの状態を変更する |
| Inspect | `inspect` | Element Ref の UIA プロパティ詳細を JSON 1 行で出力する |
| Inspect | `screenshot` | アタッチ済みウィンドウまたは要素を PNG として保存する |
| Wait | `wait-for` | Element Ref または検索条件にマッチする要素が指定 state を満たすまで待機する |
| Wait | `wait-for-window` | 検索条件にマッチする top-level window の出現を待つ (attach は行わない) |
| Lifecycle | `launch` | Win32 / .NET / UWP プロセスを起動する (attach は行わない) |
| Lifecycle | `detach` | session record を解放する。対象 window は閉じない |
| Lifecycle | `close` | attached window を閉じ、session を解放する |
| Lifecycle | `kill` | attached window の process を強制終了し、session を解放する |
| Lifecycle | `close-all` | すべての attached window を閉じる |
| Lifecycle | `daemon-stop` | localhost の HTTP daemon を停止する |
| Install | `install --skills` | AI coding client 向け Skill ファイルを展開する |

Phase 8 で追加された Mouse / Keyboard / Toggle / Window カテゴリのうち、状態変化を伴うものは既定で操作後 snapshot を自動取得します。Wait / Inspect カテゴリは取得・同期系のため auto-snapshot は発火しません。詳細は [snapshot.md](snapshot.md) と [#auto-snapshot 対象](#auto-snapshot-対象) を参照してください。

## Runtime commands

| コマンド | 主な引数 | stdout | stderr |
| --- | --- | --- | --- |
| `adact serve` | `--port <0-65535>` | 通常使わない | daemon ログ、起動時の対話セッション判定 |
| `adact local` | `--verbose` | MCP JSON-RPC 専用 | ログ、起動時エラー |

`serve` と `local` は対話 desktop 内で起動する必要があります。非対話 session では `NO_INTERACTIVE_SESSION`、exit code `4` で起動を拒否します。

## Client commands

| コマンド | 主な引数・フラグ | 成功時出力 |
| --- | --- | --- |
| `list-apps` | `--server <url>` | TSV header + rows |
| `attach` | `<w<n>>`、`--no-snapshot`、`--snapshot-dir`、`--server` | `sessionId`, `windowRef`, 必要なら `snapshot` |
| `snapshot` | `--sid <s<n>>`、`--filter operable|raw`、`--snapshot-dir`、`--server` | `sessionId`, `snapshot` |
| `click` | `<s<sid>e<eid>>`、`--button left|right|middle` (default `left`)、`--count <N>` (default `1`、汎用 N 連打。OS のダブルクリック判定は保証しない)、`--modifier <key>` (繰り返し可: `Shift` / `Control` (alias `Ctrl`) / `Alt` / `Meta` / `ControlOrMeta`、case-insensitive)、`--position x,y` (要素相対、default 中央)、`--no-snapshot`、`--snapshot-dir`、`--server` | 既定で `sessionId`, `snapshot`。`--no-snapshot` 時は `sessionId` のみ |
| `dblclick` | `--ref`, `--button`, `--modifier`, `--position`、auto-snapshot 系オプション | `sessionId`, `snapshot` |
| `hover` | `--ref`, `--modifier`, `--position`、auto-snapshot 系オプション | `sessionId`, `snapshot` |
| `mouse-move` | target (`s1e2` または `x,y`)、`--server` | (出力なし) |
| `mouse-down` / `mouse-up` | target、`--button`、`--server` | (出力なし) |
| `mouse-wheel` | target、`--delta-x <n>`、`--delta-y <n>` (Playwright/DOM 流: 正値=下/右、負値=上/左)、auto-snapshot 系オプション | `sessionId`, `snapshot` |
| `type` | `--ref`、text、`--delay-ms <ms>`、auto-snapshot 系オプション | `sessionId`, `snapshot` |
| `fill` | `<s<sid>e<eid>> <text>`、`--no-snapshot`、`--snapshot-dir`、`--server` | 既定で `sessionId`, `snapshot`。`--no-snapshot` 時は `sessionId` のみ |
| `press` | key (`"Ctrl+Shift+E"` 等の組合せ 1 文字列)、`--ref` (任意)、auto-snapshot 系オプション | `sessionId`, `snapshot` |
| `key-down` / `key-up` | key (単キー)、`--server` | (出力なし) |
| `check` / `uncheck` | `--ref`、auto-snapshot 系オプション | `sessionId`, `snapshot` |
| `select` | `--ref`、`--name <text>` / `--index <n>` / `--item-ref <ref>` のいずれか必須、auto-snapshot 系オプション | `sessionId`, `snapshot` |
| `focus` | `--ref`、`--server` | (出力なし) |
| `clear` | `--ref`、auto-snapshot 系オプション | `sessionId`, `snapshot` |
| `scroll-into-view` | `--ref`、`--server` | (出力なし) |
| `resize` | `--width <w>`、`--height <h>`、auto-snapshot 系オプション | `sessionId`, `snapshot` |
| `minimize` / `maximize` / `restore` | `--sid` (任意)、auto-snapshot 系オプション | `sessionId`, `snapshot` |
| `inspect` | `<s<sid>e<eid>>`、`--server` | UIA プロパティの JSON を 1 行で stdout 出力 |
| `screenshot` | `--ref` (任意。未指定はウィンドウ全体)、`--out <path>` (PNG 必須、default `.adact/screenshot-<sid>-<UTC ts>.png`)、`--sid`、`--server` | `{ "path": ..., "width": ..., "height": ... }` JSON 1 行 |
| `wait-for` | `--ref` または検索条件 (`--name` / `--control-type` / `--automation-id` / `--class-name`、case-insensitive exact match) のいずれか必須、`--state attached|detached|visible|hidden|enabled|disabled` (default `visible`、`detached` は ref モード専用)、`--timeout <ms>` (default 5000)、`--sid` (検索条件モード時のみ)、`--server` | `{ "ref": "s1e7", "state": "..." }` JSON 1 行 |
| `wait-for-window` | `--title` / `--class-name` / `--process-name` / `--exe` のいずれか必須 (case-insensitive 正規表現)、`--timeout <ms>` (default 5000)、`--server` | マッチ window の info JSON 1 行 (`processId`, `processName`, `windowTitle`, `controlType`, `className`, `nativeWindowHandle`)。attach は行わない |
| `launch` | `<executable>` (実行ファイルパス / PATH 名 / `shell:AppsFolder\<AUMID>`)、`-- <arg>...` (任意)、`--cwd <dir>`、`--env KEY=VALUE` (繰り返し可)、`--server` | `{ "pid": ..., "processName": ..., "executablePath": ... }` JSON 1 行。attach は行わない |
| `detach` | `[<s<n>>]`、`--server` | `sessionId`, `detached` |
| `close` | `[--sid <s<n>>]`、`--server` | `sessionId`, `closed`, `detached` |
| `kill` | `[--sid <s<n>>]`、`--server` | `sessionId`, `killed`, `detached` |
| `close-all` | `--server` | TSV rows: `sessionId`, `result`, optional `error` |
| `daemon-stop` | `--server` | `stopped` |

`attach` は positional `windowRef` (`list-apps` で得た `w<n>`) を必須とします。属性マッチングオプションは提供しません。

## 接続先解決

Client commands の接続先は次の優先順位で決まります。

| 優先度 | 入力 | 例 |
| ---: | --- | --- |
| 1 | `--server` | `adact list-apps --server http://127.0.0.1:41300/mcp` |
| 2 | `.adact/config.json` の `server` | `{ "server": "http://127.0.0.1:41300/mcp" }` |
| 3 | 既定値 | `http://127.0.0.1:41300/mcp` |

`.adact/config.json` は current directory から親 directory に向かって探索されます。`.adact/` が見つかった時点で探索を止め、`config.json` がなければ既定値へ fallback します。

## 出力形式概要

| 種類 | 使うコマンド | 形式 |
| --- | --- | --- |
| key-value | `attach`, `snapshot`, `click`, `fill`, `dblclick`, `hover`, `type`, `press`, `check`, `uncheck`, `select`, `clear`, `mouse-wheel`, `resize`, `minimize`, `maximize`, `restore`, `detach`, `close`, `kill` | `key value` を 1 行ずつ stdout に出す |
| literal line | `daemon-stop`、lifecycle の結果語 | `stopped`、`closed` など |
| TSV | `list-apps`, `close-all` | tab 区切り。`list-apps` は header あり、`close-all` は header なし |
| snapshot file | `attach`, `snapshot`, `click`, `fill`, および auto-snapshot 対象の Phase 8 操作系コマンド | `.txt` ファイル path を `snapshot <path>` として出す |
| JSON 1 行 | `inspect`, `screenshot`, `wait-for`, `wait-for-window`, `launch` | `Console.WriteLine(JsonSerializer.Serialize(...))` で 1 行出力 |
| 出力なし | `mouse-move`, `mouse-down`, `mouse-up`, `key-down`, `key-up`, `focus`, `scroll-into-view` | 成功時は stdout 無出力 (exit 0) |
| error | 全 CLI | stderr に `error` / `message` / optional `hint` |

stdout は機械可読な成功データ専用、stderr はエラーとログ用です。

## auto-snapshot 対象

`--no-snapshot` を持ち、成功時に CLI が自動 snapshot を取得するコマンド:

`attach`, `click`, `fill`, `dblclick`, `hover`, `type`, `press`, `check`, `uncheck`, `select`, `clear`, `mouse-wheel`, `resize`, `minimize`, `maximize`, `restore`

低レベル補助 (`mouse-move`, `mouse-down`, `mouse-up`, `key-down`, `key-up`, `focus`, `scroll-into-view`) と取得・同期系 (`inspect`, `screenshot`, `wait-for`, `wait-for-window`, `launch`) は auto-snapshot を発火しません。詳細は [snapshot.md](snapshot.md) を参照してください。

## `install --skills`

```powershell
adact install --skills <copilot|claude|codex> [--global]
```

| 項目 | 内容 |
| --- | --- |
| 目的 | AI coding client が ADACT CLI を見つけ、Phase 8 までで追加された全サブコマンド (`list-apps` / `attach` / `snapshot` / 操作系 / 取得系 / wait 系 / lifecycle 系) を正しい順序で使えるようにする |
| 入力 | `--skills copilot|claude|codex` は必須。`--global` は user-global install |
| 出力 | `installed adact-cli to <path>` |
| 対象 Skill | `src/Adact.Cli/Skills/adact-cli/` の `SKILL.md` と `references/*.md` |

現行 Skill の reference 対象は Phase 8 までで追加された全 30 サブコマンドです。CLI/MCP サブコマンドを追加・改名・削除した場合は Skill と同期テスト (`tests/Adact.Cli.Tests/Unit/InstallCommandTests.cs` の `ExpectedDocumentedCommands`) も更新します。

## 参照

| 文書 | 内容 |
| --- | --- |
| [errors-and-output.md](errors-and-output.md) | exit code と stderr/stdout の詳細 |
| [snapshot.md](snapshot.md) | snapshot file 形式 |
| [ref-ids.md](ref-ids.md) | `w<n>` / `s<n>` / `s<sid>e<eid>` |
| [../../discussion/014_Phase6_完了.md](../../discussion/014_Phase6_完了.md) | Skill 機構の完了記録 |
