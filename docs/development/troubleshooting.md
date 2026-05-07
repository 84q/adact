# Troubleshooting

ADACT は Windows UIA と daemon process の状態に依存します。問題が起きたら、まず `adact serve` が対象 GUI と同じ対話 session で動いているか、CLI が正しい `/mcp` endpoint に接続しているかを確認します。

## `NO_INTERACTIVE_SESSION`

| 症状 | 内容 |
| --- | --- |
| exit code | `4` |
| stderr | `error NO_INTERACTIVE_SESSION` |
| 発生箇所 | `adact serve` 起動時 |
| 原因 | SSH、Windows service、SessionId 0 など、対話 desktop ではない場所で daemon を起動した |

復旧:

1. 対象 GUI アプリが表示されている Windows ログオン session に入る。
2. その session の terminal から `adact serve` を起動する。
3. SSH 側や別 terminal の CLI は、その daemon の URL を `--server` または `.adact/config.json` で指定する。

UIA は同一 Windows session 内の top-level window しか見えません。`list-windows` が空に見える事故を避けるため、ADACT は daemon 起動時に fail-fast します。

## `OPERATION_BLOCKED`

| 症状 | 内容 |
| --- | --- |
| exit code | 通常 `1` |
| stderr | `error OPERATION_BLOCKED` |
| 典型原因 | 画面ロック中、UAC プロンプト表示中、対象ウィンドウが非アクティブまたは最小化されている |

復旧:

1. 画面がロックされている場合は解除する。
2. UAC プロンプトが表示されている場合は許可または拒否して閉じる。
3. 対象ウィンドウがアクティブで表示されていることを確認する。

## daemon 接続失敗

| 症状 | 内容 |
| --- | --- |
| exit code | `3` |
| stderr | `error CONNECTION_FAILED` |
| 典型原因 | `adact serve` が起動していない、port が違う、URL が `/mcp` ではない、firewall / port forward の問題 |

確認:

```powershell
adact serve --port 41300
adact list-windows --server http://127.0.0.1:41300/mcp
```

`.adact/config.json` を使う場合:

```json
{ "server": "http://127.0.0.1:41300/mcp" }
```

接続先解決は `--server`、`.adact/config.json`、既定値の順です。`.adact/` が見つかると親 directory への探索はそこで止まります。

## `REF_NOT_FOUND`

| 症状 | 内容 |
| --- | --- |
| exit code | 通常 `1` |
| 発生箇所 | `click` / `fill`、または MCP `adact_click` / `adact_fill` |
| 典型原因 | ref の typo、別 session の ref、対象 element が最新 snapshot から消えた、daemon 再起動で状態が消えた |

復旧:

1. `adact snapshot` を再実行する。
2. 新しい `.txt` snapshot の `[ref=s...e...]` を使う。
3. session 自体がない場合は `adact list-windows` -> `adact attach ...` からやり直す。

現行 ref は `s<sid>e<eid>` です。古い `s<sid>g<gen>e<eid>` は過去形式です。

## `INVALID_WINDOW_REF` / `WINDOW_NOT_FOUND`

| Code | 原因 | 対処 |
| --- | --- | --- |
| `INVALID_WINDOW_REF` | `w<n>` が未登録もしくは retired (window が閉じた / `list-windows` 後にずれた) | `adact list-windows` を再取得して新しい `w<n>` を渡す |
| `WINDOW_NOT_FOUND` | `w<n>` 解決後の HWND attach が失敗した (window が閉じられた等) | `adact list-windows` を再取得し、対象 window が存在することを確認する |

例:

```powershell
adact list-windows
adact attach w1
```

`attach` は `w<n>` 形式の positional 引数のみ受け付けます。process name / title 等での matching は提供しません (`list-windows` で絞り込んでから `w<n>` を渡してください)。

## snapshot が大きい

| 状況 | 対処 |
| --- | --- |
| AI に渡す snapshot を小さくしたい | 既定の `--filter operable` を使う |
| デバッグで全 tree を見たい | `adact snapshot --filter raw` を使う |
| 保存先を分けたい | `--snapshot-dir <dir>` を使う |
| click/fill 後の snapshot が不要 | `--no-snapshot` を使う |

現行 CLI snapshot は `.txt` 形式で、旧 JSON 出力より小さくなっています。それでも大きい場合、対象 window が大きすぎる、UIA tree が深すぎる、または `raw` を使っている可能性があります。

## 必要な要素が snapshot に見えない

| 可能性 | 対処 |
| --- | --- |
| `operable` filter で落ちている | `adact snapshot --filter raw` で確認する |
| element が offscreen | window を表示・展開・スクロールしてから再 snapshot する |
| modal dialog に focus が移っている | snapshot 内の `[modal]` node を確認する |
| UIA が情報を出していない | アプリ側の UIA 対応状況を確認する。必要なら将来の OCR / Vision 対象 |
| 古い snapshot を読んでいる | stdout の `snapshot <path>` で最新 file path を確認する |

## `daemon-stop` が `LOCAL_ONLY`

| 症状 | 原因 | 対処 |
| --- | --- | --- |
| `error LOCAL_ONLY` | remote host の daemon を止めようとした、または stdio mode で `adact_daemon_stop` を呼んだ | daemon と同じ host の CLI から `adact daemon-stop` を実行する |

`daemon-stop` は安全のため localhost target 専用です。

## 実アプリテストが不安定

| 症状 | 対処 |
| --- | --- |
| Calculator E2E が競合する | 他の test run が同時に Calculator を触っていないか確認する |
| click/fill が時々失敗する | 実行中に同じ desktop を人間が操作しない |
| Notepad++ smoke が skip / fail する | Notepad++ のインストール、window title、権限を確認する |
| `list-windows` が空 | daemon が対話 session で動いているか確認する |

## 参照

| 文書 | 内容 |
| --- | --- |
| [../architecture/runtime-modes.md](../architecture/runtime-modes.md) | runtime mode と対話 session 制約 |
| [../spec/errors-and-output.md](../spec/errors-and-output.md) | error code 一覧 |
| [../spec/ref-ids.md](../spec/ref-ids.md) | ref の失効条件 |
| [../spec/snapshot.md](../spec/snapshot.md) | snapshot filter と形式 |
