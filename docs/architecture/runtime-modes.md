# Runtime Modes

現行の `adact.exe` / `src/Adact.Cli` は Windows target の単一実行ファイルで、CLI client、HTTP MCP daemon、stdio MCP server の 3 つのモードを持ちます。

## モード比較

| モード | コマンド | 目的 | 典型利用者 |
| --- | --- | --- | --- |
| CLI client | `adact <subcommand>` | HTTP daemon に接続して window 操作を実行する主経路 | AI エージェント、人間 |
| HTTP daemon | `adact serve [--port <port>]` | `/mcp` で MCP tools を公開し、UIA 操作と session/ref 状態を保持する | CLI client、HTTP MCP client |
| stdio local | `adact local [--verbose]` | stdio MCP server として直接 MCP client に接続する互換経路 | Claude Desktop 等の MCP client |

## Windows target と将来境界

現行の単一 `adact.exe` は Windows GUI / UIA 操作を含むため Windows target です。`serve` と `local` は UIA を直接使う runtime なので、対象 GUI と同じ対話 Windows セッション側で動く必要があります。

将来の cross-platform 候補では、GUI を直接操作しない CLI client 部分を Windows UIA 実体から分離し、multi-target 化して macOS / Linux の remote terminal でも起動できるようにします。その場合も、GUI を読む `adact serve` 相当の daemon は Windows GUI セッション側で動く境界を維持します。

## `adact <subcommand>`

CLI client は短命プロセスです。各コマンド実行時に接続先を解決し、HTTP MCP daemon に接続して 1 つの操作を行います。

| 項目 | 内容 |
| --- | --- |
| stdin | 通常は使わない |
| stdout | 成功時の機械可読な結果。key-value、TSV、snapshot path など |
| stderr | `error` / `message` / `hint` 形式のエラー、接続失敗、CLI 段階の入力エラー |
| 状態保持 | CLI 自身は保持しない。session/ref は daemon メモリ内に保持される |
| 接続先 | `--server`、`.adact/config.json`、既定 `http://127.0.0.1:41300/mcp` の順に解決 |

## `adact serve`

`adact serve` は HTTP MCP daemon です。現行実装では 127.0.0.1 の指定 port に bind し、MCP endpoint は `/mcp` です。

| 項目 | 内容 |
| --- | --- |
| stdin | 通常は使わない |
| stdout | データ出力には使わない |
| stderr | daemon ログ、起動時の対話セッション判定結果、起動失敗時エラー |
| 状態保持 | `SessionStore` と `WindowRefStore` が process memory に保持 |
| UIA 要件 | 対象 GUI と同じ対話 Windows セッションで動く必要がある |

`serve` は実際に UIA で GUI を読む側です。そのため SSH、サービス、非対話 session から起動すると GUI window が見えません。起動時に `WinSta0` と `SessionId` を確認し、対話 desktop でなければ listener 起動前に失敗します。

## `adact local`

`adact local` は stdio MCP server です。MCP client が process を spawn し、stdin/stdout で JSON-RPC を流す用途です。

| 項目 | 内容 |
| --- | --- |
| stdin | MCP JSON-RPC 入力専用 |
| stdout | MCP JSON-RPC 出力専用。ログや人間向け文は出さない |
| stderr | ログ、起動時エラー、`NO_INTERACTIVE_SESSION` など |
| 状態保持 | process memory 内。process 終了で消える |
| 位置づけ | CLI 主経路ではなく MCP 互換経路 |

## 対話セッション制約

`serve` と `local` は UIA を直接使うため、起動時に対話 desktop 判定を行います。

| 条件 | 結果 |
| --- | --- |
| `SessionId == 0` | 起動拒否 |
| Window Station が `WinSta0` ではない | 起動拒否 |
| 対話ログオン session 内 | 起動継続 |

失敗時は exit code `4`、stderr は次の形です。

```text
error NO_INTERACTIVE_SESSION
message daemon is not in an interactive desktop session (...)
hint launch 'adact serve' from the interactive logon session that owns the target GUI windows
```

`adact local` の場合も同じ exit code `4` で、hint は `adact local` 向けになります。

## 運用上の要点

| 状況 | 推奨 |
| --- | --- |
| AI / CLI が SSH 側で動く | GUI 側の対話 session で `adact serve` を起動し、SSH 側 CLI は HTTP 接続する |
| daemon を止めたい | 同一 host の CLI から `adact daemon-stop` を実行する |
| MCP client から直接使いたい | `adact local`、または起動済み `adact serve` の `/mcp` を使う |

## 参照

| 文書 | 内容 |
| --- | --- |
| [../../discussion/018_対話セッション判定.md](../../discussion/018_対話セッション判定.md) | 対話セッション判定の設計 |
| [../spec/errors-and-output.md](../spec/errors-and-output.md) | exit code と stderr 規約 |
| [../development/troubleshooting.md](../development/troubleshooting.md) | 代表的な復旧手順 |
