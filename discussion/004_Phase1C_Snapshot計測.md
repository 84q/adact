# Phase 1-C 検証メモ: Snapshot サイズ計測

> 関連: [001\_要件定義.md](001_要件定義.md) / [002\_アーキテクチャ設計.md](002_アーキテクチャ設計.md) / [003\_実装計画.md](003_実装計画.md) / [004\_Phase1B\_FlaUI検証.md](004_Phase1B_FlaUI検証.md)
> 対象: Phase 1 セクション C「Snapshot サイズ計測」
> 実施日: 2026-04-25

## 1. 結論サマリ

* **L1（操作可能 ControlType のみ）に絞れば、検証 3 アプリすべてで 22 KB 以下** に収まり、AI コンテキスト的に十分許容範囲。
* **仕様の文字通りの L1（操作可能型のみ）と L2（さらに無名 Pane/Group 除外）は、Pane/Group が L1 で既に全削除されるため値が完全一致** する。L2 を「意味のある段階」にするには L1 を構造保持型まで広げる必要がある。
* そこで本計測では **L1.5 = L1 + 構造保持型（ToolBar / MenuBar / StatusBar / TitleBar / Pane / Group / List / Tree 等）かつ無名は除外** の派生案を追加検証。L1.5 は L1 より +15〜+50% のサイズ増だが、UI の構造（ツールバーの存在、ステータスバーの存在）が AI に伝わる。
* **Phase 2 の本実装で採用する初期フィルタは L1.5（=フィルタルール v1）を推奨**。ただし Notepad++ のような「無名 Button × 32」が ToolBar 配下にぶら下がるケースでは ToolBar をさらに簡約する追加ルールが必要（後述）。

## 2. 計測手順

### 2.1 対象アプリ

| アプリ | バージョン / 備考 | 起動状態 |
| --- | --- | --- |
| Notepad++ | 8.9.3 (winget; ARM64 ビルド) | 新規1（空ファイル）を開いた状態 |
| 電卓 | Windows 11 同梱 | 起動直後・標準モード |
| エクスプローラー | Windows 11 同梱 | `C:\Users\yuta_\dev\adact` を開いた状態（ファイル 3 個） |

### 2.2 計測コード

[`spike/flaui-b/Program.cs`](../spike/flaui-b/Program.cs) の `measure` サブコマンドを追加実装。既存 `Adact.Spike.FlaUI` を拡張する形（B-1 の `BuildNode` ロジックを流用しつつフィルタを差し込めるよう `BuildFiltered` に置き換え）。

```powershell
cd spike\flaui-b
dotnet build
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
dotnet run --no-build -- measure "notepad++"
dotnet run --no-build -- measure "電卓"
dotnet run --no-build -- measure "explorer"
```

* 各アプリ × 各フィルタ で個別 JSON ファイル `measure-<key>-<level>.json` を出力。
* JSON は `WriteIndented = true` でインデント整形。バイトサイズは UTF-8 BOM 無し基準。

### 2.3 フィルタの定義（実装と完全に対応）

* **L0 — フィルタなし**: UIA ツリー全要素を JSON 化。
* **L1 — ControlType ホワイトリスト（仕様文言通り）**:
  * 操作可能: `Button` / `Edit` / `ListItem` / `Window` / `Menu` / `MenuItem` / `CheckBox` / `ComboBox` / `Tab` / `TabItem` / `Hyperlink` / `Text` / `Document` / `TreeItem`
  * 操作可能の補強（仕様の「等」に含むと判断）: `RadioButton` / `SplitButton` / `Slider` / `Spinner`
  * **Pane / Group / ToolBar / MenuBar / StatusBar / TitleBar 等の構造保持型は除外**。
* **L1.5 — 派生案（v1 のたたき台）**: L1 + 構造保持型を許可。ただし `Pane` / `Group` のうち **Name と AutomationId が両方空のもの** は除外。
  * 追加許可: `Pane` / `Group` / `ToolBar` / `MenuBar` / `StatusBar` / `TitleBar` / `Header` / `HeaderItem` / `List` / `Tree` / `DataGrid` / `DataItem` / `Table` / `Custom`
* **L2 — 仕様文言通り**: L1 + 「無名 Pane / Group 除外」。Pane / Group は L1 で既に除外済みのため、結果は L1 と同値となる（後述の評価で詳説）。

除外時の処理は **flatten**（除外要素自身は出力しないが、子ツリーは親に直接ぶら下げる）。これにより構造の連続性は保たれ、サイズだけが減る。

### 2.4 出力 JSON スキーマ

```json
{
  "Ref": "w62",
  "Role": "Window",
  "Name": "新規1 - Notepad++",
  "AutomationId": "(任意)",
  "ClassName": "Notepad++",
  "Rect": [x, y, w, h],
  "Children": [ ... ]
}
```

* `Ref` はフィルタ後ノードに採番（フィルタごとに独立採番。同じ要素でもレベルが違えば異なる ref になる）。
* null / 空文字フィールドは出力スキップ（`DefaultIgnoreCondition = WhenWritingNull`）。

## 3. 計測結果

### 3.1 計測結果表

| アプリ | level | nodes | bytes | KB |
| --- | --- | ---: | ---: | ---: |
| **Notepad++** | L0 | 69 | 15,369 | 15.0 |
|  | L1 | 62 | 9,936 | 9.7 |
|  | L1.5 | 65 | 11,653 | 11.4 |
|  | L2 | 62 | 9,936 | 9.7 |
| **電卓** | L0 | 57 | 23,880 | 23.3 |
|  | L1 | 47 | 14,203 | 13.9 |
|  | L1.5 | 55 | 21,190 | 20.7 |
|  | L2 | 47 | 14,203 | 13.9 |
| **エクスプローラー** | L0 | 119 | 50,837 | 49.6 |
|  | L1 | 83 | 22,343 | 21.8 |
|  | L1.5 | 98 | 35,437 | 34.6 |
|  | L2 | 83 | 22,343 | 21.8 |

すべて **UIA3** バックエンド。インデント整形済み（最終フォーマットは v1 で minified を選ぶ余地あり、その場合は概ね 60〜70% のサイズになる）。

### 3.2 観察

* **L1 = L2** が 3 アプリすべてで成立: 仕様文言通りに読むと L2 は L1 と同義になる。
* **削減率**:
  * Notepad++: L0 → L1 で **35% 圧縮**（69→62 ノード、15.0→9.7 KB）
  * 電卓: L0 → L1 で **41% 圧縮**（57→47 ノード、23.3→13.9 KB）
  * エクスプローラー: L0 → L1 で **56% 圧縮**（119→83 ノード、49.6→21.8 KB）
* **L1.5 のオーバーヘッド**: L1 比 +5〜+50%。エクスプローラーは構造保持型（ToolBar / Pane の Name 付きナビゲーション領域など）が多く、増分が顕著。
* **電卓は WinUI 系で Pane の階層が深い**: L0 と L1.5 の差が小さい（23.3→20.7 KB）。これは「構造保持型のうち Name 付きのもの」が大部分を占めるため。

## 4. JSON サンプル抜粋（各アプリ × L2）

### 4.1 Notepad++（L2 ルート抜粋）

```jsonc
{
  "Ref": "w62", "Role": "Window", "Name": "新規1 - Notepad++", "ClassName": "Notepad++",
  "Rect": [0, 0, 512, 350],
  "Children": [
    { "Ref": "w2", "Role": "Tab", "Name": "Tab", "ClassName": "SysTabControl32",
      "Rect": [7, 88, 499, 234],
      "Children": [
        { "Ref": "w1", "Role": "TabItem", "Name": "新規1", "Rect": [8, 89, 95, 22] }
      ]
    },
    { "Ref": "w3", "Role": "Text", "Name": "Normal text file", "Rect": [0, 0, 0, 0] },
    { "Ref": "w4", "Role": "Text", "Name": "長さ: 0    行数: 1", "Rect": [0, 0, 0, 0] },
    { "Ref": "w5", "Role": "Text", "Name": "行: 1    桁: 1    位置: 1", "Rect": [7, 323, 229, 21] },
    { "Ref": "w6", "Role": "Text", "Name": "Windows (CR LF)", "Rect": [237, 323, 109, 21] }
    /* ... 計 32 個の無名 Button が ToolBar から flatten されてここに直接出る ... */
  ]
}
```

⚠️ ToolBar がフィルタで除去されると、その配下の **無名 Button × 32 がルート直下に並ぶ**。AI から見ると「何の Button かわからない」群が目立つ。L1.5 を採用すれば `ToolBar` でグルーピングされて読みやすくなる（その代わり +1.7 KB）。

### 4.2 電卓（L2 ルート抜粋）

```jsonc
{
  "Ref": "w47", "Role": "Window", "Name": "電卓", "ClassName": "ApplicationFrameWindow",
  "Rect": [77, 0, 1213, 939],
  "Children": [
    { "Ref": "w5", "Role": "Window", "Name": "電卓", "AutomationId": "TitleBar",
      "ClassName": "ApplicationFrameTitleBarWindow", "Rect": [1096, 1, 188, 32],
      "Children": [
        { "Ref": "w1", "Role": "MenuItem", "Name": "システム", "Rect": [0, 0, 0, 0] },
        { "Ref": "w2", "Role": "Button",   "Name": "電卓 の最小化", "AutomationId": "Minimize", "Rect": [1146, 1, 46, 32] },
        { "Ref": "w3", "Role": "Button",   "Name": "電卓 を最大化する", "AutomationId": "Maximize", "Rect": [1192, 1, 46, 32] },
        { "Ref": "w4", "Role": "Button",   "Name": "電卓 を閉じる", "AutomationId": "Close", "Rect": [1238, 1, 46, 32] }
      ]
    },
    { "Ref": "w46", "Role": "Window", "Name": "電卓", "ClassName": "Windows.UI.Core.CoreWindow", /* ... 各種ボタン群 ... */ }
  ]
}
```

WinUI/UWP は **Name と AutomationId の両方を提供**するため、AI から極めて操作しやすい（`AutomationId: Minimize` など意味のある ID）。Win32 系（NPP）と対照的。

### 4.3 エクスプローラー（L2 ルート抜粋）

```jsonc
{
  "Ref": "w83", "Role": "Window", "Name": "adact - エクスプローラー", "ClassName": "CabinetWClass",
  "Rect": [306, 107, 1139, 641],
  "Children": [
    { "Ref": "w1", "Role": "Text", "Name": "3 個の項目", "AutomationId": "PropertyValue", "ClassName": "MetadataLabel" },
    { "Ref": "w2", "Role": "RadioButton", "Name": "詳細", "AutomationId": "ViewMode_Details" },
    { "Ref": "w3", "Role": "RadioButton", "Name": "大きいアイコン", "AutomationId": "ViewMode_LargeIcons" },
    { "Ref": "w4", "Role": "Button", "Name": "1 行上", "AutomationId": "UpButton" },
    { "Ref": "w5", "Role": "Button", "Name": "下へドラッグ", "AutomationId": "DownPageButton" }
    /* ... */
  ]
}
```

エクスプローラーも AutomationId が概ね揃っており、意味のあるラベルが取れる（ただしファイル一覧領域そのものは Pane の構造が深い）。

## 5. AI コンテキスト適合性の評価

| 観点 | 評価 |
| --- | --- |
| 単一アプリ Snapshot のサイズ目標（数 KB〜十数 KB） | **L1 / L2 で 10〜22 KB**: ほぼ目標内。インデント無しなら 7〜15 KB に収まる |
| 複数アプリ同時 Snapshot（ウィンドウ列挙ユースケース） | フィルタ後でもアプリあたり平均 15 KB 程度 → 5 アプリで 75 KB。**現実的な MCP コンテキスト上限を意識した分割が必要** |
| ノード数（AI が同時に追える ID 数） | L1 で **40〜85 ノード**: AI（特に Claude/GPT-4 級）にとって扱いやすい範囲 |
| エディタ系（タブ追加でリニア増加） | Phase 1-B の知見通り、Notepad++ もタブが増えると倍増する想定 → 「アクティブタブ配下のみ」のサブツリー絞り込みが Phase 6 までに必須 |

**結論**: AI コンテキストに乗るサイズに収まる。ただし「同時に複数アプリを Snapshot する」「タブが多いエディタ」のケースでは追加の絞り込みが要る。

## 6. フィルタルール v1（Phase 2 本実装のたたき台）

### 6.1 v1 の基本形 — L1.5 を採用

| 区分 | ControlType | 扱い |
| --- | --- | --- |
| **操作可能（必ず残す）** | `Button` / `Edit` / `ListItem` / `MenuItem` / `CheckBox` / `ComboBox` / `RadioButton` / `Tab` / `TabItem` / `Hyperlink` / `TreeItem` / `SplitButton` / `Slider` / `Spinner` | 常に残す |
| **状態表示（残す）** | `Text` / `Document` / `Window` | 常に残す（入力先・読み取り対象） |
| **構造保持（条件付きで残す）** | `Pane` / `Group` | **Name または AutomationId のいずれか非空** のときのみ残す。両方空なら **flatten** |
| **コンテナ（残す、ただし圧縮対象）** | `ToolBar` / `MenuBar` / `StatusBar` / `TitleBar` / `Header` / `List` / `Tree` / `DataGrid` / `Table` / `Custom` | 残すが、子要素のさらなる圧縮を別途検討（後述 6.3） |
| **その他** | 上記以外 | flatten（除外） |

### 6.2 補助ルール

* **flatten 方式**: 除外したコンテナの子は親に直接昇格させる（情報を完全に失わない）。
* **Rect 全 0 ノードの扱い**: 標準では残す（Notepad++ StatusBar の `Text` のように、Rect=0 でも論理的には情報がある）。Phase 2 で AI 提示時に「画面外」フラグを付ける。
* **`AutomationId` 優先のセレクタ生成**: 残すノードの ref 解決は AutomationId → Name → (Role + 親内インデックス) の順。

### 6.3 v1 で残る課題（Phase 2 で対応）

1. **無名 Button が ToolBar 配下に大量発生するケース（Notepad++ 系）**:
   * v1 では `ToolBar` ごと残すのみで、配下の無名 Button × 32 はそのまま並ぶ。
   * **追加ルール案 (v1.1)**: 同じ親内に同 Role かつ Name/AutomationId が完全に空のノードが N 個（例: 5 個）以上連続する場合、最初の数個だけ残し残りを `{ "Role": "Button", "Name": "(... 27 more unnamed)", "kind": "summary" }` のような要約ノードに置換。
   * **追加ルール案 (v1.2)**: 無名 Button に対して `Properties.HelpText`（ツールチップ）の取得を試行し、ある場合は仮想的な Name として注入する。
2. **同じ Role が並ぶ子の暗黙インデックス**:
   * `Button[0]` 〜 `Button[N]` のような兄弟内連番を Snapshot 時点で付与する案。Phase 2 で要決定。
3. **エディタ系のサブツリー絞り込み**:
   * `Tab` 配下の非アクティブ `TabItem` のサブツリーを省略するオプション（`--scope active-tab` 等）。
4. **JSON フォーマット**:
   * インデント有り（人間可読、現状）vs minified（AI 渡し用）。Phase 2 で 2 モード切替を実装。

## 7. 気づき・ハマりどころ

1. **仕様文面の L1/L2 は実質一段階**: 「L1 で操作可能 ControlType に絞り、L2 でさらに無名 Pane/Group を除外」は Pane/Group が L1 で既に除外されるため二段階にならない。フィルタを 2 段階にしたいなら、L1 で構造保持型を含めるのが自然（→ 本書の L1.5 案）。
2. **ToolBar をコンテナごと潰すと UI 構造が読めなくなる**: Notepad++ ケースで顕著。実用上は ToolBar / MenuBar / StatusBar 等のラベルは残すべき。
3. **エクスプローラーをタイトル `adact` で引くと VS Code とぶつかる**: VS Code のウィンドウタイトルにも `adact` が含まれており、`FindWindow` のタイトル部分一致で先にヒットしてしまった。**プロセス名 `explorer` 指定で解決**。Phase 2 では「同じキーで複数ウィンドウがマッチした場合の警告／曖昧性解消」が必要。
4. **`--out <dir>` 引数の取り回し**: 既存 CLI が positional 引数だけを抽出する作りで、`--out` のような名前付き引数を渡すと positional から除外されて評価対象外となるため、出力先指定が無視されてカレントに落ちる挙動になった（実害は無いが Phase 2 の本実装では引数パーサを真面目にする）。
5. **Tab + 非アクティブ TabItem は隠れたコスト**: 今回の Notepad++ は新規1（タブ1個）で計測しただけだが、Phase 1-B でタブ追加時に倍増する事実が確認済み。サブツリー絞り込みは「あったら便利」ではなく「必須」レベル。
6. **コンソール文字化け**: PowerShell では `[Console]::OutputEncoding = [System.Text.Encoding]::UTF8` を実行前に設定しないと日本語タイトルが化ける。Phase 2 のドキュメントに明記する。
7. **電卓の `proc=ApplicationFrameHost`**: B-1 の知見の通り。プロセス名指定でも `electron`/`ApplicationFrameHost` の同居プロセスが多いと問題になるため、Phase 2 では PID 直指定パスも整える。

## 8. Phase 2 への申し送り

* **採用フィルタ**: v1 (= L1.5 相当) を初期実装に採用。`Snapshot Builder` には `FilterPolicy` を差し替え可能な構造を入れる（v1 / v0=L0 / v1-strict=L1 / 将来の v2 を切替）。
* **ToolBar 内無名 Button 対策**: 6.3 の v1.1（要約ノード）と v1.2（HelpText 取得）の両方を Phase 2 のスパイクとして用意する。
* **暗黙インデックス**: `Button[0..N]` 形式の兄弟連番を ref に追加。
* **サブツリー絞り込みオプション**: `windows_snapshot` ツールに `scope` パラメータ（`window` / `active-tab` / `subtree:<ref>`）を最初から設計する。
* **同名ウィンドウの曖昧性**: PID/タイトル/プロセス名のいずれか 1 つで一意に絞れない場合は、候補リストを返してエラーにする UX を MCP ツールに組み込む。
* **計測スパイク自体の流用**: `Adact.Spike.FlaUI` の `measure` サブコマンドはそのまま Phase 2 の回帰テスト（フィルタ調整後にサイズ・ノード数の差分を見る）に使える。

***

## 付録 A: 成果物ファイル

* コード: [`spike/flaui-b/Program.cs`](../spike/flaui-b/Program.cs)（`measure` サブコマンドを追加）
* 計測 JSON（spike/flaui-b/ 直下）:
  * `measure-notepad__-{L0,L1,L1_5,L2}.json`
  * `measure-電卓-{L0,L1,L1_5,L2}.json`
  * `measure-explorer-{L0,L1,L1_5,L2}.json`
  * `measure-adact-{L0,L1,L1_5,L2}.json` （タイトル一致が VS Code に流れた誤マッチ例。除外可）
