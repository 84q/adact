# Errors and Output

ADACT は CLI と MCP の両方で、成功データとエラーを明確に分けます。

- CLI は通常の成功/失敗情報を **stdout に統一**します
- MCP の業務エラーは `isError: true` の tool result として返します

## Exit codes

| Code | 名前 | 用途 |
| ---: | --- | --- |
| 0 | Success | 正常終了 |
| 1 | CommandFailed | daemon が応答した tool error、操作失敗、内部失敗 |
| 2 | UserError | CLI 段階の入力エラー、URL/config 不正、remote `daemon-stop` など |
| 3 | ConnectionFailed | daemon への接続失敗 |
| 4 | EnvironmentNotSupported | daemon 起動環境が不適切。現行では `NO_INTERACTIVE_SESSION` |

## CLI 標準出力

### 共通形

```text
result: true|false
<必要なら追加メタ>
---
<本文>
```

### 共通失敗形

```text
result: false
error: <CODE>
---
message: <human-readable message>
hint: <optional recovery hint>
```

- 成功時は `result: true`
- 失敗時は `result: false` と `error: <CODE>`
- `hint` は必要なときだけ出す
- `serve http` / `serve pipe` は継続実行コマンドのため通常の統一結果フォーマットの主対象外

## CLI 成功形式

| 形式 | コマンド | 内容 |
| --- | --- | --- |
| yaml | `attach`, 操作系, lifecycle, `inspect`, `screenshot`, `wait-for-element`, `wait-for-window`, `launch`, `install`, `daemon-stop` | 先頭メタ + `---` + yaml風本文 |
| TSV | `list-windows`, `close-all` | 先頭メタ + `---` + TSV 本文 |
| snapshot | `snapshot` | `snapshotPath` をメタ、本文に `sessionId` + 空行 + tree |

### yaml 例

```text
result: true
snapshotPath: .adact/snapshots/s1/0008.txt (unchanged)
---
action: click
target: s1e42
```

### TSV 例 (`list-windows`)

```text
result: true
---
windowRef	sessionId	processName	processId	className	windowTitle
w1	s1	notepad	12345	Notepad	Untitled - Notepad
```

### snapshot 例 (`snapshot`)

```text
result: true
snapshotPath: .adact/snapshots/s1/0012.txt (changed)
---
sessionId: s1

- Window "Untitled - Notepad" [ref=s1e1]
```

## `close-all` の例外ルール

`close-all` は部分失敗時だけ例外で、`result: false` としつつ本文を TSV のまま維持します。

```text
result: false
---
sessionId	result	error
s1	true	
s2	false	CLOSE_FAILED
```

ただし `windows_close_all` のレスポンス自体が malformed (`results` が無い/配列でない等) の場合は、TSV ではなく `INTERNAL_ERROR` の yaml失敗出力にします。

## MCP tool error

MCP tool の業務エラーは JSON-RPC error ではなく tool result として返します。

| フィールド | 内容 |
| --- | --- |
| `isError` | `true` |
| `content[0].text` | `<CODE>: <message>` |
| `structuredContent.code` | error code |
| `structuredContent.message` | message |
| `structuredContent.details` | optional details |

CLI client は `isError: true` を受けると stdout の yaml風エラー形式に変換し、通常は exit code `1` を返します。CLI 入力段階で検出できる不正は daemon に投げず exit code `2` になります。

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
| `LAUNCH_FAILED` | Engine→MCP→CLI | `launch` が失敗 | 1 |
| `WAIT_TIMEOUT` | Engine→MCP→CLI | `wait-for-element` / `wait-for-window` が timeout | 1 |
| `CONNECTION_FAILED` | CLI | daemon に接続できない | 3 |
| `LOCAL_ONLY` | CLI / MCP | remote target で `daemon-stop` | 2 または 1 |
| `OPERATION_BLOCKED` | Engine→MCP→CLI | デスクトップがロック / UAC 等で操作不能 | 1 |
| `NO_INTERACTIVE_SESSION` | daemon 起動 | `serve` が非対話 desktop で起動された | 4 |
| `INTERNAL_ERROR` | CLI / MCP | 予期しない内部失敗 | 1 |

## 参照

| 文書 | 内容 |
| --- | --- |
| [cli.md](cli.md) | CLI コマンドごとの出力 |
| [mcp-tools.md](mcp-tools.md) | MCP tool の戻り値と error 構造 |
