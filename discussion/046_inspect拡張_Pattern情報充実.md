# inspect 拡張 — UIA Pattern 情報の充実

## 背景

041「ADACT追加候補コマンドと機能」で、「いきなり操作コマンドを増やすより、まず inspect を強くして観測力を上げる方が先」という結論が出ている。

現行の `inspect` は基本プロパティ12個と5種のPattern情報（Toggle, SelectionItem, ExpandCollapse, RangeValue, Window）を出力するが、AIエージェントが操作判断に必要な情報が不足している場面がある。

## 目的

**AIエージェントが「次に何をすべきか」判断するための情報を充実させる。**

優先順位:
1. AIエージェントの操作判断材料
2. テスト生成のための要素情報収集（selector等）— 後続検討
3. 人間のデバッグ用

## 追加内容

### 既存 Pattern の拡張

| Pattern | 現行出力 | 追加プロパティ |
|---|---|---|
| RangeValue | Min, Max, Value | +SmallChange, +LargeChange, +IsReadOnly |
| Value | Value（基本プロパティとして） | +IsReadOnly |

### 新規 Pattern 追加

| Pattern | 出力プロパティ | ユースケース |
|---|---|---|
| Selection | CanSelectMultiple, IsSelectionRequired, SelectedItems(Ref一覧) | 「何が選ばれているか」の確認 |
| Grid | RowCount, ColumnCount | DataGrid/表形式UIの構造把握 |
| GridItem | Row, Column, RowSpan, ColumnSpan | セル位置の特定 |
| Table | RowOrColumnMajor, ColumnHeaders, RowHeaders | 列名による構造理解 |
| TableItem | ColumnHeaderItems, RowHeaderItems | セルがどの列に属するか |
| Scroll | HCanScroll, VCanScroll, HPercent, VPercent, HViewSize, VViewSize | スクロール位置の把握 |
| Text | フラグのみ（対応有無） | リッチテキスト系か判断 |

### 追加しないもの

Invoke, Dock, Styles, SynchronizedInput, SpreadSheet, VirtualizedItem, ItemContainer, MultipleView, Transform

理由: 情報量が少ない、有用性が低い、または出現頻度が極めて低い。

## 出力ポリシー

- 要素が実際に持つ Pattern のみ出力する（現行方式を踏襲）
- Selection の「選択中要素」等、他要素を参照する情報も Ref 形式で含める（実用性優先）

## AIエージェントの判断に困る場面と解決

| 場面 | 必要な情報 | 対応 Pattern |
|---|---|---|
| ComboBoxの操作前に展開が必要か判断 | ExpandCollapseState | 既存（変更なし） |
| DataGridの構造を把握したい | 行数・列数 | Grid |
| 特定セルの位置を特定したい | 行/列 index | GridItem |
| スクロールして要素を探したい | 現在位置・方向 | Scroll |
| Sliderの設定可能範囲を確認 | Min/Max/SmallChange | RangeValue拡張 |
| ListBoxの選択状態を確認 | 選択中要素Ref | Selection |
| 入力欄が読み取り専用か確認 | IsReadOnly | Value拡張 |
| テキストエリアの内容取得方法を判断 | TextPattern対応有無 | Text(フラグ) |

## 次のステップ

- 安定セレクタ候補の表示（044「FlaUI自動テスト生成Skill」で言及）は本件の後に別途検討

---

## 実装設計

### 変更対象ファイル

| ファイル | 変更内容 |
|---|---|
| `src/Adact.Engine/WindowSession.Inspect.cs` | `CollectPatterns()` に新 Pattern 追加 |
| `src/Adact.Mcp.Common/WindowsTools.Inspect.cs` | `SerializeInspectResult()` で配列型・スカラー型対応 |
| `src/Adact.Cli.Core/Commands/InspectCommand.cs` | CLI YAML 出力のフォーマット調整 |

### 追加する Pattern とプロパティ

#### 既存拡張

- **RangeValue**: +SmallChange(double), +LargeChange(double), +IsReadOnly(bool)
- **Value**: +IsReadOnly(bool)

#### 新規追加

- **Selection**: CanSelectMultiple(bool), IsSelectionRequired(bool), SelectedItems(string[] = Name配列)
- **Grid**: RowCount(int), ColumnCount(int)
- **GridItem**: Row(int), Column(int), RowSpan(int), ColumnSpan(int)
- **Table**: RowOrColumnMajor(string), ColumnHeaders(string[]), RowHeaders(string[])
- **TableItem**: ColumnHeaderItems(string[]), RowHeaderItems(string[])
- **Scroll**: HCanScroll(bool), VCanScroll(bool), HPercent(double), VPercent(double), HViewSize(double), VViewSize(double)
- **Text**: Preview(string, 先頭30文字), Length(int, 全文字数)

### CLI 出力フォーマット

#### フォーマットルール

| ルール | 例 |
|---|---|
| 値が1つだけの Pattern → スカラー直書き | `Toggle: On`, `ExpandCollapse: Collapsed`, `SelectionItem: Selected` |
| bool が true → キーワードのみ | `MultiSelect`, `ReadOnly`, `SelectionRequired` |
| bool が false → 非表示 | |
| Selection 単一選択 → `SelectedItem` | `SelectedItem: "Option B"` |
| Selection 複数選択 → `SelectedItems` | `SelectedItems: ["A", "B"]` |
| Scroll: 対応方向のみ表示 | VCanScroll=false → V セクション省略 |
| Text: 先頭30字 + 全文字数 | `Text: {Preview: "...", Length: 1500}` |
| GridItem: Span=1 は省略 | RowSpan/ColumnSpan が 1 なら非表示 |
| Table: Major を含む | `Major: "Row"` |

#### 出力例

**DataGrid:**
```yaml
patterns:
  Grid: {RowCount: 50, ColumnCount: 5}
  Scroll: {H: {Percent: 0, ViewSize: 80}, V: {Percent: 25, ViewSize: 50}}
  Selection: {MultiSelect, SelectedItems: ["Row 3", "Row 7"]}
  Table: {Major: "Row", ColumnHeaders: ["ID", "Name", "Amount", "Date", "Status"]}
```

**ListBox（単一選択）:**
```yaml
patterns:
  Selection: {SelectionRequired, SelectedItem: "Item A"}
  Scroll: {V: {Percent: 50, ViewSize: 30}}
```

**CheckBox:**
```yaml
patterns:
  Toggle: On
```

**ComboBox:**
```yaml
patterns:
  ExpandCollapse: Collapsed
  Selection: {SelectedItem: "Option B"}
```

**RichTextBox:**
```yaml
patterns:
  Text: {Preview: "Hello World, this is a long...", Length: 1500}
```

**読み取り専用 TextBox:**
```yaml
patterns:
  Value: {Value: "Hello World", ReadOnly}
  Text: {Preview: "Hello World", Length: 11}
```

**Slider:**
```yaml
patterns:
  RangeValue: {Value: 50, Min: 0, Max: 100, SmallChange: 1, LargeChange: 10}
```

**DataGrid セル:**
```yaml
patterns:
  GridItem: {Row: 2, Column: 1}
  SelectionItem: Selected
  TableItem: {ColumnHeaders: ["Name"]}
```

### 技術的な設計判断

| 判断 | 理由 |
|---|---|
| `CollectPatterns` は `static` のまま | Selection を Name で返すため RefRegistry 不要 |
| 選択中要素・ヘッダーは Name 文字列で返す | Ref 化するより単純。AIが名前で理解できる |
| シリアライズに `string[]` 対応を追加 | JSON 出力側で `JsonArray` に変換 |
| CLI 側でフォーマットルールを適用 | MCP(JSON)は構造そのまま、CLI(YAML)は簡潔化ルール適用 |

### 検証

- `dotnet build adact.sln` で警告ゼロ
- Unit テスト: `CollectPatterns` の戻り値検証（モック要素）
- Smoke テスト: SampleApp で DataGrid / ListBox / Slider を inspect して期待出力確認
