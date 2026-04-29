# Phase 8 完了メモ — 操作系 CLI / MCP コマンドの拡充

> 前提: [021_Phase8要件定義.md](021_Phase8要件定義.md) / [022_Phase8設計.md](022_Phase8設計.md)
> 割り込み: [023_launch要件定義.md](023_launch要件定義.md) / [024_launch設計.md](024_launch設計.md)
> 目的: Phase 8 実装の完了記録。

---

## 1. 概要

Phase 8 では AI エージェントが UI 操作を自動化するために必要なコマンド群を拡充した。Phase 5 までは attach / snapshot / click / fill / detach / close / kill / list-apps と最小限だったが、Playwright 流の操作プリミティブを揃え、25 個の CLI コマンド (CLI ↔ MCP 1:1 対応) を追加した。さらに割り込み課題として `launch` (Win32 / UWP アプリ起動) を 1 件追加した。

主な成果:

- **CLI / MCP コマンド +26**: 状態変化系 14、補助系 7、取得・同期系 4、ライフサイクル系 1
- **Engine / MCP の partial 分割**: 機能別ファイルに整理 (`WindowSession.{Mouse,Keyboard,Toggle,Window,Inspect,Wait}.cs` ほか)
- **新規エラーコード 3 種**: `LAUNCH_FAILED` / `INVALID_REF_FORMAT` / `WAIT_TIMEOUT`
- **Skill / docs 全面同期**: references 26 件追加、SKILL.md / docs/spec/ / docs/architecture/ を実装に追従
- **テスト +200 強**: Engine 119 / Mcp.Common 64 / Cli 194 = 377 unit テスト全件パス、build 0 警告 0 エラー

---

## 2. 実装コミット

| # | 種別 | 内容 | Commit |
| --- | --- | --- | --- |
| 1 | docs | 要件定義 (021) と設計 (022) | `a9959ed` |
| 2 | refactor | `MouseTarget` 型 + WindowSession/WindowsTools の partial 化 (Steps 1-3) | `ef6b6ed` |
| 3 | feat | Mouse / Keyboard / Toggle 系 16 コマンド (Step 4) | `3b6579a` |
| 4 | feat | Window state 系 (resize/minimize/maximize/restore) (Step 5) | `3765a9d` |
| 5 | docs | launch 要件定義 (023) / 設計 (024) | `cee5df9` |
| 6 | feat | launch コマンド (Win32 / UWP 対応) | `2ee4d44` |
| 7 | feat | inspect / screenshot (Step 6) | `21cfea1` |
| 8 | feat | wait-for / wait-for-window (Step 7) | `6d5362d` |
| 9 | test | auto-snapshot policy 回帰テスト (Step 8) | `9cd7738` |
| 10 | docs | spec/architecture/SKILL を Phase 8 に同期 (Steps 9-10) | `2b9cd9e` |

---

## 3. 設計からの差分

設計 022 と実装の主要な差分は以下:

| 設計記述 | 実装結果 | 理由 |
| --- | --- | --- |
| `wait-for-window` に `--window-key` を併記 | `--window-key` なし。`--title` / `--class-name` / `--process-name` / `--exe` のみ | Phase 020 の attach 簡略化以降、window-key は廃止済み。設計 022 を実装に合わせて修正 |
| `screenshot --highlight` を将来拡張として明記 | 同上 (実装せず) | FlaUI に専用 API なし、GDI 実装は別 PR で扱う |
| MCP の click/dblclick/hover の position は `position?` 1 個 | 実装は `positionX?` / `positionY?` の別引数 | MCP スキーマでは複合値より分離が扱いやすい。docs を実装に追従 |
| `type` の delay オプション名 `--delay` | 実装は `--delay-ms` | 単位を名前に含める方針。docs を実装に追従 |

launch (割り込み課題):

- UWP の Activation 経路を `IApplicationActivationManager` 経由に確定 (要件 023 / 設計 024 通り)
- 引数エスケープは .NET runtime の `PasteArguments` 規約 (`QuoteIfNeeded`) を採用

---

## 4. 機能サマリ

### 4.1 追加コマンド一覧 (CLI / MCP 1:1 対応)

| 分類 | CLI | auto-snapshot |
| --- | --- | --- |
| 状態変化系 | `click`(拡張), `dblclick`, `hover`, `type`, `check`, `uncheck`, `select`, `clear`, `press`, `mouse-wheel`, `resize`, `minimize`, `maximize`, `restore` | あり (`--no-snapshot` で抑止) |
| 補助系 | `focus`, `scroll-into-view`, `mouse-move`, `mouse-down`, `mouse-up`, `key-down`, `key-up` | なし |
| 取得・同期系 | `inspect`, `screenshot`, `wait-for`, `wait-for-window` | なし |
| ライフサイクル系 | `launch` | なし |

MCP 側はすべて `windows_*` プレフィクスの snake_case 名で公開。

### 4.2 主要な戻り値スキーマ (JSON 1 行)

| ツール | 戻り値 |
| --- | --- |
| `windows_launch` | `{ pid, processName, executablePath }` |
| `windows_wait_for` | `{ ref, state }` |
| `windows_wait_for_window` | `{ processId, processName, windowTitle, controlType, className, nativeWindowHandle }` |
| `windows_inspect` | `{ ref, name, controlType, automationId, className, helpText, value, boundingRect, isEnabled, isOffscreen, isKeyboardFocusable, hasKeyboardFocus, patterns{...} }` |
| `windows_screenshot` | `{ path, width, height }` |

### 4.3 Engine 構造の partial 分割

```
src/Adact.Engine/
  WindowSession.cs              # core (ctor, Snapshot, Click, Fill, Detach)
  WindowSession.Mouse.cs        # mouse-move/down/up/wheel, click 拡張, dblclick, hover
  WindowSession.Keyboard.cs     # press, key-down/up, type
  WindowSession.Toggle.cs       # check, uncheck, select, focus, clear, scroll-into-view
  WindowSession.Window.cs       # resize, minimize, maximize, restore
  WindowSession.Inspect.cs      # inspect
  WindowSession.Wait.cs         # wait-for (要素)
  UiaEngine.WaitForWindow.cs    # wait-for-window (セッション外)
  MouseTarget.cs                # ByRef / ByPoint 共通型
  WaitFor{State,ElementQuery,Result}.cs
  WindowSearchQuery.cs
  Exceptions/WaitTimeoutException.cs
```

MCP 側 (`WindowsTools.{Mouse,Keyboard,Toggle,Window,Inspect,Wait}.cs`) も同構造で揃えた。

### 4.4 新規エラーコード

| Code | 層 | 用途 |
| --- | --- | --- |
| `LAUNCH_FAILED` | Engine→MCP→CLI | Win32 / UWP 起動失敗 |
| `INVALID_REF_FORMAT` | MCP | ref ID の形式不正 (例: `e7` のみ) |
| `WAIT_TIMEOUT` | Engine→MCP→CLI | wait-for / wait-for-window のタイムアウト |

### 4.5 ドキュメント / Skill

- `docs/spec/cli.md`, `docs/spec/mcp-tools.md`: 26 コマンド分の引数・戻り値・エラーを追加
- `docs/spec/errors-and-output.md`: 新規エラーコードと JSON 1 行出力スキーマを追加
- `docs/spec/snapshot.md` / `docs/spec/ref-ids.md`: auto-snapshot 対象一覧と ref ライフサイクルを Phase 8 に追従
- `docs/architecture/{components,class-responsibilities,command-flows,snapshot-pipeline}.md`: partial 分割と新コマンドフローを反映
- `src/Adact.Cli/Skills/adact-cli/`: references 26 件追加、SKILL.md 更新、`InstallCommandTests` / `InstallCommandIntegrationTests` の期待値同期

---

## 5. テスト結果

| プロジェクト | Unit テスト数 |
| --- | --- |
| Adact.Engine.Tests | 119 |
| Adact.Mcp.Common.Tests | 64 |
| Adact.Cli.Tests | 194 |
| **合計** | **377** |

- すべて pass、失敗 0 / スキップ 0
- `dotnet build adact.sln`: 0 警告 / 0 エラー

E2E / Integration テストは Phase 8 範囲では追加せず、既存 (`CalculatorCliE2ETests` 等) の方針を踏襲。

---

## 6. 残課題・将来拡張

- `screenshot --highlight`: GDI 実装による副作用が大きいため別 PR で扱う
- minimize 中の操作: 暫定でエラー返却。`resize` などで自動 restore するかは将来要件次第
- `launch` の作業ディレクトリ・stdin/stdout 制御: 現状は最小機能。将来要件に応じて検討

---

## 7. レビューループの運用

review-loop skill に従い、各 Step で以下を厳格に適用した:

1. 設計の確認 (要件 021 / 設計 022 を参照)
2. Implementation サブエージェントへ委譲し実装
3. `get_errors` で軽い静的検証
4. Research サブエージェントで横断レビュー
5. 指摘の選択的適用 → ビルド / Unit テスト → commit

各 Step は 1 commit で完結させ、段階的に main に積み上げた (10 commits)。割り込みで発生した launch も同様の流れで処理した。
