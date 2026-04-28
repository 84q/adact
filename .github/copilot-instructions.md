# Project Guidelines

## プロジェクト構造

エレベータシミュレータシステム。3つのコンポーネントで構成される。

| ディレクトリ            | 技術                        | 役割                                                           |
| ----------------------- | --------------------------- | -------------------------------------------------------------- |
| `sim/`                  | C++17 (CMake)               | シミュレータ本体。制御盤とシリアル通信し、エレベータ動作を模擬 |
| `web/backend/`          | TypeScript (Hono + Node.js) | Web API サーバー。シミュレータの操作・設定を提供               |
| `web/frontend/`         | TypeScript (React + Vite)   | Web UI。Jotai で状態管理                                       |
| `web/common/interface/` | TypeScript                  | フロントエンド・バックエンド共通の型定義                       |

自動生成ファイル: `sim/src/data/data_def.hpp`, `tools/lua/lib/data.lua`, `tools/lua/lib/data_types.lua` はバックエンド起動時に `web/backend/config/config.toml` から生成される。

## ドキュメント (`docs/`)

安定した知識を集約したドキュメント群。設計検討の履歴は `discussion/` に残し、確定した仕様・設計・開発情報は `docs/` を参照する。

| カテゴリ                                      | 用途                                                             |
| --------------------------------------------- | ---------------------------------------------------------------- |
| [`docs/README.md`](../docs/README.md)         | サイトマップ・読み始めガイド                                     |
| [`docs/glossary.md`](../docs/glossary.md)     | プロジェクト用語集                                               |
| [`docs/architecture/`](../docs/architecture/) | 全体構成・通信経路・各コンポーネント内部構造                     |
| [`docs/api/`](../docs/api/)                   | Lua / REST / SSE / sim ソケットの API リファレンス               |
| [`docs/spec/`](../docs/spec/)                 | シリアル通信プロトコル・specdata 等の仕様                        |
| [`docs/development/`](../docs/development/)   | 環境構築・ビルド/テスト/lint・コーディング規約・自動生成ファイル |

詳細は [`docs/README.md`](../docs/README.md) を参照。

## ビルド・テスト

```bash
# C++ ビルド
cd sim && ./scripts/build.sh

# バックエンドテスト
cd web/backend && npm run dev -- --run
```

## C# Formatting / Linting (adact)

- C# コード変更時は必ずワークスペース設定に従う。
- フォーマットは `.editorconfig` と VS Code 設定 (`.vscode/settings.json`) を基準にする。
- リンターは .NET Analyzers (`Directory.Build.props`) を基準にする。
- 可能な限り `dotnet format adact.sln` で整形した状態を維持する。
- 可能な限り `dotnet build adact.sln` で警告・エラーを確認してから完了とする。

## ADACT Skill 同期 (`adact install --skills`)

- ADACT の CLI/MCP サブコマンドを追加・削除・改名した場合、`src/Adact.Cli/Skills/adact-cli/` 配下の Skill ファイル (`SKILL.md` および `references/<cmd>.md`) も同じコミット内で更新する。
- `references/<cmd>.md` の basename は CLI サブコマンド名と完全一致させる (例: `list-apps.md`)。
- Skill が説明対象とする CLI サブコマンド集合は `tests/Adact.Cli.Tests/Unit/InstallCommandTests.cs` の `ExpectedDocumentedCommands` でも管理する。Skill 化対象を変更したら同テストも更新する。
- Skill 内容は英語で執筆する (agentskills.io 仕様)。frontmatter の `name` はディレクトリ名 `adact-cli` と一致させる。

## 用語集

| 用語                      | 意味                                                         |
| ------------------------- | ------------------------------------------------------------ |
| スペックデータ (specdata) | シミュレータの実行パラメータ（階床数、速度、パルスレート等） |
| ジョブデータ (jobdata)    | エレベータ制御盤のデータを指す用語。                         |
| 信号 (signal)             | DIO基板経由で制御盤とやり取りするI/O信号。                   |
| スイッチ (switch)         | ユーザがUI上で操作するソフトウェアスイッチ                   |
| 呼び (call)               | エレベータのホール呼び・かご呼び                             |
