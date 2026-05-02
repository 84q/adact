# SampleApp 全サブコマンド検証結果

## 背景

ADACT CLI の全サブコマンドが SampleApp を使って動作するかを検証する。

## 環境

- **SampleApp**: PID 2096（WPF, `net10.0-windows`）
- **ADACT daemon**: ポート 41307（`adact serve`）
- **CLI**: `Adact.Cli.Client`（`--server http://127.0.0.1:41307/mcp`）

## 成功したコマンド

| # | コマンド | 詳細 | 結果 |
|---|---------|------|------|
| 1 | `list-apps` | SampleApp を検出 | ✅ |
| 2 | `attach w1` | SampleApp にアタッチ（session s1） | ✅ |
| 3 | `snapshot` | UIA snapshot 取得 | ✅ |
| 4 | `click` | Button（Submit）、RadioButton（Option B）、TabItem（複数タブ切り替え） | ✅ |
| 5 | `fill` | TextBox に "Test Input" を入力 | ✅ |
| 6 | `type` | TextBox に "ABC" を1文字ずつ入力 | ✅ |
| 7 | `clear` | TextBox をクリア | ✅ |
| 8 | `focus` | TextBox にフォーカス | ✅ |
| 9 | `check` | CheckBox をオン | ✅ |
| 10 | `uncheck` | CheckBox をオフ | ✅ |
| 11 | `select` | ComboBox で `--name "Blue"` を選択 | ✅ |
| 12 | `press` | Tab キー送信 | ✅ |
| 13 | `minimize` | ウィンドウ最小化 | ✅ |
| 14 | `maximize` | ウィンドウ最大化 | ✅ |
| 15 | `restore` | ウィンドウ復元 | ✅ |
| 16 | `resize` | `--width 800 --height 600`（minimize 後は「session locked」エラー、restore 後は成功） | ✅ |
| 17 | `inspect` | Button の UIA プロパティを JSON で取得 | ✅ |
| 18 | `screenshot` | PNG スクリーンショットを取得 | ✅ |
| 19 | `hover` | Submit Button にマウスホバー | ✅ |
| 20 | `dblclick` | DataGrid のセルをダブルクリック | ✅ |
| 21 | `mouse-wheel` | DataGrid で `--delta-y 3` を送信 | ✅ |
| 22 | `mouse-move` | DataGrid 上にマウス移動 | ✅ |
| 23 | `mouse-down` / `mouse-up` | DataGrid 上でマウス押下・解放 | ✅ |
| 24 | `scroll-into-view` | DataGrid の行をスクロールして表示 | ✅ |
| 25 | `launch calc.exe` | 電卓アプリを起動（PID 10980） | ✅ |
| 26 | `close` | SampleApp を閉じる（auto-detach） | ✅ |
| 27 | `daemon-stop` | daemon を停止 | ✅ |

## 確認できたが効果の確認が困難だったコマンド

| # | コマンド | 理由 |
|---|---------|------|
| 28 | `key-down` / `key-up` | `press` で代替確認済み。単独での動作確認は自動化が難しい |
| 29 | `mouse-move` / `mouse-down` / `mouse-up` / `mouse-wheel` | エラーなく実行されたが、UI の視覚的変化が snapshot で確認しにくい |
| 30 | `scroll-into-view` | エラーなく実行されたが、スクロール位置の変化が snapshot で確認しにくい |
| 31 | `select` | ComboBox で実行したが、選択状態の変化が snapshot で確認しにくい |

## SampleApp で検証が難しかったコマンド

| # | コマンド | 難しい理由 | SampleApp の対応状況 |
|---|---------|----------|---------------------|
| 32 | `wait-for` | 特定の状態変化（表示/非表示/有効化 等）を待機する必要があるが、自動的に状態が変化する要素が少ない | 対応可能だが、Async/Delay タブの「30秒処理」で `wait-for --state enabled` 等が使える |
| 33 | `wait-for-window` | 新しいウィンドウが表示されるのを待機する必要がある | Multi-Window タブで「Open Modal Window」ボタンがあるが、子ウィンドウの操作まで含めると手順が複雑 |
| 34 | `close-all` | 複数セッションが必要 | SampleApp は1つしか起動していない。`launch` で別アプリを起動後に実行可能 |
| 35 | `detach` | セッションを手放すだけなので動作確認が難しい（副作用がない） | `close` で auto-detach されたため間接確認 |
| 36 | `kill` | セッションのプロセスを強制終了するが、`close` で既に閉じている | `launch` で起動した別プロセスに対して `--sid` を指定して実行可能 |

## SampleApp の機能不足・制約（検証で発見）

| # | 項目 | 詳細 |
|---|------|------|
| 1 | **TreeView 展開** | `click` で TreeItem をクリックしても子ノードが展開されない（WPF 仕様）。Expander ボタンを個別にクリックする必要あり |
| 2 | **Tree & Menu タブの内容表示** | TabItem の `click` でタブは切り替わるが、snapshot に TreeView 等の内容が表示されない場合がある（キャッシュまたはタイミング問題） |
| 3 | **ContextMenu** | SampleApp に右クリックメニューはあるが、ADACT に `right-click` コマンドがないため検証不可 |
| 4 | **ComboBox/ListBox 選択確認** | `select` コマンドは実行できたが、選択状態が snapshot で確認しにくい |
| 5 | **DataGrid スクロール確認** | `mouse-wheel` や `scroll-into-view` は実行できたが、スクロール位置の変化が snapshot で確認しにくい |
| 6 | **FileDialog 操作** | OpenFileDialog / SaveFileDialog は表示できるが、ファイル選択の自動化は難しい |
| 7 | **子ウィンドウ操作** | Multi-Window タブでモーダル/モーダレスウィンドウを開けるが、子ウィンドウの snapshot 取得・操作までの一連の流れが複雑 |
| 8 | **Async/Delay 完了確認** | 30秒処理の開始はできるが、完了後の UI 更新まで待機するためには `wait-for` が必要 |

## 結論

**SampleApp は約 30 コマンド中、27 コマンドを直接検証可能な機能を持っている。**

残りのコマンド（`wait-for`, `wait-for-window`, `close-all`, `detach`, `kill`）は、SampleApp の機能を組み合わせることで間接的に検証可能だが、手順が複雑になるため「自動テストシナリオ化」には不向き。

検証が最も難しかったのは：
1. **Tree & Menu タブの内容表示**（キャッシュ/タイミング問題）
2. **選択状態・スクロール位置の変化確認**（snapshot では UI の変化が見えにくい）
3. **wait-for / wait-for-window**（状態変化を人為的に発生させる必要がある）
