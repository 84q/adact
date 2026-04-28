# 010 Phase 5 完了メモ

> 前提: [001\_要件定義.md](001_要件定義.md) / [003\_実装計画.md](003_実装計画.md) / [008\_要件再整理.md](008_要件再整理.md) / [009\_Phase5設計.md](009_Phase5設計.md)
> 目的: [009\_Phase5設計.md](009_Phase5設計.md) に基づく Phase 5 実装の完了記録。Phase 6 以降への申し送り資料。
> 検証日: 2026-04-28 (JST)

***

## 1. 概要

Phase 5 では Coding AI / 人間が直接叩く主インターフェースとなる **`adact <subcommand>` CLI クライアント** を実装した。Phase 4 で立てた HTTP MCP daemon (`adact serve`) を `Adact.Cli.Server` として class library 化し、`adact.exe` シングルバイナリの中に CLI client / HTTP server / stdio MCP の 3 モードを共存させた。daemon 側には `windowRef` (`w<n>`) を発行する `WindowRefStore` を新設し、`sessionId` (`s<n>`) / Element Ref (`s<sid>g<gen>e<eid>`) と合わせて 3 種の Ref ID 体系を確立。CLI は HTTP MCP (Streamable HTTP, `/mcp`) 経由で daemon に接続し、12 のサブコマンド (`list-apps` / `attach` / `snapshot` / `click` / `fill` / `detach` / `close` / `kill` / `close-all` / `daemon-stop` + `serve` / `local`) を提供する。ローカル直結とリモート接続の両構成、key-value (stdout) + TSV (`list-apps` / `close-all`) + key-value (stderr) のシンプルな出力規約、exit code 0/1/2/3 の 4 段階、`.adact/config.json` ベースの接続先解決を備える。

***

## 2. 実装サブタスクとコミット

`git log --oneline 3346eb0..HEAD` (設計コミット 3346eb0 以降) ベース。

| # | 種別 | サブタスク | Commit |
| --- | --- | --- | --- |
| 1 | refactor | `Adact.Cli` を `Adact.Cli` (client) + `Adact.Cli.Server` (HTTP server class lib) に分割、エンドポイントを `/` → `/mcp` に変更 | `439b1b5` |
| 2 | feat | `WindowRefStore` + `WindowKey` 新規実装、`windows_attach` の windowRef idempotent 化 | `5cf8e02` |
| 3 | feat | System.CommandLine による CLI 骨格 + 12 サブコマンド スタブ | `7aafca6` |
| 4 | feat | Connection 層 (`ConfigLoader` / `ServerEndpoint` / `AdactMcpClient`) | `91fb80a` |
| 5 | feat | `list-apps` / `attach` / `snapshot` 本実装 | `673ef7b` |
| 6 | refactor | snapshot ヘルパ・バリデータ整理 (#5 / #6 review 反映) | `255d8e1` |
| 7 | feat | lifecycle MCP ツール (`detach` / `close` / `kill` / `close-all` / `daemon-stop`) | `82863c4` |
| 8 | feat | lifecycle CLI コマンド | `ea3dbdf` |
| 9 | test | CLI L4 Smoke + L5 E2E テスト | `6401a42` |
| 10 | docs | 既存ドキュメント更新 (001 / 003 / 008) | (本コミット予定) |
| 11 | docs | 本完了メモ (`010_Phase5_完了.md`) | (本コミット予定) |

> 旧サブタスク #5 と #6 は実装途中で `click` / `fill` をまとめて 1 コミット (`255d8e1`) に統合した。`673ef7b` で `click` / `fill` の本実装まで含めて完了している。

***

## 3. 設計からの差分・追加判断

[009\_Phase5設計.md](009_Phase5設計.md) と実装の主な差分:

| 区分 | 設計 (009) | 実装 | 判断 |
| --- | --- | --- | --- |
| snapshot 拡張子 | §5.2 で `.json` (本文) / 一部 `.yml` 言及 | 一貫して `.json` (内容も JSON) | 中身が JSON のため拡張子を統一 |
| `attach` 出力 | §5.2 に `windowRef` 行は記載済 | `windowRef` 行を実装 | 設計通り、補完して文書整合 |
| ToolErrors | §6.3 で `INVALID_WINDOW_REF` / `LOCAL_ONLY` 等を追加 | 上記 + `NOT_FOUND` / `CLOSE_FAILED` / `KILL_FAILED` / `INTERNAL_ERROR` を新設 | lifecycle 系で必要、ToolErrors に追加 |
| `daemon-stop` ガード | §4.5 で CLI 側 LOCAL_ONLY ガード明記 | MCP ツール側でも stdio モード時に `LOCAL_ONLY` を返す二重ガード | stdio から HTTP daemon を意図せず止めるリスクを排除 |
| `close-all` の実装 | §4.5 で `close` を session 全件に適用 | session が 0 件のときも exit 0 / 空 TSV ヘッダのみ出力 | 「該当なし」を成功扱いとする運用判断 |
| daemon 側 snapshot 同梱 | §4.4 で `attach` / `click` / `fill` 成功時に snapshot path を返す | 設計通り。`--no-snapshot` 時は snapshot file 生成を抑止し path 行も省略 | 設計通りの実装 |
| L5 E2E ケース構成 | §9.2 で 4 シナリオ列挙 | `CalculatorCliE2ETests` 1 メソッドに通しフローを集約 + Smoke で `daemon-stop` LOCAL_ONLY / unknown title をカバー | テスト数より共有 fixture の起動コスト最小化を優先 |

***

## 4. テスト状況

| レベル | 内容 | 結果 |
| --- | --- | --- |
| L1 + L2 | Engine / Mcp.Common / Cli の Unit + FakeElement 結合 (`WindowRefStore` 採番、`ConfigLoader`、`ServerEndpoint`、`KeyValueWriter` / `TsvWriter` / `ErrorWriter` 等) | all passed |
| L3 | `Adact.Engine.Tests/IntegrationUia` (FlaUI 実 UIA) | passed |
| L4 (Smoke) | Engine 2 + Http 2 + Cli 4 (`help` / `list-apps` TSV / `daemon-stop` LOCAL_ONLY / attach unknown title) | passed |
| L5 (E2E) | Stdio 2 + Http 1 + Cli 1 (Calculator: list-apps → attach → snapshot → click(num1) → close) | passed |

ビルドは 0 warning / 0 error (`dotnet build adact.sln`)。Calculator を共有する L5 は `CalculatorMutex` (named Semaphore `Global\AdactCalculatorE2E`) で `Adact.Mcp.Stdio.Tests` / `Adact.Mcp.Http.Tests` / `Adact.Cli.Tests` を跨いで直列化。

***

## 5. 手動スモーク確認 (未実施 — ユーザによる別途実施)

設計 [009 §9.2](009_Phase5設計.md) を補う、開発者向け手動確認手順:

1. **daemon 起動**

   ```powershell
   dotnet run --project src/Adact.Cli -- serve --port 41300
   ```

2. **別ターミナルで CLI 通しフロー (電卓)**

   ```powershell
   dotnet run --project src/Adact.Cli -- list-apps
   dotnet run --project src/Adact.Cli -- attach --process-name CalculatorApp.exe
   dotnet run --project src/Adact.Cli -- snapshot
   # snapshot ファイルから Element Ref (例 s1g1e7) を確認
   dotnet run --project src/Adact.Cli -- click <ref>
   dotnet run --project src/Adact.Cli -- close
   ```

3. **lifecycle 確認**

   ```powershell
   dotnet run --project src/Adact.Cli -- close-all
   dotnet run --project src/Adact.Cli -- daemon-stop
   # stdout: "stopped"、daemon プロセス終了
   ```

4. **`daemon-stop` の LOCAL_ONLY ガード**

   ```powershell
   # 別マシンの daemon URL を指定
   dotnet run --project src/Adact.Cli -- daemon-stop --server http://192.168.1.10:41300/mcp
   # exit 2、stderr に error LOCAL_ONLY
   ```

5. **`.adact/config.json` 経由の接続先指定**

   `.adact/config.json` に `{"server": "http://127.0.0.1:41300/mcp"}` を置いた状態で
   `--server` フラグなしで `list-apps` が動くこと。

6. **VS Code Copilot Chat (内蔵 MCP)**

   `.vscode/mcp.json` の `adact-http` (`http://127.0.0.1:41300/mcp`) で `windows_list_apps` / `windows_attach` / `windows_snapshot` / `windows_click` / `windows_fill` + lifecycle 系が呼べること。

***

## 6. 既知の制約 / 後続課題

[009 §12](009_Phase5設計.md) を踏襲・更新したもの:

- `--verbose` フラグ未実装 — Phase 7+
- 環境変数による接続先指定未対応 — Phase 7+
- daemon 自動 spawn 未対応 — Phase 9+
- 認証 / TLS / CORS 未対応 — Phase 7+
- `STALE_REF` 時の自動再 snapshot は未対応 (CLI / AI 側判断) — Phase 7+
- `--format` (Markdown / JSON) 切替未対応 — Phase 7+
- `--no-snapshot` 時の出力フォーマット最適化 (実装中の Mi3 申し送り) — Phase 7+
- L5 E2E 内の global state 取り扱い (`CalculatorMutex` の三重複保持、Mi4 申し送り) — テスト用ヘルパ DLL に集約 (Phase 6 / Phase 7)
- `WindowSession.KillAsync` の同一性検証: PID 再利用対策として `Process.StartTime` での照合は将来課題 (現状 `WindowKey` の構築側で StartTime を保持済、Kill 経路で未利用)
- stdio MCP モード (`adact local`) の去就: HTTP 一本化が進めば削除候補 — Phase 7+

***

## 7. 次フェーズへの申し送り

### 7.1 Phase 6 (Skill 機構)

- 初期 Skill は Phase 5 の 5 サブコマンド (`list-apps` / `attach` / `snapshot` / `click` / `fill`) と lifecycle 5 種の計 10 コマンドをカバー対象に検討する。`adact serve` / `adact local` は daemon 管理で運用者向けのため Skill 化対象外でよい。
- Skill から CLI を呼ぶ前提のため、CLI 出力 (key-value + TSV) を構造化テキストとしてそのまま AI に渡せる設計は維持。Skill が出力をパースする責務とする。
- `windowRef` / `sessionId` / Element Ref の使い分けを Skill ドキュメントで明示する必要あり。

### 7.2 Phase 7 (安定化)

- [009 §12](009_Phase5設計.md) の Phase 7+ 列に挙がっている項目 (`--verbose` / 環境変数 / 認証・TLS / `--format` / STALE_REF 自動再 snapshot / `.adact/config.json` フィールド拡充) は本フェーズで集中整備。
- `WindowKey` のうち `processStartTime` を `WindowSession.KillAsync` でも利用し、PID 再利用に対する Kill 安全性を強化する。
- `CalculatorMutex` の 3 アセンブリ重複を共有テストヘルパ DLL に集約。
- 失敗時のスクリーンショット添付 (Phase 7 の元タスク) と Snapshot サイズチューニングは CLI Token 効率の観点でも見直す。

### 7.3 Phase 8 (サブコマンド拡張)

- Phase 5 で確立した CLI 出力規約 (key-value + TSV、exit 0/1/2/3、stderr `error/message/hint`) と Ref ID 体系 (`w<n>` / `s<n>` / `s<sid>g<gen>e<eid>`) を `close` (実 close は Phase 5 で済) / `launch` / `wait-for` / `screenshot` / `hover` / `press` / `type` / `keyboard` / `mouse` 系の追加コマンドにそのまま踏襲する。
- `attach` のフラグ体系 (`--process-name` / `--title` / `--process-id` / `--class-name` + ポジショナル `windowRef`) は Phase 7 で追加予定の `AttachQuery.Hwnd` 拡張時に `--hwnd` フラグを足すだけで済む構造になっている。
