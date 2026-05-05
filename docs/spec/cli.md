# CLI 仕様

ADACT の主インターフェースは `adact <subcommand>` CLI です。CLI は短命プロセスとして起動し、既定では Named Pipe で MCP daemon (`adact serve pipe`) に接続します。

## 出力統一ルール

`serve http` / `serve pipe` を除く通常サブコマンドは、次のどれかの形式で stdout に出力します。

```text
result: true|false
<必要なら追加メタ>
---
<本文>
```

- 成功時は `result: true`
- 失敗時は `result: false` と `error: <ERROR_CODE>`
- 通常の成功/失敗情報はすべて stdout に出し、stderr は原則使わない
- `windowRef` は通常成功出力から廃止し、`list-apps` の TSV 列としてのみ残す
- `sessionId` はメタには置かず、必要なコマンドの本文に出す

## 形式分類

| 形式 | 対象コマンド | 備考 |
| --- | --- | --- |
| TSV | `list-apps`, `close-all` | 先頭メタの後ろに TSV 本文 |
| snapshot | `snapshot` | メタに `snapshotPath`、本文に `sessionId` + 空行 + tree |
| yaml | 上記以外の 1-shot コマンド | 先頭メタの後ろに yaml風本文 |
| 対象外 | `serve http`, `serve pipe` | サーバとして継続実行するため統一結果フォーマットの対象外 |

## コマンド一覧

| カテゴリ | コマンド | 形式 | 概要 |
| --- | --- | --- | --- |
| Runtime | `serve http` | 対象外 | HTTP MCP daemon を起動する |
| Runtime | `serve pipe` | 対象外 | Named Pipe MCP daemon を起動する |
| Discovery | `list-apps` | TSV | top-level window 一覧 |
| Session | `attach` | yaml | window に attach し session を作成 |
| Snapshot | `snapshot` | snapshot | active / 指定 session の snapshot を保存・表示 |
| Mouse | `click`, `dblclick`, `hover` | yaml | UI 操作。auto-snapshot 対象 |
| Mouse | `mouse-move`, `mouse-down`, `mouse-up` | yaml | 低レベルマウス操作 |
| Mouse | `mouse-wheel` | yaml | 低レベルマウス操作 |
| Keyboard | `fill`, `type` | yaml | UI 操作。auto-snapshot 対象 |
| Keyboard | `press` | yaml | 低レベルキー操作 |
| Keyboard | `key-down`, `key-up` | yaml | 低レベルキー操作 |
| Toggle | `check`, `uncheck`, `select`, `clear` | yaml | UI 操作。auto-snapshot 対象 |
| Toggle | `focus`, `scroll-into-view` | yaml | 補助操作 |
| Window | `resize`, `minimize`, `maximize`, `restore` | yaml | window 状態変更。auto-snapshot 対象 |
| Inspect | `inspect` | yaml | UIA プロパティ詳細 |
| Inspect | `screenshot` | yaml | PNG 保存 |
| Wait | `wait-for` | yaml | 要素状態待機 |
| Wait | `wait-for-window` | yaml | top-level window 出現待機 |
| Lifecycle | `launch` | yaml | Win32 / .NET / UWP 起動 |
| Lifecycle | `detach`, `close`, `kill` | yaml | session / window の lifecycle 操作 |
| Lifecycle | `close-all` | TSV | 全 session の close 結果 |
| Lifecycle | `daemon-stop` | yaml | Named Pipe daemon 停止 |
| Install | `install --skills` | yaml | Skill ファイル展開 |

## 代表出力

### `attach`

```text
result: true
snapshotPath: .adact/snapshots/s1/0001.txt (changed)
---
sessionId: s1
processId: 12345
title: Untitled - Notepad
```

### `attach --no-snapshot`

```text
result: true
---
sessionId: s1
processId: 12345
title: Untitled - Notepad
```

### `snapshot`

```text
result: true
snapshotPath: .adact/snapshots/s1/0012.txt (changed)
---
sessionId: s1

- Window "Untitled - Notepad" [ref=s1e1]
  - Edit [ref=s1e2]
```

### `list-apps`

```text
result: true
---
windowRef	sessionId	processName	processId	className	windowTitle
w1	s1	notepad	12345	Notepad	Untitled - Notepad
```

### `close-all` の部分失敗

```text
result: false
---
sessionId	result	error
s1	true	
s2	false	CLOSE_FAILED
```

### 共通失敗

```text
result: false
error: NO_ACTIVE_SESSION
---
message: No active session. Call windows_attach first or specify sessionId explicitly.
```

## auto-snapshot 対象

`--no-snapshot` を持ち、成功時に CLI が snapshot を自動取得するコマンド:

`attach`, `click`, `fill`, `dblclick`, `hover`, `type`, `check`, `uncheck`, `select`, `clear`, `resize`, `minimize`, `maximize`, `restore`

- `--no-snapshot` 時は `snapshotPath` を出さない
- `snapshot` 本文を stdout に出すのは `snapshot` コマンドだけ

## session 指定インターフェース

一部コマンドは session 指定を `--sid` ではなく **任意位置引数 `sid`** で受け取る。

- 対象: `snapshot`, `resize`, `minimize`, `maximize`, `restore`, `close`, `kill`
- 省略時は従来どおり active session を解決する

`screenshot` は任意位置引数 `target` を 1 つ受け取り、自動判別する。

- `s<digits>e<digits>` なら element ref として扱う
- それ以外は session id として扱う
- 未指定時は active session を使う

## Runtime commands

| コマンド | 主な引数 | 備考 |
| --- | --- | --- |
| `adact serve http` | `--port <0-65535>` | 継続実行。通常の統一出力対象外 |
| `adact serve pipe` | (なし) | 継続実行。通常の統一出力対象外 |

`serve http` と `serve pipe` は対話 desktop 内で起動する必要があります。非対話 session では `NO_INTERACTIVE_SESSION`、exit code `4` で起動を拒否します。

## 接続先解決

| 優先度 | 入力 | 例 |
| ---: | --- | --- |
| 1 | `--server` | `adact list-apps --server http://127.0.0.1:41300/mcp` |
| 2 | Named Pipe (既定) | ワークスペースパスから自動生成 |

`--server` 未指定時は Named Pipe に接続します。HTTP モードを使う場合は明示的に `--server` を指定してください。

## `install --skills`

```powershell
adact install --skills <copilot|claude|codex> [--global]
```

| 項目 | 内容 |
| --- | --- |
| 目的 | AI coding client 向け Skill ファイルを展開する |
| 入力 | `--skills copilot|claude|codex` は必須。`--global` は user-global install |
| 出力 | yaml (`installed: true`, `skill`, `path`) |
| 対象 Skill | `src/Adact.Cli/Skills/adact-cli/` の `SKILL.md` と `references/*.md` |

## 参照

| 文書 | 内容 |
| --- | --- |
| [errors-and-output.md](errors-and-output.md) | exit code と CLI/MCP の出力詳細 |
| [snapshot.md](snapshot.md) | snapshot file 形式 |
| [ref-ids.md](ref-ids.md) | `w<n>` / `s<n>` / `s<sid>e<eid>` |
