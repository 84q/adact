# Phase 4 設計 — HTTP MCP サーバー化

> 前提: [001\_要件定義.md](001_要件定義.md) / [002\_アーキテクチャ設計.md](002_アーキテクチャ設計.md) / [003\_実装計画.md](003_実装計画.md) / [005\_Phase3\_完了・修正メモ.md](005_Phase3_完了・修正メモ.md)
> 位置付け: Phase 4 着手前の方針書。実装着手時の判断基準とする。具体コード断片は載せない。

***

## 1. 概要

Phase 4 のゴールは、Phase 3 で stdio MCP サーバーとして公開した `Adact.Engine` を、**Streamable HTTP の MCP サーバーとしても**公開できるようにすることである。同一 UIA Engine をトランスポートだけ差し替えて再利用する構造に整理し、`adact.exe serve` で HTTP MCP サーバーを起動できる状態を完成形とする。

運用方針として、Phase 4 / 5 では **localhost 限定運用**を前提とする。リモートマシン越しの利用は Phase 5 (`adact` プロキシ) で stdio↔HTTP ブリッジを介して行うことを基本とし、HTTP サーバー自身を LAN/WAN に直接公開する用途は当面サポートしない。これに伴い、認証・TLS・CORS は Phase 4 では実装しない。

## 2. スコープ

### 含むもの

- `Adact.Mcp.Common` プロジェクトを新規作成し、Phase 3 で `Adact.Mcp.Stdio` に置いた共通コード (`WindowsTools` / `SessionStore` / `ToolErrors`) を抽出する
- `Adact.Cli` に `serve` サブコマンドを追加し、ASP.NET Core (Kestrel) 上で Streamable HTTP MCP サーバーを起動する
- **コマンド体系の破壊的変更**: 現状の `adact --local` フラグを廃止し、`adact local` (stdio) / `adact serve` (HTTP) のサブコマンド体系に統一する。Phase 3 利用者は事実上開発本人のみであり、後方互換は取らず整合性を優先する
- `UiaEngine` 層に同時呼び出しの直列化機構 (`SemaphoreSlim(1, 1)` 共有) を導入する
- 上記の自動テスト (L1 / L4 / L5) を整備する

### 含まないもの (Phase 6 以降)

- `windows_close` MCP ツール (実ウィンドウ Close + Detach)
- `windows_launch` MCP ツール (Win32 exe / UWP AppId 起動)
- `AttachQuery.Hwnd` 拡張 ([005 §6.1](005_Phase3_完了・修正メモ.md))
- 認証・TLS・CORS の実装 (`serve` の認証トークン / TLS 関連オプションも当面用意しない)
- `--listen <addr>:<port>` で 127.0.0.1 以外にバインドする機能 (将来拡張、Phase 4 ではバインド先を `127.0.0.1` 固定とする)
- `--single-client` 起動オプション (同時接続数 1 に絞る運用モード)

## 3. 技術選定 (B)

### 3.1 トランスポート

**Streamable HTTP** を採用する。旧 HTTP+SSE トランスポートは公式 C# SDK (`ModelContextProtocol.AspNetCore`) で `[Obsolete]` 扱いとなっており、新規実装には用いない。

### 3.2 ホスト

**ASP.NET Core (Kestrel) + `ModelContextProtocol.AspNetCore` 1.2.0** を採用する。Apache-2.0 ライセンスで Phase 3 採用の `ModelContextProtocol` SDK と同一系列。stdio 用ホストビルダー (`Host.CreateEmptyApplicationBuilder` ベース) と HTTP 用ホストビルダー (`WebApplication.CreateBuilder` ベース) を `Adact.Cli` 内で起動モードに応じて分岐する。

### 3.3 起動モード

**Stateless モード**を採用する。ADACT は server→client の sampling / elicitation を使わないため、SDK のセッション管理 (Mcp-Session-Id) は不要であり、サーバー側で接続ごとの状態を保持しない構成で十分である。

### 3.4 バインドアドレス

**`http://127.0.0.1:<port>`** に固定する (Phase 4 では IPv4 ループバック決め打ち)。理由は以下の 4 点:

1. ASP.NET Core のデフォルト `0.0.0.0` / `+` (ANY) は LAN/WAN にも晒されるため、設定ミスによる外部公開事故を防ぐ
2. Windows の名前解決では `localhost` が IPv6 ループバック (`::1`) に先に解決される場合があり、IPv4 リテラル固定の方が確実
3. ループバック宛通信は Windows Defender ファイアウォールの確認ダイアログを出さない
4. コードそのものが「外部公開しない」という設計意図を明示する

将来 `--listen <addr>:<port>` フラグでバインドアドレスを上書きできる方向は残すが、Phase 4 では実装しない。

### 3.5 認証 / TLS / CORS

Phase 4 では **いずれも実装しない**。localhost 限定運用方針 (§1) によりリモートからのアクセス経路自体が存在せず、リモート利用は Phase 5 のプロキシ経由とする想定のため、HTTP サーバー単体に認証層・暗号化層を載せる必要はない。

### 3.6 `serve` のオプション体系

`adact serve` のオプションは Phase 4 では **`--port <int>` のみ実装**する。デフォルトポートは **`41300`** とし、ADACT 独自の固定値として扱う (Well-Known ポートや既存ツールと衝突しない値を選定)。

`--listen <addr>:<port>` によるバインド先指定、認証トークン、TLS 関連オプションは Phase 4 では用意しない。バインド先は `127.0.0.1` 固定 (§3.4)、認証・TLS は localhost 限定運用方針 (§1, §3.5) によるため、いずれも追加の起動オプションを必要としない。

### 3.7 ログ出力

stdio / HTTP の **両モードとも stderr に統一**する。ログ設定は `Microsoft.Extensions.Logging` 標準パターン (環境変数 `Logging__LogLevel__Default` 等) で制御し、追加の独自設定機構は用意しない。

stdio モードでは stdout が JSON-RPC 通信専用となるため stderr 出力が必須であり、HTTP モードでも一貫して stderr に揃えることで運用上の混乱を避ける。

### 3.8 MCP サーバー自己情報

MCP プロトコルの `serverInfo.name` / `serverInfo.version` は **両モードとも `adact` で統一**する。バージョンも単一 (アセンブリバージョンに連動) とし、クライアント側でトランスポートの違いを意識せずに済む UX を優先する。

## 4. プロジェクト構成 (C)

### 4.1 `Adact.Mcp.Common` 新規プロジェクト

`Adact.Mcp.Stdio` から以下のファイルを抽出し、新規ライブラリ `Adact.Mcp.Common` (net10.0-windows) に移動する:

- `WindowsTools.cs` (5 ツールの実装)
- `SessionStore.cs` (アクティブ Session 保持)
- `ToolErrors.cs` (エラーコード定数 + 構造化 content 生成)

`Adact.Mcp.Stdio` と (新規) `Adact.Cli` の HTTP 起動経路の双方が `Adact.Mcp.Common` を参照する。トランスポート固有のホスト構築コードはそれぞれの呼び出し元に残す。

### 4.2 `Adact.Cli` への `serve` サブコマンド追加

単一 exe (`adact.exe`) で複数モードを切替える方針を維持する。

| 起動方法 | モード |
| --- | --- |
| `adact.exe local` | stdio MCP サーバー (Phase 3 で実装した内容を引き継ぐ) |
| `adact.exe serve [--port 41300]` | HTTP MCP サーバー (Phase 4) |

Phase 2 で実装されていた `list` / `snapshot` デバッグ用サブコマンドは Phase 4 サブタスク #3 (Adact.Cli 再構成) で削除済み。代替として MCP ツール (`windows_list_apps` / `windows_snapshot`) を利用する。

`Adact.Cli` 内部で stdio (`Host.CreateEmptyApplicationBuilder`) と HTTP (`WebApplication.CreateBuilder`) のホストビルダーを分岐させ、ツール登録は両者とも `Adact.Mcp.Common` を共有する。

Phase 3 で実装した `adact --local` フラグは Phase 4 で削除し、`adact local` サブコマンドへ置き換える (§2 / [003 §5](003_実装計画.md))。`Adact.Cli/Program.cs` の引数解析は実装フェーズで書き換える必要がある。

### 4.3 stdio の扱い

Phase 4 では **stdio / HTTP を併存**させる。Phase 5 で `adact` プロキシ (stdio→HTTP ブリッジ) が完成した後に、stdio MCP サーバー (`Adact.Mcp.Stdio`) を削除して HTTP に一本化する選択肢を改めて評価する。

### 4.4 `Adact.Cli` の SDK 変更

`Adact.Cli` の csproj SDK を現状の `Microsoft.NET.Sdk` から **`Microsoft.NET.Sdk.Web` に変更**する。`adact serve` で ASP.NET Core (Kestrel) を組み込むための必然的な構成変更であり、§4.2 の単一 exe 方針 (1 つの `adact.exe` で stdio + HTTP の両モードを提供) を成立させるために必要である。

stdio モード起動時にも ASP.NET Core ランタイムがロードされる影響はあるが、起動時間・メモリ消費とも実用上問題ないと判断する。

### 4.5 テストプロジェクト構成

Phase 4 で以下のテストプロジェクト構成を取る。

| プロジェクト | レベル | 内容 |
| --- | --- | --- |
| `Adact.Engine.Tests` (既存) | L1 / L2 / L3 / L4 | Engine 単体の責務。`UiaEngine` の `SemaphoreSlim` 直列化テスト (L1) は Engine の責務であるため引き続きここに置く |
| `Adact.Mcp.Stdio.Tests` (既存) | L5 | stdio MCP の E2E |
| `Adact.Mcp.Http.Tests` (新規) | L4 / L5 | HTTP MCP のテスト。L4 は `HttpHost.BuildApplication(0)` を直接 `StartAsync` してエフェメラルポートで実 Kestrel を起動し、SDK の HTTP MCP client (`HttpClientTransport` + `HttpTransportMode.StreamableHttp`) で接続して `windows_list_apps` を呼ぶ。L5 は HTTP 経由で電卓 attach + snapshot |

`Adact.Mcp.Common` (§4.1) の共通ロジックを対象とするテストは適切な既存テストプロジェクトに配置する。Common 専用のテストプロジェクトは Phase 4 では新設せず、必要が生じた時点で別途検討する。

## 5. 同時接続モデルと直列化 (D)

### 5.1 SessionStore は Singleton 1 個

`SessionStore` は **DI コンテナに Singleton として 1 個登録** する (Phase 3 stdio と完全に同じ構成)。Stateless モード (§3.3) を採用するため Mcp-Session-Id は扱わず、`SessionStore` が表現するのは「Engine がどのウィンドウに attach しているか」という ADACT 内部の状態であって、SDK のクライアント識別とは別概念である。

### 5.2 直列化位置: `UiaEngine` 層で `SemaphoreSlim` を共有

`UiaEngine` および `WindowSession` の公開メソッド (`AttachAsync` / `ListWindowsAsync` / `SnapshotAsync` / `ClickAsync` / `FillAsync` 等) で同一の `SemaphoreSlim(1, 1)` を取得することにより、ツール呼び出しを Engine 層で直列化する。

理由: SDK (`ModelContextProtocol.AspNetCore`) はツール呼び出しを直列化しないため、HTTP サーバー化により並列実行が起こり得る。UIA はマシン全体で前面ウィンドウを取り合う性質があり、並列実行されると操作の決定性が損なわれる。これを ADACT 側で防ぐ最も低コストな位置が Engine 層である。stdio (Phase 3) でも同じ gate が機能するため副作用はない。

### 5.3 同時接続ポリシー

**接続自体は許容し、ツール呼び出しのみを直列化** する方針とする (§5.2)。複数のクライアントが同時に接続している状態でも、ツール呼び出しが Engine 層で順序化されるため UIA の競合は発生しない。

将来的に「同タイミングで 2 つ以上の接続を許さず、2 つ目以降は 503 を返す」モードへ切替えるための `--single-client` 起動オプションを残す方向だが、Phase 4 では実装しない。

## 6. 検証 (E)

### 6.1 自動テスト

| レベル | 内容 |
| --- | --- |
| L1 | `UiaEngine` の `SemaphoreSlim` gate が並列呼び出しを直列化することを mock element ベースで検証 |
| L4 (Smoke) | `HttpHost.BuildApplication(0)` を直接 `StartAsync` してエフェメラルポートで実 Kestrel を起動し、SDK の HTTP MCP client (`HttpClientTransport` + `HttpTransportMode.StreamableHttp`) で接続して `windows_list_apps` を 1 回呼ぶ |
| L5 (E2E) | HTTP 経由で電卓を `windows_attach` + `windows_snapshot` する (Phase 3 stdio E2E の HTTP 版) |

直列化検証は L1 のみで十分とし、L4 / L5 で並列実行を組まない。HTTP 経由で複数同時呼び出しを観測するテストは flaky になりやすく、ROI が見合わないため不採用。

### 6.2 手動疎通

**VS Code Copilot Chat (内蔵 MCP クライアント)** で stdio + HTTP の両方を確認する。これは [005 §6.3](005_Phase3_完了・修正メモ.md) で Phase 3 完了条件として残った「実 MCP クライアントからの手動疎通」と統合される。

運用メモ:

- `.vscode/mcp.json` に `adact-stdio` (`dotnet run --no-build -- local`) と `adact-http` (`http://127.0.0.1:41300/` への接続) の 2 つを定義済み。
- HTTP モードはユーザーが事前に `adact serve --port 41300` を別途起動しておく前提。stdio モードは VS Code が必要時に子プロセスを起動する。

## 7. Phase 4 への申し送り (Phase 3 から継続)

[005 §6](005_Phase3_完了・修正メモ.md) で Phase 4 への申し送りとなっていた 3 項目の Phase 4 における扱いを以下のとおり整理する。

| 元項目 | Phase 4 での扱い |
| --- | --- |
| [005 §6.1](005_Phase3_完了・修正メモ.md) `AttachQuery` の Hwnd 対応 | **Phase 6 に持ち越し**。`windows_close` / `windows_launch` の追加と同時に行う方が引数拡張のコストが一度で済むため (§2) |
| [005 §6.2](005_Phase3_完了・修正メモ.md) `NotepadppSmokeTests` の同種リスク | Phase 6 で Hwnd 対応が入れば自然解消する見込み。Phase 4 では追加対策しない |
| [005 §6.3](005_Phase3_完了・修正メモ.md) 実 MCP クライアントからの手動疎通 | **Phase 4 の §6.2 (E-2) に統合**。Phase 4 では VS Code Copilot Chat で stdio + HTTP の両方を確認する |

## 8. 完了条件

- ビルド成功 (0 warning / 0 error)
- L1 + L2 + L3 + L4 + L5 すべて green (Phase 3 までの既存ケース + Phase 4 で追加した HTTP テスト)
- 新コマンド体系 (`adact local` / `adact serve`) で起動でき、旧 `adact --local` フラグが削除されていること
- VS Code Copilot Chat から HTTP + stdio の両方で電卓・メモ帳を操作できることを手動確認
