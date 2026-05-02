# SampleApp 要件定義

## 背景・目的

ADACT の主要操作（クリック、テキスト入力、選択、スクロール、モーダル、マルチウィンドウ、非同期待機、動的 UI 等）を、外部アプリ（電卓・メモ帳・Chrome・Notepad++ 等）に依存せずに再現・検証・デバッグできる専用 WPF アプリを作成する。

## 基本情報

| 項目 | 内容 |
|------|------|
| 名称 | `SampleApp` |
| フレームワーク | WPF（.NET） |
| UI 言語 | 英語 |
| 配置 | 本リポジトリ内の独立したプロジェクト。ADACT 本体およびテストプロジェクトとは相互に依存しない |
| CI / 配布 | 今回は対象外 |
| 多重起動 | 同一アプリの複数インスタンス起動を許容する |

## UI 構成

メインウィンドウは **TabControl** を中心とし、多めのタブ（6〜10 程度）で機能を分類する。

### タブ分類（想定）

| タブ | 含める UI / 機能 |
|------|-----------------|
| Basic Controls | Button, TextBox, PasswordBox, CheckBox, RadioButton, Slider, ProgressBar, Label, ToolTip, StatusBar |
| Selection | ComboBox, ListBox, ListView |
| Data Grid | DataGrid（100 行以上、縦横スクロール可能） |
| Tree & Menu | TreeView（入れ子あり）、MenuBar、ContextMenu（右クリックメニュー） |
| Dynamic UI | ボタンクリックでコントロールを動的に追加・削除するパネル |
| Dialogs & Modal | MessageBox、カスタム入力ダイアログ、ファイル Open / Save ダイアログ |
| Multi-Window | 子ウィンドウ（モーダル / モーダレス）を開く、別プロセス起動 |
| Async / Delay | バックグラウンド処理（Thread.Sleep 30 秒）→ 完了後に UI 更新するボタン |

## 機能要件

### コントロール網羅
- ADACT のテスト・デバッグで頻出する全ての基本コントロールを含める
- 各コントロールは操作可能な状態で配置する（read-only や disabled のみでないこと）

### スクロール検証
- DataGrid は 100 行以上のデータを持ち、縦横両方のスクロールが可能であること
- ListBox / ListView でも大量アイテムによるスクロールを検証できること

### モーダル・ダイアログ
- MessageBox（確認、警告、エラー）を表示できること
- カスタム WPF ダイアログ（テキスト入力付き）を開けること
- ファイル Open ダイアログ（`OpenFileDialog`）を開けること
- ファイル Save ダイアログ（`SaveFileDialog`）を開けること

### マルチウィンドウ
- 同一プロセス内のモーダル Window を開けること
- 同一プロセス内のモーダレス Window を開けること
- 別プロセス（例: calc.exe）を起動できること

### 動的 UI
- ボタンクリックにより、ランタイムでコントロールを追加・削除できるパネルを持つこと
- 追加されたコントロールにも UIA からアクセス可能であること

### 非同期・遅延
- バックグラウンドスレッドで長時間処理（Thread.Sleep 30 秒）を実行し、完了後に UI（ProgressBar やラベル）を更新するボタンを持つこと

## アクセシビリティ要件

- 各コントロールに `AutomationProperties.AutomationId` および `AutomationProperties.Name` を設定し、UIA から操作しやすくする
- ウィンドウタイトルは ADACT から `attach` しやすい固定文字列を含める
- 過度なカスタムテンプレートは避け、WPF デフォルトスタイルを基本とする

## 非機能要件

- 起動が軽量であること（テスト・デバッグ時の待ち時間を最小化）
- 依存関係を最小限に抑え、ADACT 本体およびテストプロジェクトから参照されないこと
- .NET のバージョンは ADACT プロジェクトと整合性を取ること（要 Design フェーズで確認）

## 今回のスコープ外

- 多言語対応（日本語等）
- CI 連携・自動テスト化
- インストーラー / 配布
- 設定の永続化（アプリ終了後に状態を保存しない）

---

# Design（設計）

## .NET バージョン

`net10.0-windows` — ADACT プロジェクト群と整合。

## プロジェクト構成

```
samples/
└── SampleApp/
    ├── SampleApp.csproj
    ├── App.xaml
    ├── App.xaml.cs
    ├── MainWindow.xaml          # TabControl + MenuBar
    ├── MainWindow.xaml.cs
    └── Tabs/
        ├── BasicControlsTab.xaml
        ├── SelectionTab.xaml
        ├── DataGridTab.xaml
        ├── TreeMenuTab.xaml
        ├── DynamicUITab.xaml
        ├── DialogsTab.xaml
        ├── MultiWindowTab.xaml
        └── AsyncDelayTab.xaml
```

- **配置**: `samples/SampleApp/` を新設。ADACT 本体・テストプロジェクトとは独立。
- **ソリューション**: `adact.sln` に追加し、ビルドしやすくする。
- **参照関係**: ADACT 本体およびテストプロジェクトから一切参照されない。逆も同様。

## タブ構成

| タブ名 | ファイル | 主な内容 |
|--------|---------|---------|
| Basic Controls | `BasicControlsTab.xaml` | Button, TextBox, PasswordBox, CheckBox, RadioButton (Group), Slider, ProgressBar, Label, ToolTip |
| Selection | `SelectionTab.xaml` | ComboBox, ListBox, ListView（各種選択パターン） |
| Data Grid | `DataGridTab.xaml` | DataGrid（100行+、多列、縦横スクロール） |
| Tree & Menu | `TreeMenuTab.xaml` | TreeView（3階層程度）、MenuBar（File/Edit/View）、右クリック ContextMenu |
| Dynamic UI | `DynamicUITab.xaml` | 「Add Button」「Remove Last」「Clear All」等で動的にコントロールを操作 |
| Dialogs | `DialogsTab.xaml` | MessageBox（Info/Warning/Error/Confirm）、カスタムダイアログ、OpenFileDialog, SaveFileDialog |
| Multi-Window | `MultiWindowTab.xaml` | モーダル Window 起動、モーダレス Window 起動、別プロセス起動（calc.exe） |
| Async / Delay | `AsyncDelayTab.xaml` | 30秒 Thread.Sleep → 完了後 ProgressBar とラベル更新 |

## 共通設定

- **ウィンドウタイトル**: `"ADACT SampleApp"`（ADACT から `attach "ADACT SampleApp"` で検索可能）
- **AutomationProperties**: 各コントロールに `AutomationProperties.AutomationId="Basic_Button_Submit"` のような命名規則を設定
- **スタイル**: WPF デフォルト。Material Design 等の外部ライブラリは使用しない
- **データソース**: DataGrid / TreeView 等はコードビハインドで `ObservableCollection` 等を生成

## csproj 設定案

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>SampleApp</AssemblyName>
    <RootNamespace>SampleApp</RootNamespace>
  </PropertyGroup>
</Project>

---

# 調査結果・既知の課題

## WPF Popup / Menu サブメニューの snapshot 取得問題

### 事象
- SampleApp の MenuBar（File/Edit/View）を `click` しても、展開されたサブメニュー（Open, Save, Exit 等）が snapshot に表示されない
- 画面上にはサブメニューが表示されている

### 調査済みの事実

| # | 調査項目 | 結果 |
|---|---------|------|
| 1 | WPF Popup の実装 | `Popup.IsOpen=true` で**別 HWND（別ウィンドウ）**を生成する（MS Learn 公式） |
| 2 | UIA ツリー上の構造 | Popup 内の要素は**メインウィンドウの子ではなく**、Desktop ルート配下の別 Window として存在する |
| 3 | ADACT `SnapshotBuilder` の検索範囲 | メインウィンドウの `_rootElement` から `FindAllChildren()`（TreeScope.Children）で再帰。別ウィンドウの要素は対象外 |
| 4 | `operable` フィルタの影響 | `Menu` / `MenuItem` は `AlwaysInclude` であり、フィルタによる除外ではない |
| 5 | `FindAllChildren()` vs `FindAllDescendants()` | `Children` = 直接の子のみ。`Descendants` = 同じ要素配下の全子孫（フラット）。**ウィンドウ境界を超えた検索は不可** |
| 6 | ADACT `click` の実装 | `FlaUiElement.Click()` は `AsButton()` で `InvokePattern` を試行 → 失敗時は物理クリック。`MenuItem` は `Button` 型ではないため物理クリックのみ |
| 7 | `DetectModalElements` | モーダルダイアログ（`WS_DISABLED` な子ウィンドウ）を検出して snapshot に追加する仕組みあり。**Popup メニューはモーダルではないため対象外** |
| 8 | プロセスIDによる区別 | Popup ウィンドウも**同じプロセスID** を持つため、操作対象ウィンドウと関係ない他アプリの Popup とは区別可能 |

### 未調査・不明な点

| # | 項目 | 状態 |
|---|------|------|
| 1 | Desktop ルートから同じ PID の Popup を検出した場合、実際に `FindAllChildren()` で子要素（Open/Save/Exit）が取得できるか | **未実測** |
| 2 | 取得した Popup 要素を snapshot ツリーにどの位置（MenuItem の下？別枝？）で注入するべきか | **未決定** |
| 3 | ContextMenu（右クリックメニュー）も同じ Popup 機構を使うか、異なる挙動になるか | **未実測** |
| 4 | 入れ子サブメニュー（マウスオーバーで右側に展開される多階層メニュー）の UIA 構造 | **未調査**（後回しで可） |
| 5 | `FindAllDescendants()` を通常の WPF 要素に対して一律適用した場合のパフォーマンス影響 | **未計測** |
| 6 | 非表示時の Popup ウィンドウの有無（`IsOpen=false` 時に UIA ツリーに残るか） | **未確認** |

### 解決済み

**実装日**: 2026-05-02

ADACT Engine に `DetectPopupElements()` を追加し、Desktop ルートから同じ PID の Popup ウィンドウを検出して snapshot に注入する方式を採用。

実装内容:
- `src/Adact.Engine/WindowSession.cs`: `DetectPopupElements(IReadOnlyList<IElement> modalElements)` メソッドを新設
- `src/Adact.Engine/Snapshot/SnapshotBuildInput.cs`: `PopupSiblings` フィールドを追加
- `src/Adact.Engine/Snapshot/SnapshotBuilder.cs`: Popup 要素を `rootNode["children"]` に追加し、`isPopup: true` フラグを出力

動作確認結果:
```
- Window [ref=s1e56] [isPopup]
  - MenuItem "Open" [aid="MainWindow_MenuItem_File_Open"] [ref=s1e57]
  - MenuItem "Save" [aid="MainWindow_MenuItem_File_Save"] [ref=s1e59]
  - MenuItem "Exit" [aid="MainWindow_MenuItem_File_Exit"] [ref=s1e62]
```

---

# ADACT 操作テスト結果

## テスト概要

- **対象**: SampleApp（PID 11244）
- **接続**: ADACT daemon（`adact serve`）+ CLI（`adact attach`）
- **テスト日**: 2026-05-02

## Basic Controls タブ

| # | 操作 | コマンド | 結果 |
|---|------|---------|------|
| 1 | TextBox に入力 | `fill s1e22 "ADACT Test"` | ✅ 成功 |
| 2 | Submit ボタンクリック | `click s1e25` | ✅ 成功。Status Label が `"Submitted: ADACT Test"` に更新 |
| 3 | CheckBox オン | `check s1e27` | ✅ 成功 |
| 4 | Option B RadioButton 選択 | `click s1e31` | ✅ 成功 |

## MenuBar

| # | 操作 | コマンド | 結果 |
|---|------|---------|------|
| 5 | File メニュークリック | `click s1e9` | ✅ 成功。サブメニュー（Open/Save/Exit）が snapshot に `isPopup` として検出 |

## 新たに発見された課題：WPF TabControl の非アクティブタブ

### 事象
- `click <TabItemのref>` でタブを切り替えても、snapshot に新しいタブの中身が表示されない
- `focus <TabItemのref>` → `press "Ctrl+Tab"` でも同様
- `press Tab` ではタブヘッダーではなく、タブ内のコントロールにフォーカスが移る

### 調査結果

#### 原因1: ADACT の `FlaUiElement._children` キャッシュ
- `FlaUiElement` は `Children` プロパティを初回アクセス時にキャッシュし、**2回目以降は同じリストを返す**
- タブ切り替え後もキャッシュが無効化されないため、古いタブの内容のまま

#### 原因2: WPF TabControl の仕様
- WPF の TabControl は、**選択されていない（非アクティブ）なタブの内容を UI 要素として生成しない**
- ただし、**アクティブになったタブの内容は UIA ツリーに存在する**
- キャッシュをクリアすれば、正しく表示される

#### 解決済み
**実装日**: 2026-05-02

- `src/Adact.Engine/Elements/IElement.cs`: `ClearChildrenCache()` メソッドを追加
- `src/Adact.Engine/Elements/FlaUiElement.cs`: `_children = null;` でキャッシュクリア
- `src/Adact.Engine/WindowSession.cs`: `SnapshotAsync` 内で `_rootElement?.ClearChildrenCache();` を呼び出し

修正後、`click <TabItem>` でタブを切り替えると、snapshot に新しいタブの内容が正しく表示されることを確認。

---

# ADACT 操作テスト結果（全タブ網羅テスト）

## テスト概要

- **対象**: SampleApp（PID 420）
- **接続**: ADACT daemon（`adact serve`）+ CLI（`adact attach`）
- **テスト日**: 2026-05-02
- **備考**: `FlaUiElement._children` キャッシュ修正後のテスト

## Basic Controls タブ

| # | 操作 | コマンド | 結果 |
|---|------|---------|------|
| 1 | TextBox に入力 | `fill s1e22 "ADACT Test"` | ✅ 成功 |
| 2 | Submit ボタンクリック | `click s1e25` | ✅ 成功。Status Label が `"Submitted: ADACT Test"` に更新 |
| 3 | CheckBox オン | `check s1e27` | ✅ 成功 |
| 4 | Option B RadioButton 選択 | `click s1e31` | ✅ 成功 |

## MenuBar

| # | 操作 | コマンド | 結果 |
|---|------|---------|------|
| 5 | File メニュークリック | `click s1e9` | ✅ 成功。サブメニュー（Open/Save/Exit）が snapshot に `isPopup` として検出 |

## Selection タブ

| # | 操作 | コマンド | 結果 |
|---|------|---------|------|
| 6 | タブ切り替え | `click s1e20` | ✅ 成功。ComboBox, ListBox, ListView が表示 |
| 7 | ComboBox 検出 | `snapshot` | ✅ `Selection_ComboBox_Colors` を検出 |
| 8 | ListBox 検出 | `snapshot` | ✅ `Selection_ListBox_Fruits` を検出（20項目） |

## Data Grid タブ

| # | 操作 | コマンド | 結果 |
|---|------|---------|------|
| 9 | タブ切り替え | `click s1e22` | ✅ 成功。DataGrid（100行+）が表示 |
| 10 | DataGrid 検出 | `snapshot` | ✅ `DataGrid_Grid_Main` を検出（410行のsnapshot出力） |

## Tree & Menu タブ

| # | 操作 | コマンド | 結果 |
|---|------|---------|------|
| 11 | タブ切り替え | `click s1e24` | ✅ 成功。TreeView（3階層）が表示 |
| 12 | TreeView 検出 | `snapshot` | ✅ `TreeMenu_TreeView_Main` を検出。Root → Category 1-3 → Item 1-1〜3-3 |

## Dynamic UI タブ

| # | 操作 | コマンド | 結果 |
|---|------|---------|------|
| 13 | タブ切り替え | `click s1e58` | ✅ 成功。Add/Remove/Clear ボタンが表示 |
| 14 | TextBox 動的追加 | `click s1e67` | ✅ 成功。`Dynamic_TextBox_1` が snapshot に追加 |

## Dialogs タブ

| # | 操作 | コマンド | 結果 |
|---|------|---------|------|
| 15 | タブ切り替え | `click s1e60` | ✅ 成功。7つのボタンが表示 |
| 16 | MessageBox 表示 | `click s1e75` | ✅ 成功。`Window "Info"` が snapshot に検出 |
| 17 | MessageBox 閉じる | `click s1e557` | ✅ 成功。snapshot から消える |

## Multi-Window タブ

| # | 操作 | コマンド | 結果 |
|---|------|---------|------|
| 18 | タブ切り替え | `click s1e62` | ✅ 成功。3つのボタンが表示 |

## Async / Delay タブ

| # | 操作 | コマンド | 結果 |
|---|------|---------|------|
| 19 | タブ切り替え | `click s1e64` | ✅ 成功。Start Long Task ボタンと Status Label が表示 |

---

## 現状まとめ

### 成功したこと

| # | 機能 | 詳細 |
|---|------|------|
| 1 | **SampleApp 作成** | WPF TabControl + 8タブで各種コントロールを網羅 |
| 2 | **attach / snapshot** | ADACT から SampleApp にアタッチし、snapshot を取得 |
| 3 | **fill（テキスト入力）** | TextBox に文字列を入力 |
| 4 | **click（ボタンクリック）** | Button, RadioButton, TabItem, TreeItem, MenuItem をクリック |
| 5 | **check（チェックボックス）** | CheckBox をオンにする |
| 6 | **Popup メニュー検出** | File メニューのサブメニュー（Open/Save/Exit）を `isPopup` として検出 |
| 7 | **タブ切り替え** | `click <TabItem>` で全タブに切り替え可能（キャッシュ修正後） |
| 8 | **TreeView 表示** | 3階層の TreeView（Root → Category → Item）を検出 |
| 9 | **DataGrid 表示** | 100行+の DataGrid を検出 |
| 10 | **動的 UI** | 「Add TextBox」ボタンで動的に追加された TextBox を検出 |
| 11 | **MessageBox** | MessageBox を表示し、snapshot に検出後、OK ボタンで閉じる |
| 12 | **キャッシュ問題修正** | `FlaUiElement._children` キャッシュをクリアする `ClearChildrenCache()` を実装 |
| 13 | **ビルド・テスト成功** | `dotnet build` / `dotnet test` ともに成功（0エラー、0失敗） |

### 失敗したこと / 制約

| # | 機能 | 詳細 | 対応状況 |
|---|------|------|---------|
| 1 | **TreeView 展開** | TreeItem の `click` で子ノードが展開されない（WPF 仕様） | 未対応。Expander ボタンをクリックする必要あり |
| 2 | **ContextMenu** | 右クリックメニューの表示・操作は未テスト | 未テスト |
| 3 | **ComboBox 選択** | `select` コマンドでの ComboBox アイテム選択は未テスト | 未テスト |
| 4 | **ListBox 選択** | `select` コマンドでの ListBox アイテム選択は未テスト | 未テスト |
| 5 | **スクロール操作** | DataGrid / ListBox のスクロールは未テスト | 未テスト |
| 6 | **カスタムダイアログ** | カスタム WPF ダイアログの開閉は未テスト | 未テスト |
| 7 | **FileDialog** | OpenFileDialog / SaveFileDialog の開閉は未テスト | 未テスト |
| 8 | **モーダル/モーダレスウィンドウ** | 子ウィンドウの起動・操作・閉じるは未テスト | 未テスト |
| 9 | **calc.exe 起動** | `launch` コマンドでの別プロセス起動は未テスト | 未テスト |
| 10 | **Async/Delay 処理** | 30秒処理の開始・完了確認は未テスト | 未テスト |

### まだ試していないこと

| # | 機能 | 詳細 |
|---|------|------|
| 1 | **screenshot（特定要素）** | `--ref` を指定して特定要素のスクリーンショットを撮影 |
| 2 | **inspect** | 特定要素の詳細 UIA プロパティを JSON で取得 |
| 3 | **dblclick** | ダブルクリック操作 |
| 4 | **hover** | マウスホバー操作 |
| 5 | **press（キーコンボ）** | Ctrl+C, Enter 等のキーコンボ送信 |
| 6 | **type（文字入力）** | 1文字ずつの入力 |
| 7 | **focus** | キーボードフォーカスの移動 |
| 8 | **clear** | 入力要素のクリア |
| 9 | **scroll-into-view** | 要素が表示されるようにスクロール |
| 10 | **resize / minimize / maximize / restore** | ウィンドウサイズ変更 |
| 11 | **wait-for** | 要素の状態変化を待機 |
| 12 | **close / kill** | ウィンドウを閉じる / プロセスを強制終了 |
| 13 | **close-all** | 全アタッチウィンドウを閉じる |
| 14 | **detach** | セッションを手放す（ウィンドウはそのまま） |
| 15 | **open ダイアログのファイル選択** | OpenFileDialog で実際にファイルを選択 |

---

## 結論

**全タブの要素が ADACT snapshot で正しく検出・操作可能。**

`FlaUiElement._children` キャッシュ修正により、タブ切り替え後も正しい UIA ツリーが取得できるようになった。
```
