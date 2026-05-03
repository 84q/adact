# ADACT — Agent Guide

## 1. プロジェクト概要

ADACT (AI-driven Desktop Application CLI Tools) は、AI エージェントや人間が `adact` CLI 経由で Windows デスクトップアプリを操作するツール群。Playwright の snapshot/ref モデルを UIA (FlaUI) に持ち込む。

## 2. 主要技術スタック

- .NET 10, C# 12
- FlaUI.UIA3 (UI Automation バックエンド)
- ASP.NET Core + MCP SDK (HTTP daemon)
- xUnit + NSubstitute (テスト)

## 3. ビルド

```powershell
dotnet build adact.sln
```

## 4. テスト実行

テストは Layer Trait で分類される。実アプリを起動するテストは `Smoke` / `E2E` に分離されている。

```powershell
# 高速層（CI 用）
dotnet test --filter "Layer=Unit|Layer=Integration"

# 実アプリを起動する層（ローカルのみ）
dotnet test --filter Layer=Smoke
```

## 5. 主要ディレクトリ構成

```
src/
  Adact.Cli/           CLI エントリポイント（`adact.exe`、Windows 専用）
  Adact.Cli.Client/    CLI クライアント実装（クロスプラットフォーム対応）
  Adact.Cli.Core/      CLI コマンド、接続、出力変換
  Adact.Cli.Server/    HTTP / Named Pipe MCP daemon
  Adact.Engine/        UIA 操作実体（snapshot, click, fill など）
  Adact.Mcp.Common/    MCP tools, SessionStore, WindowRefStore
tests/                 各プロジェクトの対応テスト（Layer Trait 分類）
discussion/            設計検討・完了メモ
docs/                  現行実装に合う安定ドキュメント
```

## 6. 主要な設計・制約

- **Element Ref**: `s<sid>e<eid>` 形式。generation は廃止済み。
- **Auto-snapshot**: 一部ツール呼び出し後、daemon が自動で snapshot を再取得・返却する。
- **接続解決**: CLI client → HTTP daemon (`adact serve`) → Engine → Windows app。
- **Daemon 制約**: Engine は対話 Windows セッションで動作する必要がある。サービスセッションでは `NO_INTERACTIVE_SESSION` エラー。
- **Session / Ref ライフサイクル**: `WindowSession` がアプリ 1 つに対応。`WindowRefStore` が ref → session の解決を行う。

## 7. 開発時の注意

- **Skill 機構**: `.agents/skills/` に `adact-cli`, `testing-strategy`, `review-loop` がある。該当作業時は必ず読み込むこと。
- **テスト直列化**: `IntegrationUia` / `Smoke` / `E2E` は実アプリを操作する。並列実行に注意。
- **マルチエージェント体制**: 実装は Implementation エージェント、レビューは Orchestrator が担当。Implementation から Reviewer を呼ばない。
- **変更範囲の最小化**: 担当範囲外の変更は避け、既存のユーザ変更を巻き戻さない。
