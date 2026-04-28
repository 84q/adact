# Phase 7 設計 — Snapshot 出力形式の刷新

要件定義: [discussion/015_Phase7要件定義.md](./015_Phase7要件定義.md)

## 1. 概要

CLI が生成する snapshot ファイルを、**Playwright Aria Snapshot 風 YAML テキスト**に全面置換する。フィルタリング (operable/raw 切替・フィールド選別) はすべて CLI 側に集約し、MCP server は raw 全要素・全フィールドの JSON を返す素朴な層に整理する。1 要素 1 行 + インデント階層、フィールド厳選、default 状態省略、生 Unicode 保持を実現し、AI / 人間の両方に読みやすい形式にする。

## 2. 責任分担

| 層 | 入力 | 処理 | 出力 |
|---|---|---|---|
| **MCP server** (`Adact.Mcp.*` / `Adact.Engine`) | window handle | UIA WalkAll | raw JSON (フィルタなし、全フィールド) |
| **CLI** (`Adact.Cli`) | raw JSON, `--filter operable\|raw` | ツリーフィルタ → フィールド選別 → テキスト整形 | `.adact/*.txt` (新形式) + stdout key-value |

設計上の含意:

- MCP の `snapshot` tool 引数から `filter` を **削除** (またはサーバ側で無視)
- 既存の `OperableFilterStrategy` / `RawFilterStrategy` ロジックは **CLI 側に移設** (もしくは `Adact.Engine` 内に残しつつ CLI から呼び出す。実装フェーズで判断)
- `Adact.Engine.SnapshotBuilder` は raw JSON 生成までで終了。テキスト整形は CLI 内の新規クラスで実施
- snapshot ファイルの書き出し責任も CLI 側 (`Adact.Engine.SnapshotFileWriter` も CLI 側へ移すか、CLI 内で同等処理を行う)

## 2. 採用形式

### 2.1 全体例

```text
---
filter: operable
sessionId: s1
processName: ApplicationFrameHost
processId: 12345
generatedAt: 2026-04-28T03:42:20.900Z
---
- Window "電卓" [aid=Calculator] [ref=s1e0]
  - Group "ディスプレイ" [aid=DisplayArea] [ref=s1e1]
    - Text "0" [aid=CalculatorResults] [value="表示は 0 です"] [ref=s1e2]
  - Group "数字キーパッド" [aid=NumberPad] [ref=s1e9]
    - Button "1" [aid=num1Button] [ref=s1e10]
    - Button "2" [aid=num2Button] [ref=s1e11]
    - Button "ゼロ" [aid=num0Button] [ref=s1e19]
  - Edit [aid=NumberPadInput] [ref=s1e30]
```

### 2.2 構造

- **Frontmatter**: ファイル先頭、`---` で囲んだ YAML キー値ペア (§3 参照)
- **本体**: 各要素 1 行、行頭 `- `、子要素は親より **2 スペース** 深いインデント
- ルート要素もハイフン形式で 1 つだけ存在

### 2.3 1 要素の構造 (BNF 風)

```
"  " * depth "- " role [ " \"" name "\"" ] *attribute
attribute = " [" key "=" value "]" | " [" flag "]"
```

属性の出現順:

1. `[aid=...]` (常出し、空でなければ)
2. `[value=...]` (空でなければ)
3. 状態フラグ群 (`[disabled]` / `[focused]` / `[modal]`)
4. `[ref=...]` (**末尾固定**、必須)

### 2.4 出力フィールド一覧

| フィールド | 出力位置 | 出力条件 | 表記 |
|---|---|---|---|
| `role` (ControlType) | 行頭 (ハイフン直後) | 必須 | `Button` のように生文字 |
| `name` | role の直後 | 空でなければ | `"..."` ダブルクォート |
| `automationId` | 第 1 属性 | 空でなければ (常出し) | `[aid=...]` |
| `value` | 第 2 属性 | 空でなければ | `[value=...]` |
| `isEnabled` | 状態フラグ | false 時のみ | `[disabled]` |
| `hasKeyboardFocus` | 状態フラグ | true 時のみ | `[focused]` |
| `isModalDialog` | 状態フラグ | true 時のみ | `[modal]` |
| `ref` | 末尾固定 | 必須 | `[ref=s1e0]` |

**出力しないフィールド**:

- `className` (UIA セレクタとして外部で使う場合のみ価値、現状価値低)
- `helpText` (ノイズ大)
- `boundingRect` (ref で指定するため不要)
- `isKeyboardFocusable` (default-true で繰り返し冗長)
- `isOffscreen` (フィルタで除外済み)

### 2.5 引用符・エスケープ規則

`name` / `aid` / `value` は**常にダブルクォート**で囲む (空文字列でなければ)。

エスケープ:
- 内部の `"` → `\"`
- `\` → `\\`
- 改行 (LF) → `\n`
- タブ → `\t`
- それ以外の制御文字 → `\uXXXX`
- それ以外の通常文字 (日本語含む) → **生のまま** (Unicode escape しない)

### 2.6 ref フォーマット

Phase 5 で導入した `s<sid>e<eid>` をそのまま使用。例: `s1e10`。

## 3. Frontmatter 仕様

### 3.1 出力するキー

| キー | 値の例 | 説明 |
|---|---|---|
| `filter` | `operable` / `raw` | 適用フィルタ |
| `sessionId` | `s1` | セッション ID |
| `processName` | `ApplicationFrameHost` | プロセス名 |
| `processId` | `12345` | プロセス ID |
| `generatedAt` | `2026-04-28T03:42:20.900Z` | ISO-8601 UTC |

### 3.2 出力しないもの

- `windowTitle`: ルート要素の `name` で代用
- `options.maxDepth`: 内部値、利用者には不要
- `modalDialog` 一覧: ツリー内の `[modal]` フラグで十分

### 3.3 値の引用符

YAML 慣例に従い、英数字スペースのみなら裸、それ以外は `"..."` で囲む。frontmatter 内の `processName` 等で日本語が現れる場合はクォート。

## 4. ファイル仕様

### 4.1 拡張子

`.txt`

(YAML 風だが厳密 YAML ではないため `.yaml` を避け、テキスト系として `.txt` を採用)

### 4.2 ファイル名規約

現状: `.adact/session-<sid>-<yyyyMMddTHHmmssfff>.json`
新規: `.adact/session-<sid>-<yyyyMMddTHHmmssfff>.txt`

(拡張子のみ変更、パターンは継続)

### 4.3 文字エンコーディング

UTF-8、BOM なし、改行 LF (現状継続)

## 5. パーサー

ADACT 自身は snapshot を **再読込しない**。出力のみ。パーサ実装は本 Phase では行わない。

外部ツール (AI クライアント等) は出力をテキストとして読む。テキスト形式である利点を活かし、AI が直接理解する。

## 6. 影響範囲

### 6.1 主要変更ファイル (見込み)

| ファイル | 変更内容 |
|---|---|
| `Adact.Mcp.Common/WindowsTools.cs` (snapshot tool) | `filter` 引数を削除/無視。常に raw 全要素 JSON を返す |
| `Adact.Engine/Snapshot/SnapshotBuilder.cs` | raw 全フィールド JSON を返すように整理 (フィルタ呼出を取り除く) |
| `Adact.Engine/Filters/OperableFilterStrategy.cs` | CLI 側へ移設 or CLI から呼び出し可能に整理 |
| `Adact.Engine/Filters/RawFilterStrategy.cs` | 同上 |
| `Adact.Engine/Snapshot/SnapshotFileWriter.cs` | 拡張子変更 / CLI 側に責任移譲するなら本クラス削除も検討 |
| `Adact.Cli/...` (新規) | JSON parser、ツリーフィルタ呼出、フィールド選別、テキスト整形、`.adact/*.txt` 書き出し |
| `Adact.Cli/...` (新規) | エスケープヘルパ (`SnapshotTextEscaper` 等) |
| `Adact.Cli/Skills/adact-cli/references/snapshot.md` | 新形式の出力例に更新 |

実装フェーズでクラス配置の最終判断を行う (Engine 側に残すか CLI に移すか)。

### 6.2 テストフィクスチャ

- `tests/Adact.Engine.Tests/` 配下の snapshot 関連テスト: MCP/Engine が返す **raw JSON** のテストとして整理 (フィルタなし前提に書き換え)
- `tests/Adact.Cli.Tests/` 配下に **新形式テキストの assertion** を追加 (CLI が新形式を生成すること)
- 必要なら `tests/` 配下に形式サンプル fixture を新設

### 6.3 既存 `.adact/session-*.json` の扱い

`.gitignore` で `.adact/` 除外済み (Phase 5 確認) のため git 影響なし。実機の旧 snapshot は手動削除 or 放置 (新 snapshot は `.txt`)。

## 7. テスト戦略

### 7.1 Unit テスト

- 各フィールドの出力ルール (空 name / aid 不在 / value あり / 各状態フラグ)
- エスケープ処理 (`"` / `\n` / 通常 Unicode)
- インデント深さ
- ref 末尾固定
- frontmatter 出力

### 7.2 Integration テスト

- 既存の `CalculatorSnapshotTests` 等を新形式 fixture で更新
- raw / operable 両フィルタで一通り動くこと

### 7.3 サイズ計測

完了メモに記録するため、以下の代表サンプルで現状 vs 新形式のサイズを測定:

- 電卓 (operable)
- メモ帳 (operable)
- Explorer (operable)
- ADACT 自身 (operable)

## 8. 完了判定

[015 §4](./015_Phase7要件定義.md) に準拠:

1. 上記 4 アプリで snapshot 取得 → 「無駄がなく必要十分」と人間が判断
2. AI クライアント (Phase 6 Skill 経由) で読み取って 5 サブコマンドを使ったタスクが達成できることを手動スモーク
3. 現状 vs 新形式のサイズ比較を完了メモに記録

## 9. 実装計画

| ステップ | 内容 |
|---|---|
| 1 | MCP `snapshot` tool から `filter` 引数を削除/無視。常に raw 全要素 JSON を返すように調整 |
| 2 | `SnapshotBuilder` を raw JSON 生成専用に整理 |
| 3 | CLI 側に snapshot 専用の処理層を追加: JSON 受信 → ツリーフィルタ → フィールド選別 → テキスト整形 |
| 4 | エスケープヘルパ (`SnapshotTextEscaper` 等) |
| 5 | CLI が `.adact/*.txt` に書き出すように変更 (Engine 側の `SnapshotFileWriter` 整理) |
| 6 | Skill ドキュメント (`src/Adact.Cli/Skills/adact-cli/references/snapshot.md`) を新形式の出力例に更新 |
| 7 | 既存テストの再編 (Engine: raw JSON / CLI: 新形式テキスト) |
| 8 | サイズ計測 (旧形式 fixture と新形式の比較) |
| 9 | 4 アプリ目視レビュー + AI クライアント手動スモーク |

## 10. 設計上の留意点・open question

- raw フィルタは **operable と同じフィールドセット** に揃えるか、`isKeyboardFocusable` 等を残すか → **同じセットに揃える** (Research でも raw 固有値の利用例なし)。
- `value` が改行や ANSI 制御を含むケース → §2.5 のエスケープルールで吸収。長すぎる場合の truncate は本 Phase では行わない (将来課題)。
- 同名兄弟要素 (`Button "1"` が複数箇所に並ぶ等) は ref で区別。`automationId` も同じならば本来 UI 設計の問題なので ADACT は介入しない。
- frontmatter の YAML パースを外部が試みた場合: `processName` 等に YAML 予約文字 (`:` `#` 等) が含まれる場合に備え、§3.3 でクォート規則を遵守する。
