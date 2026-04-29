# Testing

ADACT のテストは xUnit と Layer Trait で分類します。詳細方針の参照元は [.github/skills/testing-strategy/SKILL.md](../../.github/skills/testing-strategy/SKILL.md) です。この文書は現行 `tests/Adact.*.Tests/` の構成に合わせたストック版として併せて参照します。

## テストプロジェクト

| Project | 対象 | 主な Layer |
| --- | --- | --- |
| `tests/Adact.Engine.Tests/` | UIA Engine、SnapshotBuilder、RefRegistry、例外、実アプリ smoke | `Unit`, `Integration`, `IntegrationUia`, `Smoke` |
| `tests/Adact.Cli.Tests/` | CLI command、connection、output、snapshot text pipeline、Skill install、CLI E2E | `Unit`, `Integration`, `Smoke`, `E2E` |
| `tests/Adact.Mcp.Common.Tests/` | `WindowsTools`, `WindowRefStore`, lifecycle tools | `Unit` |
| `tests/Adact.Mcp.Http.Tests/` | HTTP daemon smoke / Calculator E2E | `Smoke`, `E2E` |
| `tests/Adact.Mcp.Stdio.Tests/` | stdio MCP E2E | `E2E` |

## Layer Trait

| Layer | 用途 | 環境依存 | 例 |
| --- | --- | --- | --- |
| `Unit` | pure logic、DTO、formatter、validation、store | なし | `RefRegistryTests`, `SnapshotTextFormatterTests`, `WindowRefStoreTests` |
| `Integration` | FakeElement 等による component integration | 低 | `SnapshotBuilderTests`, `InstallCommandIntegrationTests` |
| `IntegrationUia` | FlaUI / UIA を直接使う結合 | Windows + 実アプリ | `CalculatorSnapshotTests` |
| `Smoke` | CLI / Engine / HTTP daemon の軽量実行確認 | Windows + 実アプリまたは daemon | `AdactCliSmokeTests`, `CalculatorSmokeTests` |
| `E2E` | MCP / CLI / daemon / 実アプリを通す end-to-end | 高 | `CalculatorCliE2ETests`, `CalculatorHttpE2ETests`, `WindowsToolsE2ETests` |

Trait は `[Trait("Layer", "Unit")]` のように指定します。実アプリを扱うテストはローカル開発者マシンでの実行を前提にします。

## 実アプリ E2E の注意点

| 項目 | 注意 |
| --- | --- |
| 対話 session | `adact serve` / `adact local` / UIA smoke は対象 GUI と同じ対話 Windows session で実行する |
| Calculator | 複数 test assembly が Calculator を使うため、named semaphore `Global\AdactCalculatorE2E` で直列化している |
| Notepad++ | Win32 代表の smoke 対象。インストール有無や環境差に注意する |
| UIA focus | click/fill は foreground や focus に影響されるため、実行中に人間が同じ desktop を触ると flaky になりうる |
| cleanup | 起動した実アプリは `IDisposable` / fixture で close または kill する |
| CI | `Unit` / `Integration` は常時実行候補。実アプリ系は CI 環境が整うまでローカル中心 |

## UIA テストの直列化

`Adact.Engine.Tests` では、L3 (`IntegrationUia`) と L4 (`Smoke`) を `[Collection("UiaSerial")]` で直列化します。L1 / L2 は並列実行可能です。

ADACT 本体も daemon 内で UIA 操作を直列化しますが、テスト process や実アプリ起動は test runner 側の並列性の影響を受けるため、Layer と collection の両方で制御します。

## 基本コマンド

```powershell
# build
dotnet build adact.sln

# 速いテスト: Unit のみ
dotnet test --filter Layer=Unit

# 開発時の基本: Unit + Integration
dotnet test --filter "Layer=Unit|Layer=Integration"

# UIA を含む結合
dotnet test --filter Layer=IntegrationUia

# 実アプリ smoke
dotnet test --filter Layer=Smoke

# E2E
dotnet test --filter Layer=E2E

# 全体
dotnet test
```

PowerShell では `|` を含む filter は quote してください。

## カバレッジ取得

`coverlet.collector` (各テストプロジェクトに導入済み) と `dotnet-reportgenerator-globaltool` を使う。

```powershell
# 初回のみ: ReportGenerator のグローバルツールを導入
dotnet tool install --global dotnet-reportgenerator-globaltool

# Unit + Integration のカバレッジを cobertura で出力
if (Test-Path TestResults) { Remove-Item -Recurse -Force TestResults }
dotnet test adact.sln --filter "Layer=Unit|Layer=Integration" --collect:"XPlat Code Coverage" --results-directory TestResults

# HTML レポート生成 (production code のみ。adact.dll の小文字も拾う)
reportgenerator `
  "-reports:TestResults/**/coverage.cobertura.xml" `
  "-targetdir:TestResults/coverage-html" `
  "-reporttypes:Html;TextSummary" `
  "-assemblyfilters:+Adact.*;+adact;-Adact.*.Tests"

# サマリ表示
Get-Content TestResults/coverage-html/Summary.txt

# ブラウザで詳細 (ファイル別 / 行ごとのカバー / 未カバー強調)
Start-Process TestResults/coverage-html/index.html
```

`TestResults/` は `.gitignore` 済みのため commit されない。CI 用途では同じ手順で生成可能。

## テスト追加時の目安

| 変更内容 | 追加・更新するテスト |
| --- | --- |
| validation / formatter / parser | `Unit` |
| Engine `SnapshotBuilder` の raw JSON tree 構築 | FakeElement を使う `Integration` |
| CLI parser / filter / formatter | `Unit` |
| UIA の実挙動 | 最小限の `IntegrationUia` または `Smoke` |
| CLI command 追加 | CLI `Unit`、必要なら `Smoke` / `E2E` |
| MCP tool 追加 | `Adact.Mcp.Common.Tests` の `Unit` と transport 別 E2E |
| CLI/MCP subcommand 追加・改名 | `InstallCommandTests` の Skill 同期対象も更新 |

## 参照

| 文書 | 内容 |
| --- | --- |
| [../../.github/skills/testing-strategy/SKILL.md](../../.github/skills/testing-strategy/SKILL.md) | ADACT テスト戦略の詳細 |
| [../architecture/components.md](../architecture/components.md) | production / test project の対応 |
| [../../discussion/010_Phase5_完了.md](../../discussion/010_Phase5_完了.md) | Phase 5 時点のテスト状況 |
| [../../discussion/017_Phase7_完了.md](../../discussion/017_Phase7_完了.md) | Phase 7 時点のテスト状況 |
