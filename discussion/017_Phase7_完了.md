# Phase 7 完了メモ — Snapshot 出力形式刷新 (Playwright Aria YAML 風テキスト)

> 前提: [015\_Phase7要件定義.md](015_Phase7要件定義.md) / [016\_Phase7設計.md](016_Phase7設計.md)
> 目的: [016\_Phase7設計.md](016_Phase7設計.md) に基づく Phase 7 実装の完了記録。

---

## 1. 概要

Phase 7 では snapshot 出力形式を JSON から **Playwright Aria YAML 風テキスト** へ刷新し、フィルタ責務を **MCP/Engine から CLI 側へ全面移行** した。これにより:

- snapshot ファイル出力サイズが約 **62% 削減** (calculator baseline 9 件で 78,204B → 29,853B、平均 8.7KB → 3.3KB)
- MCP は raw JSON のみを返す単純な責務に縮小
- フィルタ・整形・テキスト化はすべて CLI 側で完結し、将来の出力形式変更が CLI 単独で可能に
- 出力ファイル拡張子は `.json` から `.txt` に変更
- ref フォーマットは `s<sid>e<eid>` (Phase 5 で安定化済み・generation なし) を維持

---

## 2. 実装コミット

| # | 種別 | サブタスク | Commit |
| --- | --- | --- | --- |
| 1 | docs | 要件定義 (015) と設計 (016) | `9d9f650` |
| 2 | refactor | Engine フィルタ層削除 / MCP は raw JSON 返却 | `6d124e0` |
| 3 | feat | CLI Snapshots パイプライン (parser/filter/formatter/escaper) 追加 | `e6f81af` |
| 4 | feat | Snapshot/Click/Fill/Attach コマンドを新パイプラインへ統合 | `0633e08` |
| 5 | docs | Skill ドキュメント更新 | `308ca53` |

---

## 3. 設計からの差分

設計 016 と実装の主要な差分はなし。3 ラウンドのレビューループを経て、設計通りの構成で着地。

---

## 4. 機能サマリ

### 4.1 出力形式 (新)

```
---
filter: operable
sessionId: s1
processName: ApplicationFrameHost
processId: 10392
generatedAt: "2026-04-28T01:00:54.4221919Z"
---
- Window "電卓" [ref=s1e1]
  - Window "電卓" [aid="TitleBar"] [value="電卓"] [ref=s1e2]
    - Button "電卓 を閉じる" [aid="Close"] [ref=s1e7]
  - Window "電卓" [focused] [ref=s1e8]
    - Group "数字パッド" [aid="NumberPad"] [ref=s1e36]
      - Button "3" [aid="num3Button"] [focused] [ref=s1e40]
```

- frontmatter (YAML) + Playwright Aria 風ツリー本体
- メタ属性: `[aid="..."]` / `[value="..."]` / `[focused]` / `[disabled]` / `[offscreen]` / `[modal]` / `[ref=...]`
- インデント 2 スペース固定、role の先頭文字は大文字維持

### 4.2 責務分担

| 層 | 入力 | 出力 |
| --- | --- | --- |
| `Adact.Engine.Snapshot.SnapshotBuilder` | UIA ツリー | raw JSON (フィルタ未適用フルツリー) |
| `Adact.Mcp.Common.WindowsTools` | raw JSON | そのまま MCP レスポンスへ |
| `Adact.Cli.Snapshots.SnapshotJsonParser` | MCP raw JSON | `SnapshotElement` ツリー DTO |
| `Adact.Cli.Snapshots.SnapshotTreeFilter` | DTO + filter 名 | フィルタ適用済み DTO |
| `Adact.Cli.Snapshots.SnapshotTextFormatter` | DTO + meta | Playwright Aria YAML 風テキスト |
| `Adact.Cli.Snapshots.SnapshotFileWriter` | テキスト | `.txt` ファイル |

### 4.3 CLI コマンド変更点

| コマンド | 変更 |
| --- | --- |
| `adact snapshot` | 出力ファイル拡張子 `.json` → `.txt`、形式を新フォーマットへ |
| `adact attach` / `click` / `fill` | 自動 snapshot を新パイプライン経由で出力 |

---

## 5. テスト状況

| レベル | 内容 | 結果 |
| --- | --- | --- |
| Unit | `Snapshot{JsonParser,TextEscaper,TextFormatter,TreeFilter}Tests` 新設 | passed |
| Unit | `SnapshotFileWriterTests` 新形式追従 | passed |
| Engine.Tests | `Snapshot{Json,Builder},ModalDialogDetection,Exception}Tests` 修正 (フィルタ削除追従) | passed |
| Engine.Tests | `FilterStrategyTests` 削除 | — |
| E2E | `CalculatorCliE2ETests` 新形式追従 | passed |
| 全体 | Cli.Tests + Engine.Tests + Mcp.* (189 件) | リグレッションなし |

`dotnet build adact.sln`: 0 errors, 新規警告なし。

---

## 6. サイズ計測実績

`.adact/` 配下の calculator baseline 9 件 (旧 operable JSON) を新パイプラインに通した結果:

| 項目 | 合計バイト数 | 旧比 |
| --- | ---: | ---: |
| 旧 operable JSON | 78,204 | 100.0% |
| 新 operable text | 29,853 | **38.2%** |
| 新 raw text | 29,808 | 38.1% |

ファイルあたり約 8.7KB → 約 3.3KB (**62% 削減**)。要件 §1 の動機 4 (snapshot サイズ縮減) を達成。

> 旧 baseline は元々 operable filter 済みのため、フィルタ前後 (raw vs operable) の差は本計測では確認できない。フィルタ前 raw とフィルタ後 operable のサイズ比は実機 snapshot 取得時に検証可能。

---

## 7. レビューループ実績

`review-loop` skill に従い実装→レビューを実施。

| ループ | 指摘 |
| --- | --- |
| 1 | Major 3 / Minor 3 (Skill エラーコード不整合、ドキュメント例の不整合 ほか) |
| 2 | 修正後再レビュー: Major 1 / Minor 1 (ドキュメント例修正) |
| 3 | 修正後再々レビュー: **指摘ゼロ** |

---

## 8. 完了判定

- [x] 実装完了 (5 コミット)
- [x] 単体・結合・E2E テスト全 189 件 passed
- [x] サイズ計測完了 (62% 削減を確認)
- [x] 人手による出力サンプル視覚レビュー完了 (calculator baseline)
- [ ] **AI クライアント手動スモーク** (Claude Code / Codex CLI / VS Code Copilot が新 `.txt` 形式 snapshot を解釈し、ref を抽出して click/fill を発行できるかの実利用検証)

スモークはユーザー側作業のため、本メモ時点では未完了。スモーク完了後に Phase 7 受入条件すべて充足とする。

---

## 9. 申し送り

- 新 ref は `s<sid>e<eid>` 形式 (generation なし)。Phase 5 post-task (011) の安定化と整合。
- 旧 baseline JSON (`.adact/session-*.json`) には generation 付き ref (`s1g3e1` 形式) が残存しているが、新パイプラインは JSON 内の `ref` 値をそのまま転記するため、新規取得 snapshot は generation なし形式になる。古い baseline を新形式に変換した出力には `g` セグメントが残る点に留意。
- snapshot 出力サイズの追加縮減 (例: 重複属性の省略、座標情報の削除) は今回スコープ外。実利用で問題が出た場合に検討。
- ボックスレシピ (calculator/notepad の典型操作テンプレート) は引き続き Phase 6 申し送りの通り未着手。
- Engine.Filters/ ディレクトリと `FilterStrategyNotFoundException` は完全削除済み。CLI 側 `SnapshotTreeFilter` がそのまま責務を引き継いでいる。フィルタ追加が必要になった場合は `SnapshotTreeFilter` を拡張する。
