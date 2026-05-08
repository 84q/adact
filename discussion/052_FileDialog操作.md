# 052 FileDialog 操作

## 概念

### 目的

AI エージェントが Windows の標準 FileDialog（OpenFileDialog / SaveFileDialog）を ADACT の既存コマンドで操作できるようにする。操作手順を Skill 参照ファイルとして文書化する。

### 背景

- discussion/019 ID23「FileDialog 操作の解決策検討」が未着手
- discussion/032 で「ファイル選択の自動化は難しい」と記載されているが、具体的な検証は未実施
- FileDialog は OS 標準のモーダルダイアログであり、ADACT の既存コマンド（snapshot → fill → click）で操作可能な可能性がある

### スコープ

- **対象**: Win32 / WPF / WinForms の標準 FileDialog（`IFileDialog` ベース）
  - `Microsoft.Win32.OpenFileDialog`
  - `Microsoft.Win32.SaveFileDialog`
  - `System.Windows.Forms.OpenFileDialog`
  - `System.Windows.Forms.SaveFileDialog`
- **除外**: FolderDialog（別タスク）、UWP `FileOpenPicker`、カスタム FileDialog、専用コマンド

### 方針

1. 既存コマンドで操作する手順を確立する（専用コマンドは作らない）
2. SampleApp で `fill` の動作可否を実機検証する
3. 検証結果に基づき `references/file-dialog.md` を作成する
4. `fill` が効かない場合のフォールバック手順も検証結果に応じて記載する

### FileDialog の UIA 構造（既知）

AutomationId は Win32 ダイアログリソース ID に基づき、Windows 95 以降全バージョンで安定:

| 要素 | AutomationId | ControlType | 備考 |
|------|-------------|-------------|------|
| ファイル名入力欄 | `1148` | ComboBox > Edit | `cmb13`。ValuePattern 対応 |
| ファイルの種類 | `1136` | ComboBox | `cmb1` |
| 開く/保存ボタン | `1` | Button | `IDOK` |
| キャンセルボタン | `2` | Button | `IDCANCEL` |

### 成果物

1. SampleApp での実機検証結果（本ドキュメントに追記）
2. `src/Adact.Cli.Core/Skills/adact-cli/references/file-dialog.md`（Skill 参照ファイル）

## 設計

### 実機検証の手順

SampleApp（Dialogs タブ）で以下を検証する:

1. SampleApp をビルド・起動、`adact serve` 起動、`adact attach`
2. Dialogs タブに移動 → "Open File Dialog" ボタンを click
3. `adact snapshot` → FileDialog のモーダル構造を確認
4. ファイル名入力欄（AutomationId `1148`）の ref を特定
5. `adact fill --ref <ref> --value "C:\test.txt"` → 動作可否を確認
6. `fill` が効かない場合: `focus` → `keypress Ctrl+A` → `type` → `keypress Enter` を試行
7. Cancel ボタン（AutomationId `2`）を click → ダイアログ閉鎖を確認
8. SaveFileDialog でも同様に検証
9. 結果を本ドキュメントに追記

### `references/file-dialog.md` の構成

popup-and-modal.md と同様のスタイルで以下のセクション:

- **What is a FileDialog**: OS 標準のモーダルダイアログ。snapshot で `[modal]` フラグ付きで表示される
- **Fixed AutomationIds**: テーブル（1148, 1136, 1, 2）。Win32 リソース ID に基づき安定
- **Operation sequence**: snapshot → fill → click の 3 ステップ
- **Scope**: Win32/WPF/WinForms の標準ダイアログのみ。UWP FileOpenPicker やカスタム FileDialog は対象外
- **Fallback**: fill が効かない場合の代替手順（検証結果に応じて記載）

### SKILL.md への追記

snapshot テーブルの下の popup-and-modal 言及の近くに以下を追記:

```
Standard Windows file dialogs (Open/Save) appear as `[modal]` windows.
See [`references/file-dialog.md`](references/file-dialog.md) for fixed AutomationIds and the operation sequence.
```

## 検証結果

### 検証環境

- SampleApp（WPF、Dialogs タブ）
- adact daemon（HTTP モード、127.0.0.1:41300）
- Windows 11

### OpenFileDialog

#### snapshot 構造（抜粋）

```
- Window "ADACT SampleApp" [ref=s1e1]
  - Window "Open File" [focused] [ref=s1e422]
    - ToolBar "コマンド モジュール" [aid="FolderBandModuleInner"] [ref=...]
    - Pane "コントロール ホスト" [aid="ProperTreeHost"] [ref=...]
      - Tree "ナビゲーション ウィンドウ" [aid="100"] [ref=...]
    - Pane "シェル フォルダー ビュー" [aid="listview"] [ref=...]
      - List "項目ビュー" [ref=...]
    - Text "ファイル名(N):" [aid="1090"] [ref=...]
    - ComboBox "ファイル名(N):" [aid="1148"] [ref=s1e523]
      - Edit "ファイル名(N):" [aid="1148"] [focused] [ref=s1e524]
      - Button "開く" [aid="DropDown"] [ref=...]
    - ComboBox "ファイルの種類(T):" [aid="1136"] [value="Text files (*.txt)"] [ref=...]
      - Button "開く" [aid="DropDown"] [ref=...]
    - Button "開く(O)" [aid="1"] [ref=s1e529]
    - Button "キャンセル" [aid="2"] [ref=s1e530]
    - Pane [aid="40965"] [ref=...]
      - ToolBar "ナビゲーション ボタン" [ref=...]
      - Edit "検索ボックス" [aid="SearchEditBox"] [ref=...]
    - TitleBar [aid="TitleBar"] [value="Open File"] [ref=...]
```

#### fill 結果

- **`fill s1e524 "C:\test.txt"` → 成功** (`result: true`)
- snapshot で `[value="C:\\test.txt"]` が設定されたことを確認
- Cancel ボタン（`aid="2"`）の click でダイアログ正常閉鎖

#### 注意点

- FileDialog は **`[modal]` フラグなし**で表示される。`Window "Open File"` として main window の子に配置される
- AutomationId `1148` は ComboBox と内部の Edit の両方に付与される。fill のターゲットは **Edit**（`ControlType=Edit`）
- ファイル名 Label の AutomationId は `1090`

### SaveFileDialog

#### snapshot 構造（抜粋）

```
- Window "ADACT SampleApp" [ref=s1e1]
  - Window "Save File" [focused] [ref=s1e557]
    - ToolBar "コマンド モジュール" [aid="FolderBandModuleInner"] [ref=...]
    - Pane "コントロール ホスト" [aid="ProperTreeHost"] [ref=...]
    - Pane "シェル フォルダー ビュー" [aid="listview"] [ref=...]
    - Text "ファイル名:" [aid="SaveDialogLabel"] [ref=...]
    - ComboBox "ファイル名:" [aid="FileNameControlHost"] [ref=s1e655]
      - Edit "ファイル名:" [aid="1001"] [focused] [ref=s1e656]
      - Button "開く" [aid="DropDown"] [ref=...]
    - Text "ファイルの種類:" [aid="SaveDialogLabel"] [ref=...]
    - ComboBox "ファイルの種類:" [aid="FileTypeControlHost"] [value="Text files (*.txt)"] [ref=...]
      - Button "開く" [aid="DropDown"] [ref=...]
    - Tree "保存フィールド" [aid="SaveDialogPreviewMetadataInner"] [ref=...]
    - Button "保存(S)" [aid="1"] [ref=s1e666]
    - Button "キャンセル" [aid="2"] [ref=s1e667]
    - Pane [aid="40965"] [ref=...]
    - TitleBar [aid="TitleBar"] [value="Save File"] [ref=...]
```

#### fill 結果

- **`fill s1e656 "C:\output.txt"` → 成功** (`result: true`)
- snapshot で `[value="C:\\output.txt"]` が設定されたことを確認
- Cancel ボタン（`aid="2"`）の click でダイアログ正常閉鎖

### AutomationId の差異（重要な発見）

| 要素 | OpenFileDialog | SaveFileDialog |
|------|---------------|----------------|
| ファイル名 ComboBox | `1148` | `FileNameControlHost` |
| ファイル名 Edit | `1148` | `1001` |
| ファイルの種類 ComboBox | `1136` | `FileTypeControlHost` |
| Open/Save ボタン | `1` | `1` |
| Cancel ボタン | `2` | `2` |

設計時の想定（AutomationId が共通）とは異なり、**ファイル名入力と種類フィルタの AutomationId は OpenFileDialog と SaveFileDialog で異なる**。ボタン（`1`, `2`）のみ共通。

### 各コマンド成否サマリ

| コマンド | OpenFileDialog | SaveFileDialog |
|---------|---------------|----------------|
| `click` (ボタンで開く) | 成功 | 成功 |
| `snapshot` (構造取得) | 成功 | 成功 |
| `fill` (ファイル名入力) | 成功 | 成功 |
| `click` (Cancel で閉じる) | 成功 | 成功 |

### 結論

- `fill` は OpenFileDialog・SaveFileDialog の両方で正常に動作する（ValuePattern 経由）
- フォールバック手順（focus → keypress Ctrl+A → type → keypress Enter）は不要
- AutomationId は Open と Save で異なるため、エージェントはダイアログの種別を判別する必要がある（Window タイトルで判別可能）
