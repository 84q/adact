# Testing

ADACT のテストは xUnit と Layer Trait で分類します。詳細方針の参照元は [.github/skills/testing-strategy/SKILL.md](../../.github/skills/testing-strategy/SKILL.md) です。この文書は現行 `tests/Adact.*.Tests/` の構成に合わせたストック版として併せて参照します。

## テストプロジェクト

| Project | 対象 | 主な Layer |
| --- | --- | --- |
| `tests/Adact.Engine.Tests/` | UIA Engine、SnapshotBuilder、RefRegistry、例外、実アプリ smoke | `Unit`, `Integration`, `IntegrationUia`, `Smoke` |
| `tests/Adact.Cli.Tests/` | CLI command、connection、output、snapshot text pipeline、Skill install、CLI E2E | `Unit`, `Integration`, `Smoke`, `E2E` |
| `tests/Adact.Mcp.Common.Tests/` | `WindowsTools`, `WindowRefStore`, lifecycle tools | `Unit` |
| `tests/Adact.Mcp.Http.Tests/` | HTTP daemon smoke / Calculator E2E | `Smoke`, `E2E` |


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
| 対話 session | `adact serve` / UIA smoke は対象 GUI と同じ対話 Windows session で実行する。非対話 SSH session では GUI session 必須テストは skip される |
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

### SSH / 非対話 session から L3+ を実行する

SSH などの非対話 session から L3+ (`IntegrationUia` / `Smoke` / `E2E`) を実行する場合、
実アプリや UIA に触る処理は対話 Windows session 側で動いている必要がある。

CLI / HTTP の Smoke / E2E は `ADACT_SERVER_URL` に外部 daemon の MCP endpoint を設定すると、
テスト process 自身では daemon を起動せず、その URL に接続する。先に Windows の対話 GUI
session 側で `adact serve --port 41300` などを起動しておく。
HTTP の Calculator E2E では、外部 daemon 指定時も `adact_launch` を通して対話 GUI
session 側で Calculator を起動する。

```powershell
$env:ADACT_SERVER_URL = "http://127.0.0.1:41300/mcp"
dotnet test tests/Adact.Cli.Tests/Adact.Cli.Tests.csproj --filter "Layer=Smoke|Layer=E2E"
dotnet test tests/Adact.Mcp.Http.Tests/Adact.Mcp.Http.Tests.csproj --filter "Layer=Smoke|Layer=E2E"
```

`ADACT_SERVER_URL` が設定されている場合、`Adact.Cli.Tests` と `Adact.Mcp.Http.Tests`
はその URL を使い、自前の `adact serve` process や in-process HTTP server は起動・停止しない。
未設定の場合は従来どおり、CLI fixture は一時的な local daemon subprocess を起動し、
HTTP fixture は in-process `WebApplication` を起動する。


test runner 側が直接 UIA や対象アプリを扱うテストは非対話 session では実行対象にしない。
これらは `InteractiveFact` / `InteractiveTestGuard` により対話 desktop がない場合に skip される。

## カバレッジ取得

Layer 別にカバレッジを収集・レポート化する。`.runsettings` に共通除外設定（テスト assembly、
依存ライブラリ）を集約している。

### 前提

```powershell
# 初回のみ: ReportGenerator のグローバルツールを導入
dotnet tool install --global dotnet-reportgenerator-globaltool
```

### Layer 別レポート

```powershell
# Unit のみ（高速）
.\scripts\coverage.ps1 -Layer Unit

# Integration のみ
.\scripts\coverage.ps1 -Layer Integration

# Unit + Integration（開発時の基本）
.\scripts\coverage.ps1 -Layer Unit
.\scripts\coverage.ps1 -Layer Integration

# Smoke / E2E（実アプリ起動を伴う）
.\scripts\coverage.ps1 -Layer Smoke
.\scripts\coverage.ps1 -Layer E2E

# 全 Layer
.\scripts\coverage.ps1
```

各 Layer のレポートは `TestResults/<Layer>/coverage-html/` に出力される。
`index.html` をブラウザで開くとファイル別 / 行ごとのカバー状況が確認できる。

### 手動実行（スクリプトを使わない場合）

```powershell
# Unit + Integration のカバレッジを cobertura で出力
if (Test-Path TestResults) { Remove-Item -Recurse -Force TestResults }
dotnet test adact.sln --filter "Layer=Unit|Layer=Integration" --collect:"XPlat Code Coverage" --results-directory TestResults --settings .runsettings

# HTML レポート生成
reportgenerator `
  "-reports:TestResults/**/coverage.cobertura.xml" `
  "-targetdir:TestResults/coverage-html" `
  "-reporttypes:Html;TextSummary" `
  "-assemblyfilters:+Adact.*;+adact;-Adact.*.Tests"

# サマリ表示
Get-Content TestResults/coverage-html/Summary.txt
```

`TestResults/` は `.gitignore` 済みのため commit されない。CI 用途では `scripts/coverage.ps1` 
または同じ手順で生成可能。

### zero coverage assembly の扱い

`coverlet` は実行されたコードのみを計測対象とする。テストから到達していない assembly 
（例: `Adact.Mcp.Stdio` が Unit/Integration テストに含まれない場合）はレポートに現れない。
これは「未到達 = カバレッジ 0%」とは異なる。Layer 別レポートを比較することで、
「どの Layer でどの assembly が動作確認されているか」を判断する。

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
