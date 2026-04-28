# Phase 7 要件定義 — Snapshot 出力形式の刷新

## 1. 背景・動機

### 1.1 現状の問題

ADACT の snapshot 出力 (JSON minified) は AI / 人間の両方にとって読みづらい。

Phase 7 事前調査で以下を確認:

- **サイズ**: 電卓 (operable filter, 50 要素) で 8.7 KB / 約 2,200 tokens、Explorer (raw, 98 要素) で 39 KB / 約 9,900 tokens
- **キー名の冗長性**: キー名 (`"className"`, `"boundingRect"`, `"isKeyboardFocusable"` 等) だけで全体の **約 44%** を占める
- **default 状態の繰り返し**: `isKeyboardFocusable: true` が 1 snapshot 内で 34 回出現するなど、default 値の冗長な明示
- **Unicode escape**: `\uXXXX` で日本語が `\u96FB\u5353` のように読めない (1 snapshot に 209 回出現する例あり)
- **値の繰り返し**: `"className":"Button"` が 30 回など、低カーディナリティ値の重複
- **可読性**: minified 1 行 JSON で diff/レビュー困難

### 1.2 業界動向 (Phase 7 事前調査)

UIA / アクセシビリティツリーを AI に渡す類似ツールはほぼすべて以下の原則を採用している:

- 1 要素 1 行 + インデントで階層表現
- フィールド厳選、default / false 状態の省略
- 生 Unicode 文字 (escape しない)
- JSON ではなく YAML 風 / カスタムテキスト

代表例: Playwright Aria Snapshot、Playwright MCP `browser_snapshot`、browser-use serializer。Playwright Aria Snapshot 風 YAML が事実上のデファクト。

## 2. スコープ

### 2.1 対象

- **CLI 経由で生成される snapshot ファイル形式の全面変更** (新形式テキストに置換)
- **出力フィールドの厳選** (冗長プロパティ削除、default 状態省略)
- **生 Unicode 文字の保持** (escape しない)
- **1 要素 1 行 + インデント階層** での出力
- snapshot ファイル拡張子・ファイル名規約の見直し (新形式に合わせて)
- フィルタ/形式変換ロジックの **CLI 側への集約** (MCP server はフィルタレス JSON を返す素朴な層に整理)

### 2.2 スコープ外

- snapshot サイズの**数値目標値**は設けない (「無駄がなく必要十分」と感じたら完了。ただし現状値と新形式値の比較測定は実施)
- **MCP プロトコルが返す JSON 形式の刷新** (MCP は機械可読層として現状の JSON スキーマを維持。フィルタを外す変更のみ)
- snapshot 以外の CLI 出力 (key-value、エラー出力、TSV 等) の変更
- snapshot 差分 / 部分取得 / セッション横断キャッシュ等の構造的な仕組み変更 (設計フェーズで議論候補だが本要件のスコープ外)
- recipes (電卓・メモ帳ボックス) — 別 Phase

### 2.3 責任分担 (Phase 7 で確定)

- **MCP server**: window 指定を受けて、UIA から取得した raw 全要素・全フィールドの JSON を返す。`filter` 引数は受け付けない (シンプル化)。プロトコルとしての JSON 形式・フィールドは維持。
- **CLI**: MCP から raw JSON を受信 → ツリーフィルタ (operable/raw) → フィールド選別 → 新形式テキスト化 → `.adact/*.txt` 保存。stdout には path・要素数・root ref 等の key-value を出力。
- データ量: localhost HTTP / 数 MB 規模なので転送・パースコストは実用上問題なし。

## 3. 方針

### 3.1 形式の選定

**業界デファクト (Playwright Aria Snapshot スタイル) を最有力候補とし、設計フェーズで複数案を並べて詳細比較・決定する。**

設計で比較する候補:

- A. Playwright Aria Snapshot 風 YAML (`- ControlType "Name" [ref=...] [flag]`)
- B. browser-use 風タグ表記 (`[idx]<tag attrs>text</tag>`)
- C. その他の案 (設計フェーズで必要に応じて追加)

要件定義では**形式そのもの**は確定しない。

### 3.2 フィールド選定

設計フェーズで再検討する。検討対象:

- 必須フィールド: `ref`, `role` (ControlType)
- 条件付き必須: `name` (空でない場合)
- 任意: `automationId`, `className`, `value`, `helpText`, `boundingRect` ほか
- 状態フラグ: `isEnabled`, `isKeyboardFocusable`, `hasKeyboardFocus`, `isModalDialog` 等 — default を省略する規約を設計で明文化

## 4. 完了判定

1. **正規サンプルでの目視レビュー**: 最低限以下のアプリで snapshot を取得し、人間が「無駄がなく必要十分」と判断できること:
   - 電卓
   - メモ帳
   - エクスプローラー
   - ADACT 自身
2. **AI クライアントでの手動スモーク**: 新形式 snapshot を AI クライアント (Phase 6 の Skill 経由) が読み取って、5 サブコマンドの組み合わせでタスクを達成できることを確認。
3. **サイズ測定の記録**: 現状 JSON vs 新形式のサイズ・推定 token 数を完了メモに記録。

## 5. 設計フェーズへの引き継ぎ事項

- 採用形式の最終決定 (A / B / その他)
- 出力フィールド一覧と default 省略規約
- ref / role / name の表記法 (引用符ルール、空名の扱い等)
- 階層インデント幅 (Playwright は 2 スペース)
- ファイル拡張子・ファイル名規約 (`.adact/session-*.json` をどうするか)
- 出力時の文字エンコーディング (UTF-8 BOM なし継続)
- パーサ実装の要否 (snapshot を再読込する箇所があれば)
- 既存テスト・テストフィクスチャの更新方針
- 段階的移行 vs ビッグバン (Phase 6 完了直後でユーザ少数のため一括置換が現実的)

## 6. 参考データ (Phase 7 事前調査結果)

### 6.1 ホットスポット (期待削減効果順)

事前調査で抽出された主な改善余地:

| # | 改善 | 期待削減 (電卓 8871 base) |
|---|---|---:|
| 1 | キー名短縮 / 階層表現変更 | -25〜30% |
| 2 | default-true 状態省略 | -7〜10% |
| 3 | Unicode escape 解除 | -7% |
| 4 | boundingRect の表現変更 | -4〜6% |
| 5 | 値の辞書化 (`className` 等) | -6〜11% |

組み合わせで **-40% 程度** が現実的見込み (8.7 KB → 約 5 KB)。

### 6.2 業界実例

Playwright Aria Snapshot 例:

```yaml
- banner:
  - heading "Title" [level=1]
  - link "Get started":
    - /url: /docs
- checkbox [checked]
- button "OK" [pressed=true]
```

Playwright MCP の ref 埋込:

```yaml
- button "Submit" [ref=e2]
- button "Submit" [active] [ref=e2]
```
