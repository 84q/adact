# Phase 6 設計 — Skill 機構 (`adact install --skills`)

要件定義: [discussion/012_Phase6要件定義.md](./012_Phase6要件定義.md)

## 1. 概要

ADACT に新サブコマンド `adact install --skills <client>` を追加し、AI コーディングクライアント (Copilot / Claude Code / Codex) が自動読込する所定パスへ ADACT 用の SKILL.md (+ references) を展開する。

リファレンス実装: [Playwright Agent CLI (`@playwright/cli`)](https://github.com/microsoft/playwright)
標準仕様: [agentskills.io](https://agentskills.io/specification)

## 2. Skill ファイル仕様

### 2.1 Skill 名

`adact-cli`

(agentskills.io 仕様: lowercase + hyphen, ディレクトリ名と一致, ≤64 文字)

### 2.2 ディレクトリ構造 (Progressive Disclosure 3 階層)

```
adact-cli/
├── SKILL.md                       # Level 1+2: frontmatter + 概要本文
└── references/                    # Level 3: サブコマンド詳細 (オンデマンド読込)
    ├── attach.md
    ├── snapshot.md
    ├── click.md
    ├── fill.md
    └── list-apps.md
```

### 2.3 SKILL.md 仕様

- **Frontmatter (YAML)** — 必須:
  - `name: adact-cli`
  - `description: <発動条件 + 役割>` (≤1024 文字、英語)
- **本文** (英語、~5k tokens 以内):
  - ADACT の役割概要
  - 5 サブコマンドの一覧 (各 1-2 行) + `references/<cmd>.md` への誘導
  - 共通エラーパターン (`REF_NOT_FOUND` 等) の概要

### 2.4 references/<cmd>.md 仕様

各サブコマンドごとに英語で記述:
- 使い方 (引数・基本フロー)
- 使用例
- エラー復帰パターン

## 3. install コマンド仕様

### 3.1 構文

```
adact install --skills <client> [--global]
```

### 3.2 引数

| 引数 | 値 | 必須 | 既定 | 説明 |
|---|---|---|---|---|
| `--skills` | `copilot` / `claude` / `codex` | 必須 | — | インストール対象クライアント (単数指定のみ) |
| `--global` | flag | 任意 | false | グローバル領域に展開 (false 時は cwd) |

`--skills all` / 複数指定 / `--dry-run` / `--no-overwrite` / `--name` は **未提供** (シンプル維持)。

### 3.3 install 先パスマトリクス

| `<client>` | 既定 (cwd 配下) | `--global` |
|---|---|---|
| `copilot` | `<cwd>/.github/skills/adact-cli/` | `~/.copilot/skills/adact-cli/` |
| `claude` | `<cwd>/.claude/skills/adact-cli/` | `~/.claude/skills/adact-cli/` |
| `codex` | `<cwd>/.agents/skills/adact-cli/` | `~/.agents/skills/adact-cli/` |

`~` はユーザーホーム (Windows: `%USERPROFILE%`)。

### 3.4 動作

1. ソース ディレクトリを特定 (§4 参照)。
2. install 先ディレクトリを作成 (存在すれば再利用)。
3. ソースの SKILL.md と references/ をターゲットへ**再帰コピー** (`fs.cp` 相当)。
4. **既存ファイルは上書き** (確認プロンプトなし)。
5. 完了メッセージで展開先パスを表示。

### 3.5 失敗時

- パス書き込み失敗 (Permission, ディスクフル等) → 非 0 終了 + stderr エラーメッセージ
- 不明な `<client>` 値 → CommandLineParser のバリデーションエラー

## 4. ソース格納

### 4.1 配置

```
src/Adact.Cli/Skills/adact-cli/
├── SKILL.md
└── references/
    ├── attach.md
    ├── snapshot.md
    ├── click.md
    ├── fill.md
    └── list-apps.md
```

### 4.2 csproj 設定

`Adact.Cli.csproj` で `<None Include="Skills/**/*.md">` + `CopyToOutputDirectory="PreserveNewest"` で出力ディレクトリへコピー。

### 4.3 実行時参照

`AppContext.BaseDirectory` 起点で `Skills/adact-cli/` を解決。

## 5. テスト戦略

### 5.1 Integration テスト

`tests/Adact.Cli.Tests/` (新規) または `tests/Adact.Engine.Tests/Integration/` 内に追加:

- `adact install --skills copilot` をテンポラリ cwd で実行 → 期待パスにファイル存在を検証
- `--global` 版は環境変数等で `~` を差し替え可能な設計にして検証
- 3 クライアント × cwd/global = 6 ケース
- 既存ファイル上書き検証 (1 ケース)

### 5.2 Unit テスト (コマンド名同期)

Skill 内の references ファイル名が ADACT サブコマンド一覧と一致することを検証:
- `references/*.md` のファイル名集合 == 実装の CLI サブコマンド名集合
- 不一致 → テスト失敗

これにより**サブコマンド追加時の Skill 更新漏れを検知**する。

### 5.3 手動スモーク (完了判定)

3 クライアントで:
1. `adact install --skills <client>` 実行
2. 各クライアントが ADACT を認識
3. 与えたタスクを 5 サブコマンドの組み合わせで達成

## 6. 保守フロー

### 6.1 コーディング規約

`.github/copilot-instructions.md` または開発ドキュメントに以下を明記:
- ADACT サブコマンド (CLI/MCP) を追加・変更する場合は対応する `src/Adact.Cli/Skills/adact-cli/references/<cmd>.md` も更新する。

### 6.2 自動検証

§5.2 の Unit テストでコマンド名整合性を担保。内容そのものの正確さは人間レビューに委ねる。

## 7. 実装計画

| ステップ | 内容 |
|---|---|
| 1 | `src/Adact.Cli/Skills/adact-cli/SKILL.md` + `references/*.md` を執筆 (英語、5 サブコマンド分) |
| 2 | `Adact.Cli.csproj` に CopyToOutputDirectory 設定追加 |
| 3 | `Adact.Cli/Commands/InstallCommand.cs` (仮) を実装 (CommandLineParser 連携) |
| 4 | パス解決ロジック (cwd / `--global` / クライアント別) |
| 5 | 再帰コピー実装 |
| 6 | Integration テスト (6 ケース + 上書き) |
| 7 | Unit テスト (コマンド名同期) |
| 8 | コーディング規約への保守ルール追記 |
| 9 | 3 クライアント手動スモーク + 内容レビュー |

## 8. 設計上の留意点

- **Skill 名・ディレクトリ名・frontmatter `name` は完全一致**させること (agentskills.io 必須要件)。
- frontmatter `description` には**発動条件**を含める (例: `Use when working with ADACT to automate Windows GUI applications`)。
- Codex は AGENTS.md ベースだが Skills は `.agents/skills/` で受ける。`AGENTS.md` への追記は本 Phase では行わない。
- Copilot の `--global` パス `~/.copilot/skills/` は調査結果に基づくが、VS Code Copilot の挙動は変更され得る。実装時に最新版で動作確認すること。
