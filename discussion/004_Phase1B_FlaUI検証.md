# Phase 1-B 検証メモ: FlaUI / UIA 動作確認

> 関連: [001\_要件定義.md](001_要件定義.md) / [002\_アーキテクチャ設計.md](002_アーキテクチャ設計.md) / [003\_実装計画.md](003_実装計画.md)
> 対象: Phase 1 セクション B「FlaUI / UIA 動作確認スパイク」

## 1. 結論サマリ

* **FlaUI.UIA3 を主バックエンドとする方針は妥当**。Win32 系（Notepad）も WinUI/UWP 系（電卓）も `list` / `snapshot` / `click(Invoke)` が問題なく動作。
* **UIA2 fallback の必要性は現時点では確認できず**。検証範囲（電卓・メモ帳）では UIA3 で取れない要素は無く、UIA2 と UIA3 で同じ要素にアクセスでき、Click も同等に成功。
  * ただし古い Win32 アプリ（旧 Notepad++ や独自描画コントロール）では UIA2 が必要になる可能性は残るため、**バックエンド差し替え可能な抽象（`AutomationBase`）は維持**しておく価値はある（FlaUI 標準 API でほぼコストなく実現可能）。
* **暫定判断**: `FlaUI.UIA3` を主、`FlaUI.UIA2` パッケージ参照は残す（必要時にバックエンド切替できる構造）。Phase 2 着手時に「UIA2 を実装に組み込むか」の最終判断を、Phase 2 ターゲットの追加（旧 Notepad++ 等）と合わせて下す。

## 2. プロジェクト構成

```
spike/flaui-b/
├── Adact.Spike.FlaUI.csproj   # net10.0-windows コンソールアプリ
├── Program.cs                 # CLI 実装（list / snapshot / click）
└── (生成物) snapshot-*.json   # 検証時のスナップショット
```

* **NuGet**:
  * `FlaUI.UIA3` 5.0.0
  * `FlaUI.UIA2` 5.0.0（B-3 比較用）

### 実装した CLI

| コマンド | 役割 |
| --- | --- |
| `list [--uia2]` | デスクトップ直下のトップレベルウィンドウ一覧（pid / proc / ControlType / title） |
| `snapshot <key> [--uia2]` | プロセス名 or タイトル部分一致でウィンドウを特定し、UIA ツリーを **フィルタ無し** で JSON 化して stdout に出力。stderr に nodes / bytes / ms / backend を出す |
| `click <key> <target> [--uia2]` | ウィンドウ内 Descendant を AutomationId 優先 → Name で検索し、`Button.Invoke()` 優先・失敗時は座標 `Click()` |

`<key>` はプロセス名一致（大文字小文字無視）→ タイトル部分一致 の順で解決。
`<target>` は AutomationId → Name の順で解決。
`--uia2` で UIA2 バックエンドに切替（未指定時は UIA3）。

## 3. 実行方法

```powershell
# ビルド
cd spike\flaui-b
dotnet build

# 実行
dotnet run --no-build -- list
dotnet run --no-build -- snapshot 電卓
dotnet run --no-build -- click 電卓 num7Button
dotnet run --no-build -- snapshot Notepad --uia2
```

> 注: テスト対象アプリ（電卓・メモ帳）を事前に起動しておく必要がある。

## 4. 検証結果

### B-1: メモ帳（Notepad++ 代替）

* **環境補足**: ホストに **Notepad++ がインストールされていなかった**ため、winget でのインストールはユーザー判断に委ねるべきと考え見送り、**Windows 11 標準のメモ帳（WinUI 製、`Notepad.exe`）で代替**実施した。これにより「Win32 代表」のターゲットは Phase 2 着手前に再検討が必要。
* `list`: `pid=7404 proc=Notepad ctrl=Window title="タイトルなし - メモ帳"` を検出。
* `snapshot`: 46 nodes / 15.4 KB / 90 ms（UIA3、フィルタ無し）。WinUI のボタン群（「太字 (Ctrl+B)」「新しいタブの追加」「リンク」等）が `Role: Button` / `AutomationId` 付きで取得できる。
* `click Notepad AddButton`: `Button.Invoke()` 成功。クリック後の snapshot で `TabItem` の数が **1 → 2** に増えており、UI 状態の変化を確認。
* **結論**: Win32 ターゲットを正式に旧 Notepad++ で再検証する余地は残るが、本検証範囲では UIA3 で問題なく動作。

### B-2: 電卓（calc.exe）

* `list`: `proc=ApplicationFrameHost ctrl=Window title="電卓"`（UWP の常で実プロセスは `CalculatorApp` だが、フロントは `ApplicationFrameHost`）。タイトル一致で問題なくアタッチ可能。
* `snapshot`: 54 nodes / 21.5 KB / 111 ms（UIA3、フィルタ無し）。
* `click 電卓 num7Button`: `Button.Invoke()` 成功。
* クリック後の snapshot で `AutomationId: CalculatorResults` の `Name` が **「表示は 7 です」** になっており、UI への反映を確認。
* **結論**: 電卓では UIA3 で完全に成功。

### B-3: UIA2 / UIA3 差異

| 対象 | バックエンド | nodes | bytes | elapsedMs | 操作結果 |
| --- | --- | ---: | ---: | ---: | --- |
| 電卓 | UIA3 | 54 | 21,991 | 111 | `num7Button.Invoke()` 成功 |
| 電卓 | UIA2 | 60 | 23,692 | 81 | `num8Button.Invoke()` 成功（表示「78」確認） |
| メモ帳 | UIA3 | 46 | 15,728 | 90 | `AddButton.Invoke()` 成功（タブ追加確認） |
| メモ帳 | UIA2 | 51 | 17,645 | 64 | （操作テストは未実施、tree 走査のみ） |

* **共通要素は両方で取得・操作可能**。同一の AutomationId（`num7Button`, `AddButton` 等）が両バックエンドで見える。
* UIA2 のほうが **ノード数がやや多い**（数 % レベル）。経験則的に UIA2 は無名 `Pane` / `Group` を素直に出す傾向があり、本検証もそれと整合。
* UIA3 で見えなかった要素は今回の範囲では **検出できず**。
* 速度は UIA3 < UIA2 という結果になったが（小サンプルなので参考値）、JIT ウォームアップ等の影響もあり差は実用上誤差レベル。

## 5. 気づき・ハマりどころ

1. **`.NET 8 SDK` が当該マシンに無く `.NET 10 SDK` のみ**だったため、TargetFramework は `net10.0-windows` を採用。`net10.0`（非 Windows 派生）では `FlaUI.Core` が `NU1701` で .NET Framework 4.x として復元されてしまうため、`-windows` 修飾が必須。Phase 2 で .NET 8 LTS に揃えるかは別途決定要。
2. **`dotnet new console -f net8.0` が失敗**（targeting pack 不在）。 `-f` を外して既定の `net10.0` で生成し、csproj を `net10.0-windows` に書き換える流れにした。
3. **Process 名と表示プロセスの差**: 電卓は `proc=ApplicationFrameHost`（UWP 共通の親）として現れる。プロセス名で attach する設計はそのままだと UWP に弱い。**Phase 2 では「タイトル部分一致」フォールバックを必須**にしておくとよい。本スパイクでもその方式で問題なく解決できた。
4. **`Button.Invoke()` を最優先にしたのは正解**。座標 `Click()` だとフォーカスや前面状態に依存しやすい。Invoke パターン非対応のときだけ Click にフォールバックする現実装方針は Phase 2 にもそのまま流用可。
5. **UIA ツリーの `FindAllChildren()` 内で例外**が個別ノードで発生し得る（権限・タイミング）。各ノードを try/catch で囲む方針にした。Phase 2 でも同様の防御は必要。
6. **PowerShell のリダイレクト警告**: `dotnet run` の stderr を `2>` でファイルにリダイレクトすると PowerShell が `RemoteException` 警告を出すが、コンテンツは正しく分離されている（実害なし）。

## 6. ツリーサイズ参考値（Phase 1-C への入力）

| 対象 | バックエンド | nodes | bytes | KB |
| --- | --- | ---: | ---: | ---: |
| 電卓（メイン画面） | UIA3 | 54 | 21,991 | 21.5 |
| 電卓（メイン画面） | UIA2 | 60 | 23,692 | 23.1 |
| メモ帳（タブ1） | UIA3 | 46 | 15,728 | 15.4 |
| メモ帳（タブ1） | UIA2 | 51 | 17,645 | 17.2 |
| メモ帳（タブ2 追加後） | UIA3 | — | 33,420 | 32.6 |

* いずれも **フィルタ無し**（`ControlType` 絞り込みや無名 Pane 除外を一切していない）の値。
* 想定する Phase 1-C のフィルタ（操作可能 ControlType への絞り込み・無名 Pane 除外）を入れれば **数 KB レベル**まで圧縮できると見込まれる。
* メモ帳のタブ追加で 15→33 KB に倍増している点に注意（タブ × 編集領域がリニアにツリーを増やす）。エディタ系では「アクティブタブ配下のみを返す」など、サブツリー絞り込みのルール検討が必要。

## 7. UIA2 fallback の要否（暫定判断）

* **現時点では fallback の積極的な実装は不要**。
* ただし、**バックエンド切替の抽象（`AutomationBase` 受け渡し）は Phase 2 のコードベースにも残す**。UIA2 への差し替えは FlaUI の標準 API でほぼコストなく可能なため、保険として残す価値が高い。
* **再検討タイミング**: 旧 Notepad++ や WPF 以前の業務アプリを Phase 2 ターゲットに加えるとき。

## 8. Phase 2 への申し送り

* `Adact.Spike.FlaUI` の `BuildNode` / `FindWindow` / `Click` の構造はそのまま Phase 2 の Snapshot Builder / Tool Router の土台に流用可能。
* Phase 1-C のフィルタ仕様が決まり次第、`BuildNode` に「ControlType ホワイトリスト」「無名 Pane 除外」「サブツリー指定」を入れる。
* Process 名による attach は **UWP/WinUI で破綻**するため、Phase 2 では「タイトル部分一致」と「PID 直指定」を主とし、プロセス名は補助とする方針が妥当。
* TargetFramework は最終的に `.NET 8`（LTS）で揃えるべきか、`net10.0-windows` のままにするか、Phase 2 着手前に決定する（本スパイクは `.NET 10 SDK` の都合で後者を採用）。

***

## 9. B-1 再実行（Notepad++ 実機 / 2026-04-25 追記）

ユーザー判断により Notepad++ を `winget` で導入し、本来の Win32 代表である Notepad++ で B-1 を再実行した。

### 9.1 環境

* `winget install -e --id Notepad++.Notepad++`（v8.9.3、ARM64 ビルドが導入された）
* インストール先: `C:\Program Files\Notepad++\notepad++.exe`
* `list` 結果: `pid=9920 proc=notepad++ ctrl=Window title="新規1 - Notepad++"`
* バックエンドは UIA3 のみで検証（UIA2 比較は B-3 の Windows 標準メモ帳結果を流用）

### 9.2 アタッチ

* プロセス名 `notepad++` で問題なくアタッチ可能（FindWindow の process 名一致パスでヒット）。
* タイトル部分一致でも `Notepad++` でヒット可能。

### 9.3 ツリー走査結果

* **L0 ノード数**: **69 ノード / 15,369 byte (15.0 KB)** / 経過 161 ms。
* 主な構成:
  * ルート `Window`（ClassName=`Notepad++`）
  * `Pane`（`Scintilla`）— 編集領域。**Name / AutomationId は両方とも空**。
  * `Tab`（`SysTabControl32`）→ 子に `TabItem "新規1"`
  * `StatusBar`（`msctls_statusbar32`）→ `Text` × 6（`Normal text file`, `長さ:0 行数:1`, ...）
  * `Pane`（`ReBarWindow32`）→ 子の `ToolBar`（`ToolbarWindow32`）
    * **その下に `Button` が 32 個並ぶが、いずれも Name / AutomationId が空**（後述）
  * `MenuBar`（システムメニュー: 1 項目）/ `TitleBar` / 別の `MenuBar`（アプリケーション: `ファイル(F)`, `編集(E)`, `検索(S)`, `表示(V)`, `エンコード(N)`, ...）

### 9.4 クリック試験

* `click "notepad++" "ファイル(F)"` → MenuBar 内 MenuItem を Name でヒットし `Button.Invoke()` 成功。File メニューが展開される（手動で Esc で閉じた）。
* **ツールバーの「保存」ボタンは AutomationId / Name のどちらでも特定不能**。`Role: Button, Name: null, AutomationId: null` のボタンが 32 個連続している状態のため、AutomationId/Name 経由のクリックは原理的に不可能。

### 9.5 旧来 Win32 アプリ特有の落とし穴

| 落とし穴 | 詳細 | Phase 2 以降の対応 |
| --- | --- | --- |
| **無名 `Button` の連続発生** | Notepad++ 標準 ToolBar の Button × 32 は Name / AutomationId が一切無い（Win32 ToolbarWindow32 の典型）。AI から識別不能 | (a) ツールチップの動的取得、(b) ボタン位置（Rect）+ 連番ベースのインデックス補助、(c) メニュー経由の代替操作の優先提示、いずれかが必要 |
| **編集領域 (`Scintilla` Pane) が無名** | テキスト本文の取得・入力ターゲットになる中核要素なのに `Name` / `AutomationId` が空。ClassName でしか識別できない | Phase 2 で「ClassName / Role による特定の補助」を Ref 解決に追加検討 |
| **MenuBar が複数ある** | システムメニュー用 `MenuBar`（子1個）と、アプリケーション用 `MenuBar`（`ファイル(F)` 等を持つ）が並列に出る | Snapshot 上は問題ないが、AI へ提示する際に「メインメニュー」と「システムメニュー」を区別できると親切 |
| **Tab の `TabItem.Name` がアンダースコア未含み** | Notepad++ のタブ名は表示通り（`新規1` など）取得できる。ここは素直 | 特になし |
| **StatusBar の `Text` ノードに Rect=0 のものが混入** | 一部の `Text` ノードが `Rect: [0,0,0,0]` で、可視性を Rect で判定するロジックは要注意 | フィルタで Rect 全 0 を除外する/しないは Phase 2 で要検討（情報量と実用性のトレードオフ） |
| **ToolBar コンテナを切ると操作要素が露出** | `ToolBar` を ControlType で除外すると、その下の無名 Button が **そのままルート直下に flatten** される。フィルタ実装は flatten 方式必須 | 実装で対応済み（Phase 1-C の `BuildFiltered`） |

### 9.6 結論（B-1 更新版）

* **Notepad++ も UIA3 で問題なくアタッチ・ツリー取得・メニュー経由クリックが可能**。Phase 1-B の暫定判断（UIA3 主・UIA2 fallback は当面不要）に変更なし。
* 一方で **「Win32 代表」として真にケアすべき問題は UIA バックエンドの差ではなく、ToolBar 系の `Button` が無名で量産される点** にあることが判明。これは Phase 2 のセレクタ戦略・Snapshot 提示形式の設計に直接影響する。
* **Phase 2 申し送り（更新）**:
  * Snapshot 出力に **位置ベースの暗黙インデックス**（同じ親内での同 Role 連番）を付与する案を検討（無名 Button 群を `Button[15]` のように扱える）。
  * **ツールチップ取得 API**（`Properties.HelpText` 等）を Snapshot Builder で拾えるか調査タスクを追加。
  * 「メニュー経由代替パスの提示」を AI 向けの暗黙の優先順位として持たせる（無名 ToolBar Button を最後にする）。
