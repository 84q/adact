# Phase 4 完了確認・記録メモ

> 前提: [001\_要件定義.md](001_要件定義.md) / [002\_アーキテクチャ設計.md](002_アーキテクチャ設計.md) / [003\_実装計画.md](003_実装計画.md) / [005\_Phase3\_完了・修正メモ.md](005_Phase3_完了・修正メモ.md) / [006\_Phase4\_設計.md](006_Phase4_設計.md)
> 目的: 同一セッション内で設計→実装→レビューを回した Phase 4 の完了条件充足を記録し、Phase 5 / Phase 6 への申し送り資料とする。
> 検証日: 2026-04-27 (JST)

***

## 1. 検証の経緯

| 項目 | 内容 |
| --- | --- |
| 完了報告コミット | `a8e5af6` (#7 docs follow-up + named semaphore 時点) — 本メモのコミット (#8) で更新予定 |
| 本セッションの作業 | 設計フェーズ → 実装フェーズ (#1〜#7) → 完了確認 (#8) |
| 検証対象 | [006\_Phase4\_設計.md](006_Phase4_設計.md) §8 完了条件 + [003\_実装計画.md](003_実装計画.md) §5 Phase 4 やること |

設計フェーズでは F-1〜F-10 の判断点から F-1 / F-2 / F-3 / F-4 / F-6 / F-9 を [006\_Phase4\_設計.md](006_Phase4_設計.md) に反映した。実装フェーズはサブタスク #3 (CLI 再構成) → #1 (Common 抽出) → #2 (UiaEngine 直列化) → #4 (HTTP サーバー本体) → #5 (`Adact.Mcp.Http.Tests`) → #6 (手動疎通) → #7 (docs 後追い + named semaphore) → #8 (本メモ) の順で実施した。

***

## 2. レビュー結果サマリ

| 観点 | 結果 |
| --- | --- |
| Phase 4 完了条件 ([006 §8](006_Phase4_設計.md)) | 全項目充足 (本メモ §3) |
| ビルド | 0 warning / 0 error |
| L1 + L2 テスト | 54 / 54 passed |
| L3 + L4 + L5 | 8 / 8 passed (詳細は §4) |
| 検出指摘 | 各サブタスクで Critical / Major 0、Minor 1 件 (案 A 採用に伴う設計 docs 修正、#7 で対応済)、Nit 数件は受容 |

各サブタスクで Implementation → Research の review-loop を 1 回ずつ回した。Minor 1 件は L4 テストを `WebApplicationFactory<TEntryPoint>` ベースから案 A (`HttpHost.BuildApplication(0)` を直接 `StartAsync`) に切り替えた件で、設計 docs 側の追従漏れを #7 で修正した。Nit はいずれも申し送りせず受容している。

***

## 3. 主要成果

| 項目 | 内容 |
| --- | --- |
| `Adact.Mcp.Common` 新設 | Phase 3 で `Adact.Mcp.Stdio` に置いていた `WindowsTools` / `SessionStore` / `ToolErrors` を抽出し、Stdio / HTTP 両ホストから参照する構成へ |
| `UiaEngine` 直列化 | `SemaphoreSlim(1, 1)` ベースの `RunSerializedAsync` を導入し、`UiaEngine` と `WindowSession` で同一 gate を共有 ([006 §5.2](006_Phase4_設計.md)) |
| HTTP サーバー本体 (`HttpHost.cs`) | Kestrel `127.0.0.1:<port>` バインド、Stateless モード ([006 §3.3](006_Phase4_設計.md))、stderr ログ統一、`serverInfo.name = "adact"` |
| CLI 再構成 (`Adact.Cli`) | Web SDK へ切替え、`adact local` (stdio) / `adact serve --port <n>` (HTTP) のサブコマンド体系へ。旧 `--local` フラグは廃止 ([006 §2](006_Phase4_設計.md)) |
| `Adact.Mcp.Http.Tests` 新設 | L4 Smoke 2 件 + L5 E2E 1 件。`HttpHost.BuildApplication(0)` を `StartAsync` してエフェメラルポートで実 Kestrel 起動、`HttpClientTransport` + `StreamableHttp` で接続 |
| `CalculatorMutex` (named Semaphore) | `Global\AdactCalculatorE2E` で Stdio.Tests / Http.Tests のアセンブリを跨いで電卓 L5 を直列化。`dotnet test --filter Layer=E2E` 1 コマンドで 3/3 green |
| `.vscode/mcp.json` | `adact-stdio` (`dotnet run -- local`) と `adact-http` (`http://127.0.0.1:41300/`) を定義。VS Code Copilot Chat 1.117.0 から手動疎通確認済み ([006 §6.2](006_Phase4_設計.md)) |

***

## 4. 最終テスト結果

| レベル | 内容 | 結果 |
| --- | --- | --- |
| L1 + L2 | Unit / Integration (UIA 非依存) — `UiaEngineSerializationTests` 3 件追加 | 54 / 54 passed |
| L3 | IntegrationUia (FlaUI 実 UIA) | 1 / 1 passed |
| L4 (Smoke) | Engine 2 (notepad++ / Calculator) + Http 2 (initialize / list\_apps) | 4 / 4 passed |
| L5 (E2E) | Stdio 2 + Http 1 (`CalculatorMutex` で直列化) | 3 / 3 passed |
| **合計** | | **62 / 62 passed** |

ビルドは 0 warning / 0 error (`dotnet build adact.sln`)。`dotnet test --filter Layer=E2E` を単体で実行しても `CalculatorMutex` により電卓系の競合は発生しない。

***

## 5. コミット履歴 (本 Phase 4 範囲)

`git log --oneline af38a1e..HEAD` 取得時点。

| Commit | 種別 | サブタスク | 概要 |
| --- | --- | --- | --- |
| `af38a1e` | chore | #3 | Phase 4 scaffold (CLI サブコマンド体系化 + 設計 docs) |
| `9e63eaf` | refactor | #1 | `Adact.Mcp.Common` を `Adact.Mcp.Stdio` から抽出 |
| `b1912b0` | feat | #2 | `UiaEngine` / `WindowSession` を共有 `SemaphoreSlim` で直列化 |
| `5d5a16c` | feat | #4 | HTTP MCP サーバー (`adact serve`) 実装 |
| `3a8cff8` | test | #5 | `Adact.Mcp.Http.Tests` (L4 Smoke + L5 E2E) を追加 |
| `fc11ef5` | chore | #6 | `.vscode/mcp.json` を追加 (手動疎通用) |
| `a8e5af6` | chore | #7 | docs 後追い (案 A 反映) + named semaphore による L5 アセンブリ間直列化 |
| (本コミット予定) | docs | #8 | `discussion/007_Phase4_完了.md` 追加 |

***

## 6. Phase 5 / Phase 6 への申し送り

### 6.1 Phase 5 (proxy 実装)

- `adact` (引数なし) を localhost MCP HTTP プロキシ既定モードとする計画。Phase 4 で確定した `adact local` / `adact serve` のサブコマンド体系の上に proxy を載せる前提。
- proxy E2E テスト追加時には `CalculatorMutex` の共通化 (現在 `Adact.Mcp.Stdio.Tests` と `Adact.Mcp.Http.Tests` に同内容を 2 重に保持) を再検討する。テスト用ヘルパー DLL に集約する案を最有力候補とする。

### 6.2 Phase 6 (UI 拡張)

- [005 §6.1](005_Phase3_完了・修正メモ.md) の `AttachQuery.Hwnd` 対応は Phase 6 へ持ち越し継続。`windows_close` / `windows_launch` 追加と同時に行うと引数拡張のコストが一度で済む。
- [005 §6.2](005_Phase3_完了・修正メモ.md) の `NotepadppSmokeTests` 同種リスクも Phase 6 で Hwnd 対応が入れば自然解消する見込み。Phase 4 では追加対策しない方針を維持。

### 6.3 設計差分の記録

- [006 §4.5](006_Phase4_設計.md) / §6.1 で当初想定した L4 テスト構成 (`WebApplicationFactory<TEntryPoint>`) は、実装段階で **案 A (`HttpHost.BuildApplication(0)` + `StartAsync` + `HttpClientTransport` の `StreamableHttp` モード)** に切り替えた。設計 docs はサブタスク #7 で同案に追従済。Phase 5 で別の HTTP テストを書く際もこの構成を踏襲する。
- 設計レビュー時点では **Mutex** 採用想定だったが、`Mutex` の thread affinity (取得スレッドと解放スレッドが一致する必要) が xUnit の `await` 跨ぎで運用しづらいため、実装では **named `Semaphore`** に変更した。クラス名は役割名として `CalculatorMutex` を残置 (利用側のコードを変えずに済むため)。
