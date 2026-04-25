---
name: testing-strategy
description: 'ADACT プロジェクトのテスト方針。ユニット・結合・スモーク・E2E の各レベルで何をどの程度確認するかをまとめたガイド。テストコードを書く・追加する・レビューする・テストプロジェクト構成を決める・xUnit Trait でレベル分類する・モック設計する・FlaUI を使う/使わないテストを切り分ける、いずれかの場面で必ず参照する。Phase 2 以降のすべての実装フェーズで適用される。'
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
- Phase ごとに何をテストすべきか確認する

## テストレベル定義

ADACT のテストは以下の 5 レベルに分類する。レベルが上がるほど環境依存が強くなり、実行コストも高くなる。

| レベル | 名称 | 対象 | 環境依存 | 速度 | xUnit Trait |
|---|---|---|---|---|---|
| L1 | ユニット | pure ロジック (フィルタ判定・Ref 採番・JSON 生成・例外型) | なし | 高速 | `Layer=Unit` |
| L2 | コンポーネント結合 (FlaUI 除く) | Engine モジュール間結合 (SnapshotBuilder ↔ RefRegistry ↔ FilterStrategy 等) | なし | 高速 | `Layer=Integration` |
| L3 | コンポーネント結合 (FlaUI 込み) | UIA バックエンドの直叩きを含む結合 | Windows + 起動済アプリ | 中速 | `Layer=IntegrationUia` |
| L4 | スモーク / E2E (Engine) | 起動 → attach → 操作 → 検証 を Engine 通しで実施 | Windows + アプリ自動起動 | 遅い | `Layer=Smoke` |
| L5 | MCP 経由 E2E | MCP クライアント ↔ サーバー ↔ Engine を通す | 同上 + MCP クライアント | 遅い | `Layer=E2E` |

## 各レベルの方針

### L1: ユニット

- **対象**:
  - `IFilterStrategy` 実装の `Decide` / `ExtractProperties`
  - `RefRegistry` の採番・世代管理
  - `SnapshotResult` JSON シリアライズ
  - 独自例外型のメッセージ・コード
  - `AttachQuery` のマッチングロジック (UIA を介さない部分)
- **量**: ロジック分岐をすべて網羅する。1 メソッドあたり数本
- **モック**: 不要。pure function 中心。`AutomationElement` を直接渡さなければならない場合は薄い抽象 (例: `IElement`) を切る
- **速度目標**: 全体で 1 秒以内
- **CI**: 常時実行

### L2: コンポーネント結合 (FlaUI 除く)

- **対象**:
  - `SnapshotBuilder` がフィルタ戦略を呼びながら木構造を組む
  - `RefRegistry` が世代を進める / Snapshot ごとの破棄
  - モーダルダイアログ検出ロジック
  - 複数 Session 並行時の Ref ID 衝突回避
- **量**: 主要シナリオ 5〜10 本
- **モック方針**: UIA 要素は **Engine 内部で抽象化された `IElement` 型** でフェイクする。FlaUI を呼ばずに済む構造にする
  - これは Engine 設計時に「IElement のような抽象を切る」ことを前提とする
- **速度目標**: 全体で数秒以内
- **CI**: 常時実行

### L3: コンポーネント結合 (FlaUI 込み)

- **対象**: 実 UIA 要素を相手にした SnapshotBuilder。実アプリへ attach した状態で snapshot を取り、フィルタが期待通り動くか
- **量**: 電卓 1 本程度。Notepad++ や他アプリへ拡張は必要に応じて
- **準備**: `Process.Start` で対象アプリ起動、テスト後 `Kill`
- **CI**: ローカル / 開発者マシンのみ。CI は Phase 6 以降で検討

### L4: スモーク / E2E (Engine)

- **対象**: `engine.AttachAsync` → `SnapshotAsync` → `ClickAsync` / `FillAsync` → 期待状態の確認 を Engine 通しで実施
- **必須ケース** (Phase 2):
  - 電卓 (UWP 代表): 数字ボタンクリック → 表示が更新される
  - Notepad++ (Win32 代表): メニュー操作 (例: ファイル → 新規) → 期待状態
- **量**: 各アプリ 1〜2 ケース、計 2〜4 本
- **準備**: `Process.Start`、テスト後 `Kill` または明示的クローズ
- **CI**: ローカル / 開発者マシンのみ

### L5: MCP 経由 E2E

- **対象**: MCP クライアントから ADACT MCP サーバーを叩いて、実アプリを操作するまでを通す
- **Phase 2 ではスコープ外**。Phase 3 で stdio MCP の最小スモークを追加、Phase 4 で HTTP MCP のスモーク、Phase 5 でプロキシ経由のスモーク
- **量**: 各 Phase で 1〜2 本

## Phase ごとの取り込み

| Phase | L1 | L2 | L3 | L4 | L5 |
|---|---|---|---|---|---|
| Phase 2 | 必須 | 必須 | 1 本以上 | 必須 (電卓 + Notepad++) | — |
| Phase 3 | 維持 | 維持 | 維持 | 維持 | stdio で 1〜2 本追加 |
| Phase 4 | 維持 | 維持 | 維持 | 維持 | HTTP で追加 |
| Phase 5 | 維持 | 維持 | 維持 | 維持 | proxy 経由で追加 |
| Phase 6 | 拡充 | 拡充 | 拡充 | 拡充 | 拡充 + CI 整備 |

## プロジェクト構成

```
tests/
└── Adact.Engine.Tests/
    ├── Adact.Engine.Tests.csproj
    ├── Unit/                       (L1)
    │   ├── FilterStrategyTests.cs
    │   ├── RefRegistryTests.cs
    │   ├── SnapshotJsonTests.cs
    │   └── ExceptionTests.cs
    ├── Integration/                (L2)
    │   ├── SnapshotBuilderTests.cs (IElement モック使用)
    │   └── ModalDialogDetectionTests.cs
    ├── IntegrationUia/             (L3) — ローカルのみ
    │   └── CalculatorSnapshotTests.cs
    └── Smoke/                      (L4) — ローカルのみ
        ├── CalculatorSmokeTests.cs
        └── NotepadppSmokeTests.cs
```

L5 (MCP 経由) は Phase 3 以降に `tests/Adact.Mcp.Stdio.Tests/` 等を追加。

## 命名規約

### テストクラス名

`<対象クラス名>Tests` (例: `RefRegistryTests`, `SnapshotBuilderTests`)。

スモーク・統合系は対象アプリ名 + 用途で `<アプリ名>SmokeTests` (例: `CalculatorSmokeTests`)。

### テストメソッド名

`<操作>_<前提>_<期待>` 形式 (例: `Issue_AfterReset_ReturnsFirstId`, `Decide_OnUnnamedPane_ReturnsFlatten`)。

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

### IElement 抽象 (L2 のため)

UIA 要素 (`AutomationElement`) を直接扱わず、Engine 内部に薄い抽象を切る:

```csharp
public interface IElement
{
    string? Name { get; }
    string? AutomationId { get; }
    string ControlType { get; }
    string? ClassName { get; }
    bool IsEnabled { get; }
    bool IsOffscreen { get; }
    Rect BoundingRectangle { get; }
    // 必要なプロパティのみ
    IReadOnlyList<IElement> Children { get; }
    void Click();
    void Fill(string text);
}
```

実装:
- `FlaUiElement : IElement` (FlaUI ラップ、production 用)
- `FakeElement : IElement` (テスト用、in-memory ツリー組み立て)

L2 ではすべて `FakeElement` で構成して FlaUI を呼ばない。

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

ADACT 自身は **Phase 2 では `LaunchAsync` を提供しない**。テスト準備でアプリを起動する場合は `System.Diagnostics.Process.Start` を直接呼ぶ。

完成形では `windows_launch` MCP ツールとして提供予定 (Phase 4 or 7)。テストコードでは MCP ツール経由ではなく `Process.Start` を継続使用してよい。

## アンチパターン

- **L1 の中で FlaUI を呼ぶ**: 速度劣化と環境依存を招く。pure ロジックなら必ず L1 にする
- **L2 で FakeElement を作らず FlaUI を直接使う**: これは L3 になる。L2 として書きたいなら抽象化を徹底する
- **L4 を CI 必須にする**: 環境依存で flaky になる。Phase 6 で CI 整備するまでローカル限定
- **テストメソッド名に日本語を使う**: Test Explorer / レポートツールで文字化けの恐れ
- **Trait なしでスモークを混在**: フィルタ実行ができなくなる。必ず Trait を付ける
- **テストアプリを `Kill` し忘れる**: 開発者マシンに残骸が残る。`IDisposable` / `IAsyncLifetime` で確実に cleanup
