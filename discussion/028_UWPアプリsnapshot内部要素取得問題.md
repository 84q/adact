# 028 UWP アプリ（電卓）の snapshot で内部要素が取得できない問題

> 関連: [027 操作ブロック検知](027_操作ブロック検知.md)
> 作成日: 2026-05-01 (JST)

---

## 1. 現象

電卓（Calculator / UWP アプリ）を attach して `snapshot --filter raw` を実行したところ、以下の要素しか取得できない。

```
- Window "電卓" [ref=s1e1]
  - Window "電卓" [aid="TitleBar"] [value="電卓"] [ref=s1e2]
    - MenuBar "システム" [aid="SystemMenuBar"] [ref=s1e3]
      - MenuItem "システム" [ref=s1e4]
    - Button "電卓 の最小化" [aid="Minimize"] [ref=s1e5]
    - Button "電卓 を最大化する" [aid="Maximize"] [ref=s1e6]
    - Button "電卓 を閉じる" [aid="Close"] [ref=s1e7]
  - Pane [ref=s1e8]   # ← コンテンツ領域。子要素なし。
```

数字ボタン（"1", "2", "+" など）が **一切取得できない**。

---

## 2. 調査結果

### 2.1 UIA tree の構造

電卓は `ApplicationFrameHost.exe`（UWP ホスト）内で動作する。

```
ApplicationFrameWindow（外枠）
├── Window "電卓"（TitleBar）
│   ├── MenuBar "システム"
│   ├── Button "最小化"
│   ├── Button "最大化"
│   └── Button "閉じる"
└── Pane ApplicationFrameInputSinkWindow（コンテンツ領域）
    └── [数字ボタン等はここに存在するはず]
```

`inspect s1e8` の結果:

```json
{
  "ref":"s1e8",
  "name":null,
  "controlType":"Pane",
  "className":"ApplicationFrameInputSinkWindow",
  "boundingRect":{"x":0,"y":32,"width":1710,"height":955},
  "isEnabled":true,
  "isOffscreen":false,
  ...
}
```

### 2.2 原因

| 要因 | 説明 |
| --- | --- |
| UWP アプリのサンドボックス化 | 電卓は `ApplicationFrameHost.exe` 内で隔離実行され、UIA tree の一部が隠蔽される |
| FlaUI の `FindAllChildren()` | UWP の `CoreWindow` 内部要素を UIA3 経由で取得できない場合がある |
| 既知の制約 | FlaUI の issue でも UWP アプリ（特に Windows Calculator、Store アプリ）の要素取得が報告されている |

### 2.3 press/type が動作する理由

`press` / `type` は **キーボードイベント**を対象ウィンドウに直接送るため、UIA tree を介さずに動作する。つまり「要素は見えないが、ウィンドウ自体にはキー入力を届けられる」状態。

---

## 3. 解決策候補

### 案 A: `FindAllChildren` → `FindAllDescendants` に変更

`FlaUiElement.Children` の `_el.FindAllChildren()` を `_el.FindAllDescendants()` に変更すると、Pane の内部まで掘り下げて取得できる可能性がある。

- **利点**: 実装が単純。1 行変更で済む可能性。
- **リスク**: 巨大な UIA tree（ブラウザ、IDE 等）でパフォーマンスが悪化する。すべてのアプリで深い走査が走る。

### 案 B: UWP 専用のフォールバック

`ApplicationFrameInputSinkWindow` 以下の要素を取得するために、FlaUI の `RawViewWalker` や `ControlViewWalker` を使う専用ロジックを追加する。

- **利点**: UWP アプリに特化した精度向上。
- **リスク**: UWP アプリ固有のハックになり、保守性が下がる。他の UWP アプリでも同じ問題が出るか未確認。

### 案 C: ドキュメント化 + 代替手段提供

UWP アプリの snapshot 取得制約を docs に明記し、`press` / `type` / `mouse-move` などの「要素を介さない操作」を推奨する。

- **利点**: 現状維持で安全。実装コストゼロ。
- **リスク**: ユーザーが UWP アプリのボタンを `click` で操作できない。

---

## 4. 検証結果

### 4.1 案 A（`FindAllDescendants`）の検証結果

| アプリ | FindAllChildren | FindAllDescendants | 時間（Children/Descendants） | 数字ボタン |
| --- | --- | --- | --- | --- |
| 電卓 | 3 | 52 | 16ms / 19ms | ✅ 0-9 取得可能 |
| Notepad++ | 2 | 23 | 11ms / 11ms | N/A | 3ボタン |

**重要な発見**: `FindAllDescendants` で電卓の数字ボタン（0-9）が**取得できた**。パフォーマンスはほぼ同等（16ms vs 19ms）。

### 4.2 UIA ツリーの詳細構造

電卓の `ApplicationFrameWindow` の子要素:

```
ApplicationFrameWindow（外枠）
├── Window "電卓" [Class: ApplicationFrameTitleBarWindow]
│   ├── MenuBar "システム"
│   ├── Button "最小化"
│   ├── Button "最大化"
│   └── Button "閉じる"
├── Window "電卓" [Class: Windows.UI.Core.CoreWindow]  ← 実際の UWP コンテンツ
│   ├── Text "電卓" [Class: TextBlock]
│   └── Custom [Class: Custom]  ← この内部にボタンが存在
│       └── Group
│           └── Button "1"
│           └── Button "2"
│           └── ...
└── Pane [Class: ApplicationFrameInputSinkWindow]  ← 子要素なし（0）
```

**核心的事実**:
- `CoreWindow.FindAllChildren()`: **2 要素のみ**（TextBlock, Custom）
- `CoreWindow.FindAllDescendants()`: **44 要素**（うちボタン 33）
- `ApplicationFrameInputSinkWindow.FindAllChildren()`: **0 要素**
- `ApplicationFrameInputSinkWindow.FindAllDescendants()`: **0 要素**

つまり、ボタンは `ApplicationFrameInputSinkWindow` ではなく **`Windows.UI.Core.CoreWindow` の深い階層**に存在する。

### 4.3 案 B（TreeWalker）の検証結果

FlaUI 5.0.0 の API で `GetRawTreeWalker()` は利用不可。`FindAllDescendants` で代替可能なため、案 B は不要と判断。

### 4.4 新たな課題

`FindAllDescendants()` は**フラットなリスト**を返すため、UIA ツリーの階層構造が失われる。例:

```
階層構造（期待）:
Window
└── Custom
    └── Group
        └── Button "1"

FindAllDescendants の結果（フラット）:
[Window, Custom, Group, Button "1"]
```

このため、`SnapshotBuilder` の再帰構造と整合させるには、親子関係の復元が必要。

---

## 5. 実装方針の決定

### 採用案: **案 A + UWP 特化ハンドリング**

`FlaUiElement.Children` で通常は `FindAllChildren()` を使い、UWP の特殊要素（`Windows.UI.Core.CoreWindow`）に対してのみ `FindAllDescendants()` で深く掘る。

**理由**:
- Notepad++ 等の Win32 アプリには**影響なし**
- 電卓等の UWP アプリで**ボタンを取得可能**
- パフォーマンス劣化**ほぼなし**

**実装方法**:
1. `FlaUiElement.Children` で `ClassName == "Windows.UI.Core.CoreWindow"` の場合、`FindAllDescendants()` を使う
2. `FindAllDescendants` の結果から、現在の要素 `_el` 自身を除外し、残りを子として返す
3. **階層構造はフラット化される**（孫要素が子として表示される）— これは既知の制約

### 代替案: **現状維持 + ドキュメント化（案 C）**

UWP アプリの制約を docs に明記し、`press` / `type` を推奨する。

- **利点**: 実装リスクゼロ
- **欠点**: UWP アプリの `click` 操作が制限される

---

## 6. 今後の検証方針

1. **実装の検証**: `CoreWindow` 特化ハンドリングを実装し、電卓で snapshot を再取得
2. **他の UWP アプリ**: 設定アプリ、メールアプリ等でも同じ問題が出るか確認
3. **階層復元の検討**: `FindAllDescendants` の結果から親子関係を復元する方法の有無
4. **判断**: 実装の複雑さと効果を比較し、採用するか現状維持（案 C）にするか決定

---

## 5. 関連情報

- `src/Adact.Engine/Elements/FlaUiElement.cs`: `Children` プロパティの `FindAllChildren()` 呼び出しが該当箇所
- `src/Adact.Engine/Snapshot/SnapshotBuilder.cs`: `BuildNode` で再帰的に `el.Children` を走査
- 電卓は `ApplicationFrameHost.exe` プロセス内で実行されるため、`ProcessName` が `ApplicationFrameHost` になる

---

## 6. 未決事項

| 項目 | 決めること |
| --- | --- |
| 検証優先順位 | 案 A を先に試すか、案 B を先に試すか |
| パフォーマンス基準 | `FindAllDescendants` に変更した場合、どの程度のパフォーマンス劣化を許容するか |
| 対象アプリ範囲 | 電卓だけでなく、他の UWP アプリ（メール、写真等）でも同じ問題が出るか調査するか |
| 代替操作の推奨 | `press` / `type` / マウス座標操作をどの程度推奨するか |

---

## 7. 完了記録

**完了日: 2026-05-01**

### 採用した方針

- **案 A（`FindAllDescendants` 適用）を採用**。
- `CoreWindow`（`Windows.UI.Core.CoreWindow`）に対してのみ `FindAllDescendants()` を使用。
- 他のコントロールタイプでは従来通り `FindAllChildren()` を維持し、パフォーマンス影響を最小化。

### 実装詳細

1. **`FlaUiElement.Children` の変更**:
   - `_automationElement.ControlType` が `CoreWindow` の場合、`FindAllDescendants()` を使用
   - それ以外は従来通り `FindAllChildren()`
   - `FindAllDescendants` の重複防止のため、`RuntimeId` ベースの重複排除を追加

2. **`SnapshotBuilder` の重複排除**:
   - `_emittedRefs` (`HashSet<string>`) を追加
   - `BuildNode` が `null` を返すよう変更（既に出力済みの ref の場合）
   - 子要素追加時に `null` チェックを追加

### 検証結果

- **電卓 snapshot**: 52 要素を取得（s1e1..s1e52）、数字ボタン 0-9 を含む
- **クリック操作**: `click s1e40`（Button "1"）が正常に動作
- **計算検証**: `1 + 5 = 6` の計算が正しく実行されることを確認
- **パフォーマンス**: Notepad++ で `FindAllDescendants` と `FindAllChildren` の差は 11ms vs 11ms（1.00x）
- **テスト**: 全 529 テストが合格

### コミット

`28b5a48` feat: enable UWP app (Calculator) snapshot and interaction via CoreWindow FindAllDescendants workaround
