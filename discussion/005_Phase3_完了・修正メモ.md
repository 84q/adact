# Phase 3 完了確認・レビュー対応メモ

> 前提: [001\_要件定義.md](001_要件定義.md) / [002\_アーキテクチャ設計.md](002_アーキテクチャ設計.md) / [003\_実装計画.md](003_実装計画.md)
> 目的: 別セッションで完了報告された Phase 3 のレビューと、本セッションで実施した残課題対応の記録。Phase 4 着手時の参照資料とする。
> 検証日: 2026-04-27

***

## 1. 検証の経緯

| 項目 | 内容 |
| --- | --- |
| 完了報告コミット | `4683e7d`（別セッションで Phase 3 完了として報告） |
| 本セッションの作業 | レビュー → 指摘事項の対応 → 実機テスト → 文書化 |
| レビュー対象 | [003\_実装計画.md](003_実装計画.md) §4 の Phase 3 完了条件 10 項目 (003 §4 全体から検証観点として整理した 10 項目: プロジェクト構成 / MCP ツール 5 種 / Session ハイブリッド / エラー応答 / 自動待機 γ 案 / CLI --local / stderr ロギング / L5 テスト / L1〜L4 維持 / 公式 SDK 採用) |

別セッションで Phase 3 まで完了した旨の報告を受け、本セッションで完了条件の充足確認とコードレビューを実施した。レビュー過程で 2 件の Minor 指摘 (M-1, M-2) を抽出し、さらに実機テスト中に Calculator 起動シナリオで flakiness を 1 件発見した。本メモは以上の経緯と対応結果をまとめる。

***

## 2. レビュー結果サマリ

| 観点 | 結果 |
| --- | --- |
| Phase 3 完了条件 (10 項目)\* | 自動テストレベルでは充足 (`4683e7d` 時点)。実 MCP クライアントによる手動疎通確認は §6.3 のとおり未実施 |
| ビルド | 0 warning / 0 error |
| L1 + L2 テスト | 51 / 51 passed |
| 検出指摘 | Minor 2 件 (M-1 / M-2) + 実機 flakiness 1 件 |
| 対応後の最終テスト | 56 / 56 passed (詳細は §4) |

検出指摘はいずれも Minor 相当で、Phase 3 の完了条件を覆すものではない。ただし Phase 4 (windows\_close 等の追加) との整合上、ここで対応しておく方が望ましいと判断した。

\* 「Phase 3 完了条件 10 項目」は 003 §4 全体から検証観点として整理した 10 項目 (プロジェクト構成 / MCP ツール 5 種 / Session ハイブリッド / エラー応答 / 自動待機 γ 案 / CLI --local / stderr ロギング / L5 テスト / L1〜L4 維持 / 公式 SDK 採用)。003 §4 完了条件セクション自体は明示的には 2 項目 (検証通過 / クライアント疎通)。

***

## 3. レビュー指摘と対応

### 3.1 M-1: auto-wait 仕様の抽象化

| 項目 | 内容 |
| --- | --- |
| 事象 | [002\_アーキテクチャ設計.md](002_アーキテクチャ設計.md) §6.6 が UIA `WaitWhileBusy` を要求しているが、実装は `Process.WaitForInputIdle` を使用 |
| 根本原因 | FlaUI.UIA3 では `WaitWhileBusy` 相当 API が公開されておらず、機械的な仕様一致が不可能 |
| 採用方針 | **案 C — 仕様を抽象化**。両者は実質等価 (UI スレッドのアイドル待機) のため、設計仕様を「実装系が提供する `WaitForInputIdle` 系の機構を用いる」に緩和 |
| 対応 commit | `3d4f535` |
| 対応ファイル | [001\_要件定義.md](001_要件定義.md) / [002\_アーキテクチャ設計.md](002_アーキテクチャ設計.md) / [003\_実装計画.md](003_実装計画.md) |

### 3.2 M-2: AttachQuery への ClassName 追加

| 項目 | 内容 |
| --- | --- |
| 事象 | `WindowsTools` が className 指定時に「ProcessId 経由で再アタッチ」する実装になっており、同一 PID で複数ウィンドウを持つアプリでは取り違えのリスクがあった |
| 採用方針 | **案 α — `AttachQuery` に `ClassName` を追加**し、Engine 側で正式に対応する。MCP ツール層での再アタッチ・ヒューリスティクスを廃止 |
| 対応 commit | `3d4f535` |
| 対応ファイル | `AttachQuery.cs` / `UiaEngine.cs` / `WindowsTools.cs` / 新規テスト 2 ファイル (Unit `AttachQueryMatchesTests` 6 ケース + Integration `AttachQueryClassNameDisambiguationTests` 3 ケース、計 9 ケース) |

### 3.3 Calculator テストの flakiness

| 項目 | 内容 |
| --- | --- |
| 発見契機 | L3 (IntegrationUia) 初回実行で `AmbiguousAttachException` が断続的に発生 |
| 原因分析 | Windows 11 電卓は同タイトル・同 PID・同 ClassName のウィンドウが起動直後に瞬間的に複数存在する。`AttachQuery` のどのフィールド (Title / ProcessId / ClassName) でも一意に絞れず、Hwnd のみが一意な識別子 |
| 採用方針 | **テスト側の自衛のみ**。`Initialize` 冒頭で既存の電卓プロセスを Kill する。本質対策 (Hwnd ベース attach) は Phase 4 送り (§6.1) |
| 対応 commit | `3d4f535` |
| 対応ファイル | `CalculatorSnapshotTests.cs` / `CalculatorSmokeTests.cs` |

***

## 4. 最終テスト結果

| レベル | 内容 | 結果 |
| --- | --- | --- |
| L1 + L2 | Unit / Integration (UIA 非依存) | 51 / 51 passed |
| L3 | IntegrationUia (FlaUI 実 UIA) | 1 / 1 passed |
| L4 | Smoke (notepad++ / Calculator) | 2 / 2 passed |
| L5 | E2E (MCP stdio 経由) | 2 / 2 passed |
| **合計** | | **56 / 56 passed** |

ビルドは 0 warning / 0 error。電卓を事前起動した状態でも §3.3 の対応により green。

***

## 5. コミット履歴 (本セッションでの追加)

| Commit | 種別 | 概要 |
| --- | --- | --- |
| `105954a` | chore | `.editorconfig` / `Directory.Build.props` / `.vscode/settings.json` 追加、agent 指示の整備 |
| `722f176` | style | `.editorconfig` に従いコードベース全体を 4-space インデントに整形 |
| `3d4f535` | fix | Phase 3 レビュー指摘 (M-1 / M-2 / Calculator flakiness) への対応 |

***

## 6. Phase 4 への申し送り (残課題)

### 6.1 AttachQuery の Hwnd 対応 (本質対策)

- **背景**: §3.3 のとおり、同 PID・同タイトル・同 ClassName のウィンドウは現状の `AttachQuery` で識別不能。Hwnd のみが一意な識別子。
- **案 (要 Phase 4 設計)**:
  - `AttachQuery` に `Hwnd` フィールドを追加
  - `WindowInfo.NativeWindowHandle` 経由で hwnd-attach をサポート
- **影響範囲**: Engine API + MCP ツール (`windows_attach` の引数拡張) + テストの再構築
- **整合性**: Phase 4 で `windows_close` 等を追加する際に同時実装すると引数拡張が一度で済む。

### 6.2 NotepadppSmokeTests の同種リスク

- 既存 notepad++ プロセスが残った状態でテストを開始すると、`ByProcess("notepad++")` が複数マッチする可能性がある。
- 発生メカニズムは Calculator (同 PID 内で複数ウィンドウ) とは異なり、**複数プロセス側**で起きる。
- §6.1 の Hwnd 対応が入れば自然解消する。それまでに Calculator と同様の Initialize 冒頭 Kill を導入するかは Phase 4 着手時に判断する。

### 6.3 Phase 3 完了条件の MCP クライアント疎通

- [003\_実装計画.md](003_実装計画.md) §4 の検証項目「最低 1 つの MCP クライアント (Claude Code 等) から ADACT 経由で操作」について。
- L5 自動テストでは spawn ベースの自前 SDK クライアントによる疎通を確認済み。
- **Claude Code 等の実クライアントからの手動疎通確認は未実施** (環境準備の手間が大きいため)。
- Phase 4 着手前に実施するかは別途判断。
