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
| Action | `click` | Element Ref の要素をクリックする |
| Action | `fill` | Element Ref の要素へ文字列を上書き入力する |
| Lifecycle | `detach` | session record を解放する。対象 window は閉じない |
| Lifecycle | `close` | attached window を閉じ、session を解放する |
| Lifecycle | `kill` | attached window の process を強制終了し、session を解放する |
| Lifecycle | `close-all` | すべての attached window を閉じる |
| Lifecycle | `daemon-stop` | localhost の HTTP daemon を停止する |
| Install | `install --skills` | AI coding client 向け Skill ファイルを展開する |

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
| `click` | `<s<sid>e<eid>>`、`--no-snapshot`、`--snapshot-dir`、`--server` | 既定で `sessionId`, `snapshot`。`--no-snapshot` 時は `sessionId` のみ |
| `fill` | `<s<sid>e<eid>> <text>`、`--no-snapshot`、`--snapshot-dir`、`--server` | 既定で `sessionId`, `snapshot`。`--no-snapshot` 時は `sessionId` のみ |
| `detach` | `[--sid <s<n>>]`、`--server` | `sessionId`, `detached` |
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
| key-value | `attach`, `snapshot`, `click`, `fill`, `detach`, `close`, `kill` | `key value` を 1 行ずつ stdout に出す |
| literal line | `daemon-stop`、lifecycle の結果語 | `stopped`、`closed` など |
| TSV | `list-apps`, `close-all` | tab 区切り。`list-apps` は header あり、`close-all` は header なし |
| snapshot file | `attach`, `snapshot`, `click`, `fill` | `.txt` ファイル path を `snapshot <path>` として出す |
| error | 全 CLI | stderr に `error` / `message` / optional `hint` |

stdout は機械可読な成功データ専用、stderr はエラーとログ用です。

## `install --skills`

```powershell
adact install --skills <copilot|claude|codex> [--global]
```

| 項目 | 内容 |
| --- | --- |
| 目的 | AI coding client が ADACT CLI を見つけ、正しい順序で `list-apps` / `attach` / `snapshot` / `click` / `fill` を使えるようにする |
| 入力 | `--skills copilot|claude|codex` は必須。`--global` は user-global install |
| 出力 | `installed adact-cli to <path>` |
| 対象 Skill | `src/Adact.Cli/Skills/adact-cli/` の `SKILL.md` と `references/*.md` |

現行 Skill の reference 対象は 5 基本サブコマンドです。CLI/MCP サブコマンドを追加・改名・削除した場合は Skill と同期テストも更新します。

## 参照

| 文書 | 内容 |
| --- | --- |
| [errors-and-output.md](errors-and-output.md) | exit code と stderr/stdout の詳細 |
| [snapshot.md](snapshot.md) | snapshot file 形式 |
| [ref-ids.md](ref-ids.md) | `w<n>` / `s<n>` / `s<sid>e<eid>` |
| [../../discussion/014_Phase6_完了.md](../../discussion/014_Phase6_完了.md) | Skill 機構の完了記録 |
