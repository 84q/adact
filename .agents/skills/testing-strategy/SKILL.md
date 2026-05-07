---
name: testing-strategy
description: "ADACT プロジェクトの現行テスト方針。ユニット・結合・スモーク・E2E の Layer Trait と test project 対応、モック設計、FlaUI / 実アプリ起動を伴うテストの切り分けを確認する場面で必ず参照する。"
---

# ADACT テスト戦略

## When to Use

以下のいずれかに該当する場合、必ずこのスキルを読み込んでから作業すること:

- 新しいテストコードを書く / 既存テストを修正する
- テストプロジェクトの追加・分割を検討する
- モック / スタブ / フェイクの設計を決める
- どのレベルのテストを書くか迷う
- xUnit の `Trait` 分類・命名規約を確認したい
- FlaUI / 実アプリ起動を伴うテストを書く・実行する
- CI でどのテストを動かすか判断する
- 現行 test project と `Layer` Trait の対応を確認する

## テストレベル定義

ADACT のテストは以下の 5 レベルに分類する。レベルが上がるほど環境依存が強くなり、実行コストも高くなる。

| レベル | 名称                            | 対象                                                                        | 環境依存                 | 速度 | xUnit Trait            |
| ------ | ------------------------------- | --------------------------------------------------------------------------- | ------------------------ | ---- | ---------------------- |
| L1     | ユニット                        | pure ロジック (Ref 採番・失効管理、snapshot parser / filter / formatter、DTO、例外型) | なし                     | 高速 | `Layer=Unit`           |
| L2     | コンポーネント結合 (FlaUI 除く) | Engine snapshot 構築、CLI snapshot text pipeline、command / store 結合       | なし                     | 高速 | `Layer=Integration`    |
| L3     | コンポーネント結合 (FlaUI 込み) | UIA バックエンドの直叩きを含む結合                                          | Windows + 起動済アプリ   | 中速 | `Layer=IntegrationUia` |
| L4     | スモーク / E2E (Engine)         | 起動 → attach → 操作 → 検証 を Engine 通しで実施                            | Windows + アプリ自動起動 | 遅い | `Layer=Smoke`          |
| L5     | MCP / CLI 経由 E2E              | MCP クライアントまたは CLI client ↔ daemon ↔ Engine を通す                  | 同上 + MCP / CLI client  | 遅い | `Layer=E2E`            |

## 各レベルの方針

### L1: ユニット

- **対象**:
  - CLI 側 `SnapshotTreeFilter` / `SnapshotTextFormatter` の filter・整形ロジック
  - CLI 側 `SnapshotJsonParser` の raw snapshot JSON parse
  - `RefRegistry` の安定 ref 採番・失効管理 (`s<sid>e<eid>`、generation なし)
  - snapshot DTO / parser / formatter の境界表現
  - 独自例外型のメッセージ・コード
- **量**: ロジック分岐をすべて網羅する。1 メソッドあたり数本
- **モック**: 不要。pure function 中心。UIA 要素が必要な構築ロジックは L2 で `Adact.Engine.Elements.IElement` / `FakeElement` を使う
- **速度目標**: 全体で 1 秒以内
- **CI**: 常時実行

### L2: コンポーネント結合 (FlaUI 除く)

- **対象**:
  - `SnapshotBuilder` が Engine raw snapshot JSON の木構造を組む
  - `RefRegistry` が RuntimeId ベースで ref を再利用し、消えた要素を失効させる
  - CLI parser → `SnapshotTreeFilter` → `SnapshotTextFormatter` の snapshot text pipeline
  - モーダルダイアログ検出ロジック
  - 複数 Session 並行時の Ref ID 衝突回避
- **量**: 主要シナリオ 5〜10 本
- **モック方針**: UIA 要素は Engine 内部の `IElement` / `FakeElement` でフェイクする。FlaUI を呼ばずに済む構造にする
- **速度目標**: 全体で数秒以内
- **CI**: 常時実行

### L3: コンポーネント結合 (FlaUI 込み)

- **対象**: 実 UIA 要素を相手にした SnapshotBuilder。実アプリへ attach した状態で raw snapshot を取り、UIA 由来の tree / properties が期待通り出るか
- **量**: 電卓 1 本程度。Notepad++ や他アプリへ拡張は必要に応じて
- **準備**: `Process.Start` で対象アプリ起動、テスト後 `Kill`
- **CI**: ローカル / 開発者マシンのみ。CI 環境が整うまでは常時実行対象にしない

### L4: スモーク / E2E (Engine)

- **対象**: `engine.AttachAsync` → `SnapshotAsync` → `ClickAsync` / `FillAsync` → 期待状態の確認 を Engine 通しで実施
- **代表ケース**:
  - 電卓 (UWP 代表): 数字ボタンクリック → 表示が更新される
  - Notepad++ (Win32 代表): メニュー操作 (例: ファイル → 新規) → 期待状態
- **量**: 各アプリ 1〜2 ケース、計 2〜4 本
- **準備**: `Process.Start`、テスト後 `Kill` または明示的クローズ
- **CI**: ローカル / 開発者マシンのみ

### L5: MCP / CLI 経由 E2E

- **対象**: MCP クライアントまたは CLI client から ADACT (`adact serve` + HTTP daemon) を叩いて、実アプリを操作するまでを通す
- **テストプロジェクト**: `tests/Adact.Mcp.Http.Tests/`、`tests/Adact.Cli.Tests/` の transport / client 別 E2E
- **起動方式**: HTTP / CLI は `adact serve` を対話 Windows session 側で起動し、client が `/mcp` に接続する
- **クライアント SDK**: 公式 `ModelContextProtocol` C# SDK のクライアント API (`ModelContextProtocol.Client` 名前空間) を使用。生 JSON-RPC を手で組まない
- **対象アプリの起動**: L4 と同じく test fixture 側で `Process.Start` を使う。現行実装に `launch` / `adact_launch` はない
- **代表ケース**:
  - `adact_list_windows` ツールを呼んで結果が取得できること
  - 電卓を起動 → `adact_attach` → `adact_snapshot` → tree に Button が含まれること
  - CLI client / HTTP daemon 経由で attach、snapshot、click などの主要フローが通ること
- **量**: transport / client ごとの代表フローを薄く保ち、重い網羅は L1 / L2 に寄せる
- **命名規約・Trait**: L1〜L4 と同じ (`<操作>_<前提>_<期待>` 形式、`Trait("Layer", "E2E")`)

## Layer Trait と test project の対応

| Project | 対象 | 主な Layer |
| --- | --- | --- |
| `tests/Adact.Engine.Tests/` | UIA Engine、SnapshotBuilder、RefRegistry、例外、実アプリ smoke | `Unit`, `Integration`, `IntegrationUia`, `Smoke` |
| `tests/Adact.Cli.Tests/` | CLI command、connection、output、snapshot text pipeline、Skill install、CLI E2E | `Unit`, `Integration`, `Smoke`, `E2E` |
| `tests/Adact.Mcp.Common.Tests/` | `WindowsTools`、`WindowRefStore`、lifecycle tools | `Unit` |
| `tests/Adact.Mcp.Http.Tests/` | HTTP daemon smoke / Calculator E2E | `Smoke`, `E2E` |

実装フェーズの履歴ではなく、変更対象がどの Layer / project に属するかで追加・更新するテストを決める。`Unit` / `Integration` は常時実行候補、実アプリを扱う `IntegrationUia` / `Smoke` / `E2E` はローカル開発者マシン中心で扱う。

## プロジェクト構成

```
tests/
├── Adact.Engine.Tests/
│   ├── Unit/                       (L1: RefRegistry、snapshot DTO、例外)
│   ├── Integration/                (L2: SnapshotBuilder、modal detection)
│   ├── IntegrationUia/             (L3: ローカルのみ)
│   └── Smoke/                      (L4: ローカルのみ)
├── Adact.Cli.Tests/                (L1/L2/L4/L5: command、snapshot text pipeline、CLI E2E)
├── Adact.Mcp.Common.Tests/         (L1: tools / store / lifecycle)
├── Adact.Mcp.Http.Tests/           (L4/L5: HTTP daemon smoke / E2E)
```

L5 (MCP / CLI 経由) は transport ごとの test project で扱う。HTTP / CLI は `adact serve` で起動した daemon に接続する。HTTP / CLI は `adact serve` で起動した daemon に接続する。詳細は L5 の項参照。

## 命名規約

### テストクラス名

`<対象クラス名>Tests` (例: `RefRegistryTests`, `SnapshotBuilderTests`)。

スモーク・統合系は対象アプリ名 + 用途で `<アプリ名>SmokeTests` (例: `CalculatorSmokeTests`)。

### テストメソッド名

`<操作>_<前提>_<期待>` 形式 (例: `Issue_AfterReset_ReturnsFirstId`, `Apply_OnUnnamedPane_ReturnsFlatten`)。

日本語メソッド名は使わない (xUnit / Test Explorer の互換性)。

### Trait 付与

```csharp
[Trait("Layer", "Unit")]      // L1
[Trait("Layer", "Integration")] // L2
[Trait("Layer", "IntegrationUia")] // L3
[Trait("Layer", "Smoke")]     // L4
[Trait("Layer", "E2E")]       // L5
```

クラスレベルでまとめて付与してよい。

## モック方針

### IElement / FakeElement (L2 のため)

UIA 要素 (`AutomationElement`) を直接扱わず、Engine 内部の抽象 `Adact.Engine.Elements.IElement` を使う。production では FlaUI ラップ実装、テストでは `tests/Adact.Engine.Tests/FakeElement.cs` の `FakeElement` で in-memory ツリーを組み立てる。

L2 では `FakeElement` を使って FlaUI を呼ばない。`IElement` の形を説明するための疑似 interface はここに重複させず、現行コードを参照する。

### モックライブラリ

xUnit と組み合わせる場合は `NSubstitute` または `Moq` を採用。L2 のテストでは原則として `FakeElement` をプログラマティックに組むほうが読みやすいので、モックライブラリは限定的に使用する。

## 実行コマンド

```powershell
# L1 (ユニット) のみ — CI 用
dotnet test --filter Layer=Unit

# L1 + L2 — 開発時に頻繁に
dotnet test --filter "Layer=Unit|Layer=Integration"

# L4 (スモーク) — ローカルでのみ実行
dotnet test --filter Layer=Smoke

# 全部
dotnet test
```

## アプリ起動の扱い

現行 ADACT は `launch` / `adact_launch` を提供しない。テスト準備で実アプリを起動する場合は fixture 側で `System.Diagnostics.Process.Start` を直接呼ぶ。

`launch` は Phase 8-A 候補として `docs/roadmap/phase8-and-beyond.md` に整理済み。テストコードでは現行どおり MCP ツール経由ではなく `Process.Start` を使う。

## アンチパターン

- **L1 の中で FlaUI を呼ぶ**: 速度劣化と環境依存を招く。pure ロジックなら必ず L1 にする
- **L2 で FakeElement を作らず FlaUI を直接使う**: これは L3 になる。L2 として書きたいなら抽象化を徹底する
- **L4 を CI 必須にする**: 環境依存で flaky になる。CI 環境が整うまではローカル限定
- **テストメソッド名に日本語を使う**: Test Explorer / レポートツールで文字化けの恐れ
- **Trait なしでスモークを混在**: フィルタ実行ができなくなる。必ず Trait を付ける
- **テストアプリを `Kill` し忘れる**: 開発者マシンに残骸が残る。`IDisposable` / `IAsyncLifetime` で確実に cleanup
