# 039: SampleApp UIA パターン検証拡張 要件定義と設計

## 背景・目的

ADACT の `SampleApp` は、WPF の代表的なコントロールを 8 タブに分けて配置し、`snapshot` / `inspect` / 基本操作コマンドの検証に使える状態になっている。一方で、`discussion/037_UIAパターン対応要否.md` で整理した UIA Control Pattern のうち、現在の SampleApp では出現しない、または出現しても観測・操作の検証観点が不足している Pattern が残っている。

本拡張の目的は、SampleApp を UIA Pattern 検証用に拡張し、各 Pattern が ADACT の `snapshot` / `inspect` でどこまで観測できるかを確認できる状態にすることである。その結果をもとに、ADACT に専用操作コマンドを追加すべきか、既存コマンド・キー入力・マウス操作で代替できるかを判断できるようにする。

## スコープ

- `samples/SampleApp/` の既存 8 タブに、自然に配置できる UIA Pattern 検証要素を追加する設計。
- 独立性が高い検証領域として、最大 3 つの新規タブを追加する設計。
  - `Drag & Drop`
  - `Rich Text`
  - `Advanced / Custom Patterns`
- `discussion/037_UIAパターン対応要否.md` の分類と矛盾しない形で、対応済み・対応すべき・不要に分類された Pattern の観測方針を整理する。
- 標準 WPF コントロールだけでは出にくい Pattern について、Custom AutomationPeer を使う候補領域を整理する。
- 将来の実装時に使える優先順位・作業分割案を示す。

## 非目的

- 本文書では SampleApp 本体の XAML / C# 実装は行わない。
- ADACT の `snapshot` / `inspect` 出力仕様や CLI / MCP コマンドの実装変更は行わない。
- すべての UIA Pattern を SampleApp で実用的に操作可能にすることは目的にしない。特に `discussion/037` で「不要」とされた Pattern は、観測可能性の確認対象にはしても、ADACT の自動操作対象としての優先度は上げない。
- 外部アプリ、Office、ブラウザ、サードパーティ UI ライブラリへの依存は追加しない。
- UI の見た目の美しさや画面密度の最適化は優先しない。Pattern 検証の網羅性を優先する。

## 設計方針

1. **既存タブに自然に追加できるものは既存タブへ追加する**
   - `SelectionPattern`、`RangeValuePattern`、`GridPattern`、`TablePattern`、`ScrollPattern` など、既存コントロールの性質に近いものは既存タブを拡張する。
2. **独立した操作モデルは新規タブに分離する**
   - Drag & Drop、Rich Text、Advanced / Custom Patterns は検証手順・Custom AutomationPeer の必要性が高いため新規タブとする。
3. **AutomationId / Name を検証用途で明示する**
   - 既存命名規則に合わせ、例: `DragDrop_ListBox_Source`, `RichText_RichTextBox_Document`, `Advanced_CustomPeer_StylesSample` のようにタブ名を接頭辞にする。
4. **観測対象と操作対象を分けて設計する**
   - `snapshot` ではツリー上の見え方、`inspect` では Pattern の有無・プロパティ値を確認する。
   - 操作コマンド化の判断は、観測できることとは別に、ADACT の既存コマンドで安全・安定に代替できるかで評価する。
5. **標準 WPF 優先、必要な場合だけ Custom AutomationPeer を使う**
   - まず標準 WPF コントロールで出る Pattern を利用する。
   - `DragPattern` / `DropTargetPattern`、`StylesPattern`、`SpreadsheetPattern` 系、`NotificationPattern` など、標準 WPF だけで安定して出しにくいものは Custom AutomationPeer 候補とする。
6. **`discussion/037` の優先度を維持する**
   - 高優先度: 専用コマンド追加候補として重点検証する。
   - 中優先度: ユースケースを再現し、専用コマンドの必要性を判断できる材料を集める。
   - 低優先度・不要: 原則として観測可否の確認に留め、専用コマンド追加の前提にはしない。

## 既存タブごとの追加要素案

### Basic Controls

| 追加要素案 | 狙う UIA Pattern | `snapshot` / `inspect` で確認したい観点 | 操作コマンド追加要否の判断観点 |
|---|---|---|---|
| Slider の現在値表示 Label と、複数 Slider（水平・垂直・無効状態） | RangeValuePattern, ValuePattern | `Minimum` / `Maximum` / `Value` / `SmallChange` / `LargeChange` が `inspect` で読めるか。無効状態が `IsEnabled` と Pattern 可用性に反映されるか。 | `set-range` が必要か。既存の `fill` / キー操作 / マウス操作で安定して任意値にできるか。 |
| ProgressBar の determinate / indeterminate 切替 | RangeValuePattern, ValuePattern | 進捗値、範囲、indeterminate 時の値の見え方。`snapshot` に状態変化が出るか。 | ProgressBar は通常操作対象ではなく読み取り用途中心のため、専用操作より `inspect` 拡張で十分か。 |
| ToggleButton / ThreeState CheckBox | TogglePattern | `On` / `Off` / `Indeterminate` の状態が `inspect` で区別できるか。 | 既存 `check` / `uncheck` で 2 値は足りるか。3 状態用の明示操作が必要か。 |
| ReadOnly TextBox / Disabled TextBox | ValuePattern, TextPattern | 読み取り専用・無効時の Value 可読性、操作可否、`IsReadOnly` 相当情報の取得可否。 | `fill` 失敗時のエラー品質確認。新コマンドではなく診断情報拡充で足りるか。 |

### Selection

| 追加要素案 | 狙う UIA Pattern | `snapshot` / `inspect` で確認したい観点 | 操作コマンド追加要否の判断観点 |
|---|---|---|---|
| 単一選択 ListBox と複数選択 ListBox を並置 | SelectionPattern, SelectionItemPattern, ScrollPattern, ScrollItemPattern | 選択中項目、複数選択可否、必須選択か、各 item の `IsSelected` が読めるか。 | `get-selection` が必要か。`snapshot` の選択表示だけで検証可能か。複数選択操作を `select` 拡張で扱うべきか。 |
| ComboBox の editable / non-editable 比較 | ExpandCollapsePattern, SelectionPattern, ValuePattern | 展開状態、選択項目、編集可能 ComboBox の入力値がどの Pattern に出るか。Popup 内 item が snapshot に出るか。 | `select` で足りるか。editable ComboBox は `fill` + `select` の組み合わせでよいか。 |
| 大量項目 ListBox / ListView（仮想化 ON） | ItemContainerPattern, VirtualizedItemPattern, ScrollPattern, ScrollItemPattern | 画面外 item が `snapshot` に出ない場合、`inspect` で ItemContainer / VirtualizedItem が見えるか。Realize 前後の差分。 | `find-item` / `realize` 相当が必要か。`scroll-into-view` と名前検索だけで足りるか。 |
| ListView GridView の列ヘッダー付き選択 | GridPattern, TablePattern, SelectionPattern | ListView が Grid / Table として見えるか、列ヘッダーが取得できるか。 | DataGrid と同じ `grid-get` / `table-headers` 対象に含めるべきか。 |

### Data Grid

| 追加要素案 | 狙う UIA Pattern | `snapshot` / `inspect` で確認したい観点 | 操作コマンド追加要否の判断観点 |
|---|---|---|---|
| 編集可能 DataGrid（Text, CheckBox, ComboBox 列） | GridPattern, GridItemPattern, TablePattern, TableItemPattern, ValuePattern, TogglePattern, SelectionItemPattern | 行・列数、セルの行列位置、ヘッダー対応、セル内編集要素の Pattern が取得できるか。 | `grid-get` / `table-headers` に加え、セル編集は既存 `dblclick` + `fill` で足りるか。 |
| 固定ヘッダー・横スクロールが必要な多列 DataGrid | ScrollPattern, GridPattern, TablePattern | スクロール可能方向、現在位置、横スクロール後の snapshot 変化。 | コンテナ単位の `scroll` が必要か。`mouse-wheel` / `scroll-into-view` で代替可能か。 |
| 選択モード切替（行選択・セル選択・複数選択） | SelectionPattern, SelectionItemPattern, GridItemPattern | 選択対象が row / cell のどちらとして見えるか、複数選択状態を読めるか。 | `get-selection` の戻り値に cell 位置情報が必要か。 |
| 行数 1000 以上 + 仮想化 ON DataGrid | ItemContainerPattern, VirtualizedItemPattern, ScrollPattern | 仮想化 item の出現有無、Realize 可能性、snapshot サイズへの影響。 | `find-item` の優先度判断。大量データで既存 snapshot 全取得が現実的か。 |

### Tree & Menu

| 追加要素案 | 狙う UIA Pattern | `snapshot` / `inspect` で確認したい観点 | 操作コマンド追加要否の判断観点 |
|---|---|---|---|
| 展開済み / 折りたたみ済み TreeViewItem の混在 | ExpandCollapsePattern, SelectionItemPattern, ScrollItemPattern | `ExpandCollapseState`、選択状態、画面外ノードの扱い。 | `select` 内の展開だけで足りるか。明示 `expand` / `collapse` が必要か。 |
| 深い階層・大量ノード TreeView | ScrollPattern, ScrollItemPattern, ItemContainerPattern, VirtualizedItemPattern | 深いノードの snapshot 表現、仮想化時の検索・Realize 可能性。 | Tree 用 `find-item` が必要か。 |
| チェック可能 MenuItem / ラジオ MenuItem | TogglePattern, SelectionItemPattern, ExpandCollapsePattern, InvokePattern | メニュー Popup 内で checked 状態が読めるか。Popup 検出の安定性。 | 既存 `click` / `check` で十分か。メニュー項目専用 select は不要か。 |
| 多階層サブメニューと ContextMenu | ExpandCollapsePattern, InvokePattern, TogglePattern | Popup が複数 HWND として snapshot に注入されるか、ContextMenu も検出できるか。 | `right-click` / context menu 操作が必要か。既存 `mouse-down/up` で代替可能か。 |

### Dynamic UI

| 追加要素案 | 狙う UIA Pattern | `snapshot` / `inspect` で確認したい観点 | 操作コマンド追加要否の判断観点 |
|---|---|---|---|
| 動的に追加される Button / CheckBox / ComboBox / ListBox | InvokePattern, TogglePattern, ExpandCollapsePattern, SelectionPattern | 追加直後の snapshot 反映、ref 更新、AutomationId の一意性。 | 新コマンドではなく auto-snapshot / ref lifecycle の検証で足りるか。 |
| 表示 / 非表示 / Enabled 切替パネル | 各 Pattern の可用性変化 | `IsOffscreen`、`IsEnabled`、Pattern の有無が状態変化に追従するか。 | `wait-for` の state 条件拡張が必要か。 |
| 動的に仮想化リストを生成 | ItemContainerPattern, VirtualizedItemPattern | 生成後の Pattern 出現、Realize 前後の差分。 | `find-item` の必要性判断に使えるか。 |
| Live region 風の通知領域 | NotificationPattern（候補） | WPF 標準で NotificationPattern が出るか、出ない場合 Custom Peer が必要か。 | `discussion/037` では不要分類。イベント監視コマンドは原則追加しない判断でよいか。 |

### Dialogs

| 追加要素案 | 狙う UIA Pattern | `snapshot` / `inspect` で確認したい観点 | 操作コマンド追加要否の判断観点 |
|---|---|---|---|
| カスタムダイアログに TextBox / ComboBox / DataGrid を配置 | WindowPattern, ValuePattern, SelectionPattern, GridPattern | モーダル Window と内部 Pattern が同一 snapshot に出るか。親 Window の disabled 状態。 | 既存 `wait-for-window` / `attach` / `snapshot` で足りるか。 |
| Resizable / non-resizable dialog 比較 | WindowPattern, TransformPattern | `CanResize`、最小化・最大化可否、Transform 可否。 | 既存 `resize` / `minimize` / `maximize` のエラー診断で十分か。 |
| OpenFileDialog / SaveFileDialog の主要要素観測 | WindowPattern, ValuePattern, SelectionPattern, InvokePattern | OS ダイアログ内のファイル名入力、ツリー、リスト、ボタンが snapshot / inspect でどう見えるか。 | 専用 file dialog コマンドが必要か。既存 `fill` / `click` / `press` で手順化できるか。 |
| 通知 MessageBox の種類比較 | WindowPattern, InvokePattern | アイコンやボタン種類がどこまで UIA で判別できるか。 | MessageBox 専用コマンドは不要で、既存 `click` + `wait-for-window` で足りるか。 |

### Multi-Window

| 追加要素案 | 狙う UIA Pattern | `snapshot` / `inspect` で確認したい観点 | 操作コマンド追加要否の判断観点 |
|---|---|---|---|
| 子 Window に独自 TabControl / DataGrid を配置 | WindowPattern, TransformPattern, SelectionPattern, GridPattern | 複数 Window の ref / session 解決、子 Window 内 Pattern の観測。 | `attach` し直しが必要か、同一 session の sibling window として扱うべきか。 |
| ToolWindow 風の小窓 | WindowPattern, DockPattern（候補）, TransformPattern | WPF 標準で DockPattern が出るか。Window style の違いが inspect に出るか。 | `discussion/037` では Dock は不要分類。専用 dock 操作は追加しない判断でよいか。 |
| 最前面 / 所有 Window / モーダレス複数起動 | WindowPattern | Window 階層、所有関係、active/focused 状態が追えるか。 | window 切替・focus 系の既存コマンドで足りるか。 |
| 別プロセス起動の結果表示 | WindowPattern | `launch` した外部プロセスと SampleApp の WindowRefStore 上の関係。 | Pattern 検証より session lifecycle 検証。新 Pattern コマンドは不要。 |

### Async / Delay

| 追加要素案 | 狙う UIA Pattern | `snapshot` / `inspect` で確認したい観点 | 操作コマンド追加要否の判断観点 |
|---|---|---|---|
| 進捗が 0→100 に変化する ProgressBar | RangeValuePattern, ValuePattern | 非同期更新中の値変化、完了時の値、`wait-for` で待てる状態。 | Progress 読み取りは `inspect` で足りるか。 |
| 完了時に ListBox / DataGrid に項目追加 | SelectionPattern, GridPattern, ItemContainerPattern | 非同期生成された項目の snapshot 反映、ref 再割当。 | `wait-for` の `exists` / `count` 条件拡張が必要か。 |
| Cancel ボタンの有効 / 無効切替 | InvokePattern, TogglePattern, IsEnabled | 操作可能状態の変化が snapshot に出るか。 | 専用 Pattern コマンドではなく `wait-for --state enabled` の改善で足りるか。 |
| 通知イベント発火サンプル | NotificationPattern（候補） | UIA event として観測できるか。現在の snapshot / inspect だけでは捉えにくいか。 | `discussion/037` では不要分類。イベント購読コマンドは原則対象外。 |

## 新規タブ案

### Drag & Drop

目的: `DragPattern` / `DropTargetPattern` の観測可否と、既存マウス操作でドラッグ＆ドロップを安定再現できるかを検証する。

| 追加要素案 | 狙う UIA Pattern | `snapshot` / `inspect` で確認したい観点 | 操作コマンド追加要否の判断観点 |
|---|---|---|---|
| Source ListBox と Target ListBox | DragPattern, DropTargetPattern, SelectionItemPattern | item がドラッグ可能として見えるか、drop target の効果や target effect が取れるか。 | `drag` / `drop` コマンドが必要か。`mouse-down` / `mouse-move` / `mouse-up` で安定するか。 |
| 並べ替え可能 ListBox | DragPattern, DropTargetPattern | ドラッグ中・ドロップ後の順序変化が snapshot に出るか。 | 座標ベース drag だけで足りるか、要素間 drag が必要か。 |
| ファイルドロップ風の Drop Zone | DropTargetPattern | DropTarget の名前・効果・受け入れ状態が読めるか。 | 外部ファイル drag は ADACT の責務に含めるか。まずは要素間 drag を優先するか。 |
| Custom AutomationPeer 付き Drag Source / Drop Target | DragPattern, DropTargetPattern | 標準 WPF で出ない場合、Custom Peer で Pattern を明示した時に `inspect` が拾えるか。 | Pattern ベース操作 API を実装する価値があるか。 |

### Rich Text

目的: `TextPattern` と TextRange 相当の情報が、プレーン TextBox と RichTextBox でどこまで観測できるかを比較する。

| 追加要素案 | 狙う UIA Pattern | `snapshot` / `inspect` で確認したい観点 | 操作コマンド追加要否の判断観点 |
|---|---|---|---|
| RichTextBox（見出し・太字・リンク風テキスト・複数段落） | TextPattern, ValuePattern, StylesPattern（候補） | 全文、選択範囲、行・段落境界、書式情報が取得できるか。 | `get-text` が必要か。`snapshot` の Name / Value だけで検証可能か。 |
| 読み取り専用 FlowDocumentScrollViewer | TextPattern, ScrollPattern | 編集不可文書で TextPattern が出るか、スクロール範囲が取れるか。 | 読み取り専用文書の取得は `get-text` で扱うべきか。 |
| 選択範囲表示と Copy ボタン | TextPattern | UIA 上の selected text が取れるか、既存 `press Ctrl+C` と比較できるか。 | 専用 `get-selected-text` が必要か、クリップボード依存でよいか。 |
| カスタム Text Peer のサンプル | TextPattern2, TextEditPattern, TextChildPattern（観測候補） | WPF 標準では出にくい Text 系拡張 Pattern の有無。 | `discussion/037` では TextPattern2 / TextEdit / TextChild は不要分類。専用コマンド化しない前提で観測のみ。 |

### Advanced / Custom Patterns

目的: 標準 WPF コントロールでは出にくい Pattern を Custom AutomationPeer で明示し、ADACT が Pattern の存在を観測できるかを検証する。ただし、`discussion/037` で低優先度・不要に分類されたものは、専用コマンド追加の優先度を上げない。

| 追加要素案 | 狙う UIA Pattern | `snapshot` / `inspect` で確認したい観点 | 操作コマンド追加要否の判断観点 |
|---|---|---|---|
| View 切替サンプル（List / Tiles / Details） | MultipleViewPattern | 現在 View ID / View 名、切替可能 View 一覧が読めるか。 | `discussion/037` では低頻度・クリック代替可能。専用コマンド不要でよいか。 |
| Style 情報付きサンプル | StylesPattern | style id/name、fill color、font などが取れるか。 | 低優先度。視覚検証は screenshot の方が実用的か。 |
| Spreadsheet 風グリッド | SpreadsheetPattern, SpreadsheetItemPattern, GridPattern, TablePattern | formula / annotation 風情報を Custom Peer で出した時に inspect で読めるか。 | Excel 特化のため低優先度。汎用 `grid-get` で足りる範囲を切り分ける。 |
| Dock 風パネル | DockPattern | dock position が取れるか。 | `discussion/037` では不要。専用操作なし。 |
| Transform2 風ズームパネル | TransformPattern2, TransformPattern | zoom level / zoom min-max が取れるか。 | ズーム操作は不要分類。既存 resize との混同を避ける。 |
| Annotation / ObjectModel / CustomNavigation サンプル | AnnotationPattern, ObjectModelPattern, CustomNavigationPattern | Custom Peer で Pattern が見えるか、値が inspect に出せるか。 | いずれも不要分類。ADACT 本体の対応対象外でよいかを確認するための観測のみ。 |
| Legacy IAccessible サンプル | LegacyIAccessiblePattern | WPF 既定または Custom Peer で MSAA 互換情報が出るか。 | レガシーアプリ専用。SampleApp では観測のみ。 |

## Pattern 別の検証狙い

| 分類 | Pattern | SampleApp 拡張での主な配置 | 主な確認目的 |
|---|---|---|---|
| 対応済み | Invoke, Value, Toggle, SelectionItem, ExpandCollapse, ScrollItem, Window, Transform | 既存全タブ、Dialogs、Multi-Window | 既存コマンドが拡張後も安定するか。状態・Pattern 情報が `inspect` で十分読めるか。 |
| 高優先度 | Selection | Selection, Data Grid, Tree & Menu | 選択中 item、複数選択可否、必須選択を取得できるか。 |
| 高優先度 | RangeValue | Basic Controls, Async / Delay | Slider / ProgressBar の範囲・現在値を読めるか、値設定コマンドが必要か。 |
| 高優先度 | Scroll | Selection, Data Grid, Tree & Menu, Rich Text | コンテナスクロールの方向・現在位置・割合を読めるか、量指定スクロールが必要か。 |
| 高優先度 | Grid / GridItem | Data Grid, Selection, Dialogs | 行列指定・セル位置・セル内容検証に必要な情報が取れるか。 |
| 高優先度 | Table / TableItem | Data Grid, Selection | ヘッダーとセルの対応を取得できるか。 |
| 中優先度 | Drag / DropTarget | Drag & Drop | 標準 WPF または Custom Peer で Pattern が出るか、既存マウス操作との差を確認する。 |
| 中優先度 | ItemContainer / VirtualizedItem | Selection, Data Grid, Tree & Menu, Dynamic UI | 仮想化 item の検索・Realize が必要かを判断する。 |
| 中優先度 | Text | Rich Text, Basic Controls | 全文・選択範囲・テキスト構造の取得が必要かを判断する。 |
| 低優先度 | Spreadsheet / SpreadsheetItem / Styles | Advanced / Custom Patterns, Rich Text | 専用コマンド不要の前提で観測可能性を確認する。 |
| 不要 | Transform2, Dock, TextPattern2, TextEdit, TextChild, Annotation, SynchronizedInput, LegacyIAccessible, ObjectModel, CustomNavigation, MultipleView, Notification | Advanced / Custom Patterns, Dynamic UI, Async / Delay | `discussion/037` の不要分類を維持しつつ、`inspect` が Pattern の存在を誤らず示せるかを確認する。 |

## `snapshot` / `inspect` で確認したい観点

### `snapshot`

- コントロールツリー上に対象要素が自然な階層で出るか。
- Popup、ContextMenu、ComboBox dropdown、多階層メニューなど別 HWND になり得る要素が見えるか。
- 仮想化 item が snapshot に出る範囲と、出ない item の扱いが明確か。
- 選択状態、展開状態、有効 / 無効、表示 / 非表示が snapshot の属性・テキストから判断できるか。
- スクロール後、ドラッグ後、非同期更新後、動的追加後に snapshot が最新状態へ更新されるか。
- ref の安定性と lifecycle が、動的 UI・仮想化・Popup・複数 Window で破綻しないか。

### `inspect`

- 対象要素がサポートする UIA Pattern 一覧を取得できるか。
- Pattern 固有プロパティを判断材料として十分に出せるか。
  - Selection: 選択中 item、複数選択可否、選択必須か。
  - RangeValue: 現在値、最小値、最大値、刻み、読み取り専用か。
  - Scroll: 水平 / 垂直スクロール可否、現在位置、表示割合。
  - Grid / GridItem: 行数、列数、行 index、列 index、row span / column span。
  - Table / TableItem: 行ヘッダー、列ヘッダー。
  - Text: document range、selected range、本文、属性取得可否。
  - Drag / DropTarget: drag state、drop effect、drop target effect。
- Pattern が標準 WPF 由来か Custom AutomationPeer 由来かを切り分けられるか。
- Pattern が出ない場合に、WPF 標準の制約なのか ADACT 側の観測不足なのかを判断できるか。

## 操作コマンド追加要否を判断する観点

| 判断観点 | 内容 |
|---|---|
| 頻度 | 業務アプリ自動化・自動テストで頻出するか。 |
| 既存コマンドでの代替性 | `click`、`fill`、`select`、`press`、`mouse-*`、`scroll-into-view` の組み合わせで安定するか。 |
| 座標依存の有無 | 代替手段が座標・画面サイズ・DPI に強く依存するなら専用コマンド候補。 |
| 検証可能性 | 操作後の状態を `snapshot` / `inspect` で機械的に検証できるか。 |
| Pattern 固有情報の必要性 | Pattern API でしか取得できない情報がテスト assertion に必要か。 |
| 失敗時診断 | 既存コマンド失敗時に、Pattern 情報があれば原因を説明できるか。 |
| 実装リスク | FlaUI / UIA の Pattern 実装差、Custom Peer 依存、OS バージョン差が大きすぎないか。 |

`discussion/037` の分類に沿うと、まず専用コマンド追加候補として重点評価すべきなのは `get-selection`、`set-range`、`scroll`、`grid-get`、`table-headers`、`find-item`、`get-text`、`drag`、`drop` である。`drop` は DropTargetPattern 側の候補として `drag` と組み合わせて評価する。低優先度・不要に分類された Pattern は、SampleApp 上で観測できても、原則として専用操作コマンド追加の根拠にはしない。

## 標準 WPF だけで出にくい Pattern と Custom AutomationPeer 候補

| Pattern | 標準 WPF での出やすさ | Custom AutomationPeer の必要性 | 備考 |
|---|---|---|---|
| DragPattern / DropTargetPattern | 出にくい | 高 | WPF の通常 DragDrop 実装だけでは UIA Pattern として安定露出しない可能性が高い。 |
| ItemContainerPattern / VirtualizedItemPattern | 仮想化設定次第 | 中 | VirtualizingStackPanel / DataGrid 仮想化で出る可能性があるが、確実な検証には専用サンプルが必要。 |
| TextPattern | TextBox / RichTextBox で出る可能性あり | 低〜中 | RichTextBox でまず検証し、不足する TextRange 属性だけ Custom Peer を検討。 |
| StylesPattern | 出にくい | 高 | RichTextBox の書式が StylesPattern として出るとは限らない。 |
| SpreadsheetPattern / SpreadsheetItemPattern | ほぼ出ない | 高 | Excel 相当の専用 Peer が必要。低優先度のため後回し。 |
| MultipleViewPattern | WPF ListBox/ListView で既定サポートが見える場合あり | 中 | 実際の view 切替を表現するなら Custom Peer が必要。 |
| TransformPattern2 | 出にくい | 高 | ズーム可能パネルを Custom Peer で表現する必要がある。不要分類。 |
| DockPattern | 出にくい | 高 | WPF layout の DockPanel とは UIA DockPattern は別物として扱う。不要分類。 |
| AnnotationPattern | 出にくい | 高 | 文書レビュー用途。観測のみ。 |
| NotificationPattern | 出にくい | 高 | イベント監視が主用途で、snapshot / inspect だけでは価値が限定的。 |
| ObjectModelPattern / CustomNavigationPattern | 出にくい | 高 | アプリ固有モデル・アクセシビリティ用途。ADACT の汎用操作とは距離がある。 |
| LegacyIAccessiblePattern | WPF 既定で出る場合あり | 低〜中 | レガシー互換情報の観測に留める。 |
| SynchronizedInputPattern | 出にくい | 高 | `wait-for` で代替する方針と矛盾しないよう、観測のみ。 |

## 実装時の優先順位・作業分割案

### Phase 1: 高優先度 Pattern を既存タブへ追加

- Basic Controls: RangeValue / Toggle の状態比較を追加。
- Selection: 単一選択・複数選択・editable ComboBox・仮想化 ListBox を追加。
- Data Grid: 編集可能 DataGrid、多列 DataGrid、選択モード比較、仮想化 DataGrid を追加。
- Tree & Menu: 展開状態、チェック可能 MenuItem、多階層メニュー、ContextMenu 検証を追加。

目的: `SelectionPattern`、`RangeValuePattern`、`ScrollPattern`、`GridPattern`、`GridItemPattern`、`TablePattern`、`TableItemPattern` の観測材料を揃える。

### Phase 2: 中優先度 Pattern 用の新規タブを追加

- `Drag & Drop` タブを追加し、標準 WPF 実装と Custom AutomationPeer 実装を比較する。
- `Rich Text` タブを追加し、TextPattern / TextRange 相当の取得可能性を検証する。
- Selection / Data Grid / Tree & Menu に仮想化サンプルを追加し、ItemContainer / VirtualizedItem の出現を確認する。

目的: `drag` / `drop`、`find-item`、`get-text` の専用コマンド化判断に必要な材料を揃える。

### Phase 3: Advanced / Custom Patterns タブを追加

- `Advanced / Custom Patterns` タブに、低優先度・不要分類の Pattern を Custom AutomationPeer で観測できる最小サンプルを配置する。
- `inspect` が Pattern の存在を正しく表示できるかを確認する。

目的: ADACT が未知・低優先度 Pattern を見たときに診断情報として破綻しないことを確認する。専用操作コマンド追加は原則行わない。

### Phase 4: 検証シナリオ文書化

- 各タブ・各 Pattern について、`snapshot` → `inspect` → 操作 → 再 `snapshot` / `inspect` の最小手順を整理する。
- `discussion/032_SampleApp_全サブコマンド検証結果.md` の後継として、Pattern 別検証結果を別文書にまとめる。

## 未決事項

- `inspect` が Pattern 固有プロパティをどの粒度まで出すべきか。現状の出力で不足する場合、SampleApp 拡張と並行して ADACT 側の inspect 拡張が必要になる。
- Drag & Drop の Custom AutomationPeer をどこまで本格実装するか。Pattern の観測だけでよいのか、実際のドラッグ状態・ドロップ効果まで再現するかは未決。
- 仮想化サンプルで WPF 標準の `ItemContainerPattern` / `VirtualizedItemPattern` が安定して出るかは実測が必要。
- `NotificationPattern` は snapshot / inspect だけでは検証価値が低い。イベント購読を ADACT の対象外とするなら、SampleApp 側の実装優先度を下げてよい。
- 新規タブ 3 つを追加すると TabControl が横に混み合う。スクロール可能なタブヘッダーやタブ内ナビゲーションを入れるかは未決。ただし、網羅性を優先するため初期実装では単純追加でよい。
- 低優先度・不要 Pattern の Custom AutomationPeer 実装が過度に複雑になる場合、Advanced / Custom Patterns タブは「Pattern 名ごとの最小ダミー Peer」に留めるか、Phase 3 自体を後回しにする。
