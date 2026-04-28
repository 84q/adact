# Phase 6 完了メモ — Skill 機構 (`adact install --skills`)

> 前提: [012\_Phase6要件定義.md](012_Phase6要件定義.md) / [013\_Phase6設計.md](013_Phase6設計.md)
> 目的: [013\_Phase6設計.md](013_Phase6設計.md) に基づく Phase 6 実装の完了記録。
> 検証日: (実装完了時、2025 年内の継続)

***

## 1. 概要

Phase 6 では AI コーディングクライアント (VS Code GitHub Copilot / Claude Code / OpenAI Codex CLI) が ADACT を発見・正しく利用するための **Skill 機構** を導入した。`adact install --skills <client>` サブコマンドで `agentskills.io` 標準準拠の SKILL.md 一式 (1 SKILL.md + `references/<cmd>.md` × 5) をクライアント別の自動読込パスへ展開する。Playwright Agent CLI (`@playwright/cli install --skills`) をリファレンス実装としつつ、`--skills copilot|claude|codex` の 3 系統に**完全分岐**したパス展開を採用。`--global` でユーザーホーム配下にも展開できる。Skill 内容は英語で、5 サブコマンド (`attach` / `snapshot` / `click` / `fill` / `list-apps`) の使い方・例・エラー復帰パターンを Progressive Disclosure 3 階層で提供する。

***

## 2. 実装コミット

| # | 種別 | サブタスク | Commit |
| --- | --- | --- | --- |
| 1 | docs | 要件定義 (012) と設計 (013) | `1215604` |
| 2 | feat | `adact install --skills` サブコマンド + Skill バンドル + テスト + 規約追記 | `3779ee2` |

***

## 3. 設計からの差分

[013\_Phase6設計.md](013_Phase6設計.md) と実装の主な差分・追加判断:

| 区分 | 設計 (013) | 実装 | 判断 |
| --- | --- | --- | --- |
| エラーコード名 | §2.4 で `STALE_REF` を例示 | Skill では `Adact.Mcp.Common/ToolErrors.cs` 実コード (`REF_NOT_FOUND` 等) に統一。設計書 §2.3 も同時訂正 | レビュー指摘 (Major-1) を受け、CLI 側マッピング層を入れずに Skill 側を実コード名に揃える方針を採用 |
| `ErrorCodes` CLI 定数 | 言及なし | `StaleRef` / `ClickFailed` / `FillFailed` 定数を削除 (declaration のみで未 emit) | 不要コードの除去。実装と Skill の整合に加え、実装内部の dead code を一掃 |
| `attach` のエラー区別 | §3.4 のみ | Skill `attach.md` で書式不正 (`INVALID_ARGUMENT`) と registry 未登録 (`INVALID_WINDOW_REF`) を明確に区別 | レビュー指摘 (Minor-2) 反映。`AttachCommand.ValidateAttachArgs` の挙動と整合 |
| Skill `description` | §2.3 で「発動条件を含める」と一般原則のみ記載 | "Use when the user asks to automate, drive, script, or test ..." の **アクション動詞 + 具体的タスク列挙** スタイル | レビュー指摘 (Minor-1) 反映。AI クライアントが拾いやすい記述に強化 |

***

## 4. 機能サマリ

### 4.1 サブコマンド

```
adact install --skills <copilot|claude|codex> [--global]
```

- `--skills`: 必須・単数指定のみ。`--skills all` / 複数指定 / `--dry-run` / `--no-overwrite` / `--name` は未提供。
- `--global`: フラグ。指定時はユーザーホーム配下に展開。

### 4.2 展開先パスマトリクス

| `<client>` | 既定 (cwd 配下) | `--global` |
| --- | --- | --- |
| `copilot` | `<cwd>/.github/skills/adact-cli/` | `~/.copilot/skills/adact-cli/` |
| `claude`  | `<cwd>/.claude/skills/adact-cli/` | `~/.claude/skills/adact-cli/` |
| `codex`   | `<cwd>/.agents/skills/adact-cli/` | `~/.agents/skills/adact-cli/` |

### 4.3 Skill バンドル構造

```
src/Adact.Cli/Skills/adact-cli/
├── SKILL.md                # frontmatter (name, description) + 概要本文
└── references/
    ├── attach.md
    ├── snapshot.md
    ├── click.md
    ├── fill.md
    └── list-apps.md
```

`Adact.Cli.csproj` で `Skills/**/*.md` を `CopyToOutputDirectory="PreserveNewest"` に指定。実行時は `AppContext.BaseDirectory/Skills/adact-cli/` から再帰コピー。テストプロジェクトでも `<Link>` でテスト bin に展開。

***

## 5. テスト状況

| レベル | 内容 | 結果 |
| --- | --- | --- |
| Unit | `InstallCommandTests` (引数バリデーション、`references/*.md` ↔ CLI サブコマンド名 ↔ `Program.BuildRoot()` の三方一致) | passed |
| Integration | `InstallCommandIntegrationTests` (3 クライアント × cwd/global = 6 ケース + 上書き 1 ケース) | passed |
| 全体 | Cli.Tests Unit Layer 104 件 | リグレッションなし |

`dotnet build adact.sln`: 0 errors, 新規警告なし。

***

## 6. レビューループ実績

`review-loop` skill に従い実装→レビューを実施。

| ループ | 指摘 |
| --- | --- |
| 1 | Research レビュー: Major 1 / Minor 2 / Nit 2 (Skill エラーコード不一致、description 改善、attach.md エラー区別、テスト整形、コメント追加) |
| 2 | 修正後再レビュー: **指摘ゼロ** |

***

## 7. 完了判定

- [x] **Skill 内容の人間レビュー** 完了
- [x] **3 クライアント手動スモーク** 完了 (copilot / claude / codex すべて、`adact install --skills <client>` 後に AI クライアントが ADACT を認識し 5 サブコマンドの組み合わせでタスク達成できることを確認)

Phase 6 受入条件すべて充足。

***

## 8. 申し送り

- snapshot 出力サイズの縮減テクニック (要件 §1 の動機 4) は本 Phase で**完全に Phase 7 へパス**した。Skill 内でも触れていない。
- 電卓・メモ帳などのボックスレシピ (要件 §1 の動機 5) は **Phase 6 スコープ外**。将来 Phase で recipes/ 等を追加する際は `adact-cli` Skill とは別 Skill として独立させるか、`references/` 拡張で吸収するか要設計。
- MCP ツール `description` フィールドの強化は Phase 6 では行わなかった。AI クライアントが MCP 接続のみで ADACT を理解できる必要が出てきた場合は別 Phase で検討。
- Skill 同期ルールは [.github/copilot-instructions.md](../.github/copilot-instructions.md) に追記済み。CLI/MCP サブコマンドを追加・改名・削除した際は対応する `references/<cmd>.md` も更新すること。Unit テスト (`InstallCommandTests`) で同期ずれを検知できる。
- `~/.copilot/skills/` パスは Research 調査時点 (2025) の VS Code Copilot 仕様。今後の VS Code 仕様変更に追随が必要な場合は `InstallCommand.cs` のパス定義を更新すること。
