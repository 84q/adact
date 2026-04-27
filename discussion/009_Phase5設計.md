# 009 Phase 5 設計 — CLI クライアント実装

[008 要件再整理](008_要件再整理.md) で確定した新ビジョン (`AI → ADACT CLI → MCP daemon → UIA → App`) に基づき、Phase 5 の具体的な設計を確定する。

参考: [001 要件定義](001_要件定義.md) / [003 実装計画](003_実装計画.md) / [008 要件再整理](008_要件再整理.md)

## 0. 設計フェーズの位置づけ

Phase 5 は **CLI クライアント層** の実装フェーズ。Phase 4 までで実装済の HTTP MCP daemon (`adact serve`) は変更を最小限にとどめ、その上に Coding AI / 人間が直接叩ける CLI コマンド群を被せる。

実装は別フェーズ (Implementation サブエージェント委譲) で行う。本書は実装に必要な仕様・構造の確定が目的。実装コードは含めない。

## 1. 全体アーキテクチャ

```
[Coding AI / Human]
         │
         │ shell exec (subcommand + flags)
         ▼
   ┌─────────────────────────────┐
   │ adact.exe (Adact.Cli)      │  CLI client
   │  Commands/                  │
   │  McpClient/                 │
   │  Output/                    │
   └─────────────────────────────┘
         │
         │ HTTP MCP (Streamable HTTP)
         ▼
   ┌─────────────────────────────┐
   │ adact serve                 │  HTTP MCP daemon
   │  (Adact.Cli.Server)         │  ※ 同じ adact.exe の subcommand
   │  WindowsTools / SessionStore│
   │  WindowRefStore (★ 新規)    │
   └─────────────────────────────┘
         │
         │ FlaUI / UIA
         ▼
     [Windows GUI App]
```

主要な変更点 (Phase 4 から):

- **Adact.Cli プロジェクトの分割**: client 部と server 部を分離 ([§9](#9-プロジェクト構成))
- **新規 subcommand 10 個**: `list-apps` / `attach` / `snapshot` / `click` / `fill` / `detach` / `close` / `kill` / `close-all` / `daemon-stop`
- **WindowRefStore の追加**: daemon 側に top-level window の Ref ID 管理を追加 ([§7](#7-window-ref-id-仕様))
- **CLI フレームワーク導入**: System.CommandLine ([§8](#8-cli-フレームワーク--プロジェクト構成))

## 2. 通信仕様

### 2.1 プロトコル

- **HTTP MCP (Streamable HTTP)** を採用 (stdio MCP は既存の `local` subcommand で並存)
- ModelContextProtocol.AspNetCore 1.2.0 (server) / ModelContextProtocol HttpClientTransport (client)
- Stateless = true (Phase 4 から踏襲、session 状態は singleton DI で保持)

### 2.2 エンドポイント

- 既定 URL: `http://127.0.0.1:41300/mcp`
- サーバ側: `app.MapMcp("/mcp")` に変更 (Phase 4 までは `/` 直マップ)
- `.vscode/mcp.json` の `adact-http` の URL も `/mcp` に追従

### 2.3 認証 / TLS

- Phase 5 では **不採用** (no auth / no TLS / no CORS)
- ローカルおよびトラステッドなネットワーク内利用が前提
- 将来必要になった段階で Phase 7 以降で検討

## 3. 接続先指定方式

### 3.1 解決優先度

```
(高) --server <url> フラグ
        ↓ なければ
     <cwd> から親ディレクトリへ再帰探索した最初の .adact/config.json の "server" フィールド
        ↓ なければ
     http://127.0.0.1:41300/mcp (既定)
```

環境変数による指定は **不採用**。

### 3.2 URL 形式

- フル URL のみ受け付ける (補完なし)
- 例: `http://192.168.1.10:41300/mcp`
- ホスト省略形 (`192.168.1.10` のみ等) は不正としてエラー

### 3.3 設定ファイル仕様

- ファイル名: `.adact/config.json` (`config.json` 採用)
- 配置: workspace root 配下 `.adact/` ディレクトリ
- 探索: cwd から親へ再帰 (git 流)。最初に見つかった `.adact/` で停止
- 不存在時: 静かに既定値にフォールバック (stderr ログなし)
- Phase 5 で扱うフィールドは `"server"` のみ
- フォーマット: JSON (.NET 標準 `System.Text.Json`、外部依存ゼロ)

例:
```json
{
  "server": "http://192.168.1.10:41300/mcp"
}
```

### 3.4 localhost 判定 (`daemon-stop` ガード用)

- URL の host 部 (大文字小文字無視) が次のいずれかなら localhost 扱い:
  - `127.0.0.1`
  - `::1`
  - `localhost`
- DNS 解決ベースではなく文字列ベース (なりすまし回避)

## 4. CLI コマンド一覧

### 4.1 サブコマンド一覧

| カテゴリ | コマンド | 役割 |
| --- | --- | --- |
| daemon 管理 | `adact serve` | HTTP MCP daemon を起動 (既存) |
| daemon 管理 | `adact local` | stdio MCP モードで起動 (既存、維持) |
| daemon 管理 | `adact daemon-stop` | daemon を正常終了 (ローカル限定) |
| 列挙 | `adact list-apps` | top-level window 一覧を取得 |
| アタッチ | `adact attach <ref-or-flags>` | window を session として確保 |
| 観測 | `adact snapshot` | 現在の active session の UI ツリーを取得 |
| 操作 | `adact click <ref>` | UI 要素にクリック |
| 操作 | `adact fill <ref> <text>` | UI 要素にテキスト入力 |
| クリーンアップ | `adact detach` | session を解除 (window は無傷) |
| クリーンアップ | `adact close` | window を Close → 自動 detach |
| クリーンアップ | `adact kill` | プロセスを Kill → 自動 detach |
| クリーンアップ | `adact close-all` | 全 session を close |

### 4.2 共通フラグ

| フラグ | 適用範囲 | 用途 |
| --- | --- | --- |
| `--server <url>` | 全コマンド (`serve`/`local` 除く) | 接続先 URL |
| `--no-snapshot` | snapshot を返すコマンド (`attach` / `click` / `fill` / `snapshot`) | snapshot ファイル生成抑制 |
| `--snapshot-dir <path>` | 同上 | snapshot 保存先 (既定 `.adact/`) |
| `--sid <n>` | `snapshot` / `detach` / `close` / `kill` | 対象 session 指定 (省略時 active session) |

### 4.3 attach コマンドの引数仕様

ポジショナル引数 (Window Ref) とフラグ指定 (詳細クエリ) の両対応。

- ポジショナル: `adact attach w3` (`list-apps` で得た windowRef)
- フラグ指定: 以下を任意組合せで指定 (AND 条件)
  - `--process-name <name>` 例: `calc.exe`
  - `--title <substring>` 例: `"電卓"`
  - `--process-id <pid>` 例: `1234`
  - `--class-name <className>` 例: `ApplicationFrameWindow`

ポジショナル引数が `^w\d+$` 形式に一致すれば Window Ref として解釈、それ以外の文字列は不正として `INVALID_ARGUMENT`。

### 4.4 snapshot 自動同梱

- `attach` / `click` / `fill` の **成功時**、結果に snapshot path を含める
- `--no-snapshot` 指定時は snapshot を取得・保存しない
- snapshot 自体はファイル (既定 `.adact/session-<sid>-gen-<gen>-<timestamp>.yml`) に書き出し、stdout には **path のみ** を出力 (コンテキストウィンドウ節約)
- `detach` / `close` / `kill` / `close-all` は snapshot 同梱なし

### 4.5 lifecycle セマンティクス

| コマンド | 操作対象 | 挙動 | 失敗時 | リモート可 |
| --- | --- | --- | --- | --- |
| `detach` | session のみ | session レコード削除 / Ref 無効化。window/process 無傷 | (ほぼ失敗しない) | ✅ |
| `close` | window | UIA `WindowPattern.Close()` / WM_CLOSE → 成功時のみ自動 detach | session 残存 | ✅ |
| `kill` | プロセス | `Process.Kill()` (TerminateProcess) → 成功時のみ自動 detach | session 残存 | ✅ |
| `close-all` | 全 attach 中 window | 各 session に `close` を実行 → 成功分のみ detach | 各 session の結果を出力、1 つでも失敗で exit 1 | ✅ |
| `daemon-stop` | daemon プロセス | (1) 全 session detach → (2) HTTP listener 停止 → (3) exit 0 | — | ❌ ローカル接続時のみ |

`close-all` の出力例:
```
sessionId	result	error
s1	ok
s2	ok
s3	fail	CLOSE_TIMEOUT
```

## 5. 出力形式

### 5.1 全体方針

- 既定形式: **シンプル key-value (改行区切り、空白 1 個区切り)**
- 表形式: **TSV (タブ区切り) + ヘッダ行あり**
- Markdown / JSON 切替は Phase 5 では不採用 (Phase 7+ で `--format` フラグ追加検討)

### 5.2 各コマンドの stdout

`attach` / `click` / `fill` / `snapshot` (成功時):
```
sessionId s1
generation 1
snapshot .adact/session-1-gen-1-20251108T120000.yml
```

`list-apps`:
```
windowRef	sessionId	processName	processId	className	windowTitle
w1	s1	calc.exe	1234	ApplicationFrameWindow	電卓
w2	-	notepad.exe	5678	Notepad	無題 - メモ帳
w3	-	chrome.exe	9999	Chrome_WidgetWin_1	GitHub - Chrome
```

attach 済み window は `sessionId` 列が埋まり、未 attach は `-`。

`detach`:
```
sessionId s1
detached
```

`close`:
```
sessionId s1
closed
detached
```

`kill`:
```
sessionId s1
killed
detached
```

`close-all`: [§4.5](#45-lifecycle-セマンティクス) 参照。

`daemon-stop`:
```
stopped
```

### 5.3 ヘッダ行と列順

- `list-apps` の TSV にはヘッダ行を出力 (self-documenting)
- 列順は固定: `windowRef` / `sessionId` / `processName` / `processId` / `className` / `windowTitle`
- 将来の列追加時はヘッダで判別可能

### 5.4 sessionId / windowRef / Ref ID の表記

- Window Ref: `w<n>` (例: `w1`, `w2`)
- Session ID: `s<n>` (例: `s1`, `s2`)
- Element Ref: `s<sid>g<gen>e<eid>` (例: `s1g1e7`)

すべて前置文字付きで Ref ID 種別を識別可能。

## 6. エラー出力規約

### 6.1 exit code

| code | 意味 | 例 |
| --- | --- | --- |
| 0 | 成功 | 全コマンド正常終了 |
| 1 | コマンド実行失敗 (daemon は応答した) | TIMEOUT / NOT_FOUND / CLICK_FAILED / CLOSE_FAILED / STALE_REF |
| 2 | ユーザエラー (CLI 段階で検出) | INVALID_ARGUMENT / INVALID_REF_FORMAT / 不正な URL / 設定 parse error / LOCAL_ONLY |
| 3 | 接続失敗 | daemon 不在 / TCP/HTTP エラー |

### 6.2 stderr フォーマット

成功時は stderr に何も出さない。エラー時のみ key-value 形式:

```
error <CODE>
message <human-readable text>
hint <suggestion>     # 任意
```

例:
```
error CONNECTION_FAILED
message could not connect to http://127.0.0.1:41300/mcp
hint ensure 'adact serve' is running
```

### 6.3 エラーコード一覧

既存 [`Adact.Mcp.Common/ToolErrors`](../src/Adact.Mcp.Common) と整合させる。CLI 専用コードを追加。

| コード | exit | 用途 |
| --- | --- | --- |
| `INVALID_ARGUMENT` | 2 | 引数フォーマット不正 |
| `INVALID_REF_FORMAT` | 2 | Ref ID 書式不正 (`w\d+` / `s\d+` / `s\d+g\d+e\d+` のいずれにもマッチしない) |
| `INVALID_WINDOW_REF` | 1 | Window Ref が引退済み or 該当 window が消失 |
| `NOT_FOUND` | 1 | window / session が見つからない |
| `STALE_REF` | 1 | 古い generation の Element Ref ID |
| `NO_ACTIVE_SESSION` | 1 | active session なし |
| `AMBIGUOUS_ATTACH` | 1 | 複数候補ヒット |
| `CLICK_FAILED` | 1 | クリック失敗 |
| `FILL_FAILED` | 1 | 入力失敗 |
| `CLOSE_FAILED` | 1 | window Close 失敗 |
| `KILL_FAILED` | 1 | プロセス Kill 失敗 |
| `TIMEOUT` | 1 | 操作タイムアウト |
| `SNAPSHOT_FAILED` | 1 | snapshot 取得失敗 |
| `CONNECTION_FAILED` | 3 | daemon 接続不可 (CLI 側で検出) |
| `LOCAL_ONLY` | 2 | `daemon-stop` を非ローカル接続で実行 |
| `INTERNAL_ERROR` | 1 | 想定外例外 |

### 6.4 部分失敗の扱い (`close-all`)

- 各 session の結果を stdout に明細表示
- 1 つでも失敗があれば exit 1
- stderr にサマリは出さない (stdout に既に含まれているため)

### 6.5 `--verbose` フラグ

Phase 5 では不採用。Phase 7 以降で daemon 通信ログ等の出力を検討。

## 7. Window Ref ID 仕様

### 7.1 概要

`list-apps` で取得した top-level window に短い Ref ID (`w1`, `w2`, ...) を割り当てる。AI / 人間がこの ID を `attach` の引数に使えるようにする。

### 7.2 発行・管理場所

daemon 側に **WindowRefStore** (singleton) を新設。CLI 側では状態を持たない。

- WindowRefStore は MCP ツール `windows_list_apps` および `windows_attach` の入力経路に組み込まれる
- ストレージ: in-memory (daemon 再起動で消える)

### 7.3 一意性判定 (WindowKey)

HWND 単独では理論上再利用される可能性があるため、3 点組で window を一意化:

```
WindowKey = (HWND, processId, processStartTime)
```

`list-apps` 実行時のロジック:
1. 現在のすべての top-level window を列挙
2. 各 window で WindowKey を構築
3. WindowRefStore から WindowKey で検索
   - ヒット: 既存の `windowRef` を返す
   - 未ヒット: 新しい `windowRef` (単調増加カウンタ) を採番して登録
4. 列挙されなかった WindowKey は引退マーク (以降 `INVALID_WINDOW_REF`)
5. 採番カウンタは引退時に減らさない (再利用なし)

### 7.4 採番の単調増加性

| 状態 | windowRef |
| --- | --- |
| 起動直後 list-apps | `w1` (calc), `w2` (notepad), `w3` (chrome) |
| notepad 閉鎖、再 list-apps | `w1` (calc), `w3` (chrome) ← w2 は欠番 |
| 別の notepad 起動、再 list-apps | `w1` (calc), `w3` (chrome), `w4` (notepad) ← w2 は再利用しない |

### 7.5 ライフタイム

- daemon 起動中は永続 (TTL なし)
- `list-apps` を呼ぶ度に HWND の生存確認、消失していれば store から引退マーク
- 引退済み windowRef に対する `attach` は `INVALID_WINDOW_REF`

### 7.6 attach 後の windowRef

- attach 後も windowRef は維持 (sessionId と並存)
- `list-apps` は両 ID を表示 (`windowRef` 列 + `sessionId` 列)
- 同じ windowRef に対する 2 度目の `attach` は **idempotent**: 既存 sessionId を返す (新採番しない)
- session detach 後は windowRef のみ残る (再 attach 可能)

### 7.7 MCP ツール側の変更

| ツール | 変更内容 |
| --- | --- |
| `windows_list_apps` | レスポンスに `windowRef` / `sessionId` フィールドを追加。WindowRefStore を経由して採番 |
| `windows_attach` | 入力に `windowRef` フィールドを追加。既存の `processName` / `title` / `processId` / `className` と排他または併用可 |

### 7.8 Element Ref ID (既存)

Element Ref ID (`s<sid>g<gen>e<eid>`) の仕様は [001 §9 Phase 3](001_要件定義.md) のまま変更なし。CLI 側で形式チェック (`^s\d+g\d+e\d+$`) を行い、不正なら `INVALID_REF_FORMAT` (exit 2)。daemon 側で有効性チェック (generation 一致等)。STALE_REF 時の自動再 snapshot は **行わない** (ユーザ / AI 判断)。

## 8. CLI フレームワーク + プロジェクト構成

### 8.1 CLI フレームワーク

**System.CommandLine** (Microsoft 公式、2.0.0-beta) を採用。

- .NET 10 ターゲットと整合
- サブコマンド + フラグ + ヘルプ自動生成
- 型安全な引数バインディング

懸念: API 変更リスク (preview 段階)。.NET 10 GA 時期に正式版リリース見込みのため、Phase 5 実装期間中の API 変更は許容範囲とみなす。

### 8.2 プロジェクト構成 (P2: 関心分離)

```
src/
  Adact.Cli/                       # CLI クライアント (adact.exe のエントリ)
    Program.cs                     # System.CommandLine 構成
    Commands/                      # 各サブコマンドハンドラ
      ListAppsCommand.cs
      AttachCommand.cs
      SnapshotCommand.cs
      ClickCommand.cs
      FillCommand.cs
      DetachCommand.cs
      CloseCommand.cs
      KillCommand.cs
      CloseAllCommand.cs
      DaemonStopCommand.cs
      ServeCommand.cs              # → Adact.Cli.Server を呼ぶ
      LocalCommand.cs              # → Adact.Mcp.Stdio を呼ぶ
    McpClient/                     # HTTP MCP クライアント
      AdactMcpClient.cs
      ConnectionResolver.cs        # --server / config.json / 既定
      ConfigLoader.cs
    Output/                        # 出力ヘルパ
      KeyValueWriter.cs
      TsvWriter.cs
      ErrorWriter.cs
    SDK: Microsoft.NET.Sdk (Web 不要)
    deps: System.CommandLine, ModelContextProtocol (client),
          Adact.Cli.Server (project ref), Adact.Mcp.Stdio, Adact.Mcp.Common

  Adact.Cli.Server/                # HTTP MCP サーバ (class library)
    HttpHost.cs                    # 既存の HttpHost を移動
    SDK: Microsoft.NET.Sdk.Web
    deps: ModelContextProtocol.AspNetCore, Adact.Mcp.Common

  Adact.Mcp.Common/                # 既存
    WindowsTools.cs
    SessionStore.cs
    WindowRefStore.cs              # ★ Phase 5 新規
    ToolErrors.cs

  Adact.Mcp.Stdio/                 # 既存 (変更なし)
  Adact.Engine/                    # 既存 (変更なし)
```

### 8.3 配布物

- 単一の `adact.exe` (Adact.Cli のビルド出力) のみ
- Adact.Cli.Server は class library として Adact.Cli から参照 → 同 exe に同梱
- `serve` subcommand 実行時のみ ASP.NET Core ランタイムを起動

## 9. テスト計画

### 9.1 テストプロジェクト構成

| プロジェクト | 役割 | 主な対象 |
| --- | --- | --- |
| `Adact.Cli.Tests` (新規) | CLI クライアント L1/L2 テスト | Commands / McpClient / Output / ConnectionResolver / ConfigLoader |
| `Adact.Mcp.Common.Tests` (新規 or 既存活用) | WindowRefStore L1 単体 | WindowRefStore 採番ロジック、WindowKey 一意性 |
| `Adact.Mcp.Http.Tests` (既存) | MCP HTTP サーバ L2 結合 | `windows_list_apps` の windowRef レスポンス、`windows_attach` の windowRef 受付 |
| `Adact.Engine.Tests` (既存) | UIA L3 / L4 | 変更なし |

### 9.2 E2E (L5) シナリオ追加

- `list-apps` → `attach <windowRef>` → `snapshot` → `click <ref>` → `close` の通しフロー (電卓)
- `daemon-stop` のローカル限定エラー
- `close-all` の部分失敗時の exit 1 確認
- `--server` フラグと `.adact/config.json` の優先度確認

### 9.3 既存テストへの影響

- `windows_list_apps` のレスポンス追加フィールド (`windowRef`, `sessionId`) によるテスト更新
- `app.MapMcp("/mcp")` への変更に伴う既存 HTTP テストの URL 更新
- `.vscode/mcp.json` の URL 更新 (テスト対象外だが docs 整合)

## 10. 主要実装タスク (Implementation 委譲時の構造)

実装は Implementation サブエージェントに委譲する。本書で確定した仕様に基づき、以下のタスクを順次進める想定:

1. **プロジェクト構成変更**: `Adact.Cli` → `Adact.Cli` + `Adact.Cli.Server` 分離
2. **System.CommandLine 導入**: Program.cs を Root Command + Commands/ 構成に書き換え
3. **WindowRefStore 実装**: `Adact.Mcp.Common/WindowRefStore.cs` 追加、WindowKey 一意性ロジック
4. **MCP ツール拡張**: `windows_list_apps` レスポンス + `windows_attach` 入力に `windowRef` 追加
5. **HTTP MCP クライアント実装**: `McpClient/AdactMcpClient.cs` (HttpClientTransport 経由)
6. **接続先解決**: `ConnectionResolver` + `ConfigLoader` (.adact/config.json 親再帰探索)
7. **各 subcommand ハンドラ実装**: `Commands/*.cs` (10 個)
8. **出力ヘルパ実装**: `Output/KeyValueWriter` / `TsvWriter` / `ErrorWriter`
9. **`MapMcp("/mcp")` 変更**: HttpHost.cs + .vscode/mcp.json 更新
10. **テスト追加**: Adact.Cli.Tests / Adact.Mcp.Common.Tests / E2E シナリオ
11. **ドキュメント更新**: 001 / 003 / README に Phase 5 完了記録、CLI ヘルプテキスト

## 11. 既存ドキュメントへの反映

- [001 要件定義](001_要件定義.md): §8 成功の定義 / §9 設計決定事項に Phase 5 完了基準を追記 (実装後)
- [003 実装計画](003_実装計画.md): §6 Phase 5 セクションに本書へのリンクを追加 (実装後)
- [008 要件再整理](008_要件再整理.md): §5 Phase 5 設計フェーズ申し送り → 本書で確定済として参照可能に

## 12. 未決事項

Phase 5 実装中または Phase 5 完了後の振り返りで再判断する事項:

| 項目 | 内容 | 判断時期 |
| --- | --- | --- |
| `--format` 切替 | Markdown / JSON 出力モードの追加 | Phase 7 安定化 |
| `.adact/config.json` のフィールド拡充 | `defaultSnapshotDir` / `outputFormat` 等 | Phase 7+ |
| 環境変数サポート | AI ツールでの永続接続先指定の利便性次第で再検討 | Phase 7+ |
| `stop` / `kill-all` daemon 管理コマンド | 現状除外、運用上の必要性が出た時点で追加 | Phase 7+ |
| 自動 spawn | CLI が daemon を自動起動する機能 | Phase 9+ |
| 認証 / TLS | リモート運用の本格化時に検討 | Phase 7+ |
| STALE_REF 自動再 snapshot | UI 状態の安全性次第 | Phase 7+ |
| 自前 stdio MCP モード (`local`) の去就 | HTTP 一本化が定着すれば削除候補 | Phase 7+ |

## 13. 設計フェーズの完了基準

本書で以下が確定済み:

- [x] 通信プロトコル (HTTP MCP Streamable HTTP)
- [x] daemon 起動方式 (手動、in-process なし、自動 spawn は将来)
- [x] daemon-session 関係 (1:N)
- [x] snapshot 自動同梱方針 (file 保存 + path のみ stdout)
- [x] lifecycle コマンド体系 (detach / close / kill / close-all / daemon-stop)
- [x] 接続先指定方式 (フラグ + config.json + 既定値)
- [x] 認証 / TLS (Phase 5 では不採用)
- [x] CLI フレームワーク (System.CommandLine)
- [x] コマンド体系全体 (フラット動詞ベース、attach は windowRef + フラグ両対応)
- [x] Window Ref ID 仕様 (WindowKey 一意性、単調増加採番、attach 後並存、idempotent)
- [x] 出力形式 (key-value + TSV + ヘッダあり)
- [x] エラー出力規約 (exit 0/1/2/3、stderr key-value、ToolErrors 整合)
- [x] Ref ID 有効性チェック (CLI で形式、daemon で有効性、自動再試行なし)
- [x] プロジェクト構成 (Adact.Cli + Adact.Cli.Server 分離)

実装は Implementation サブエージェントに委譲する。
