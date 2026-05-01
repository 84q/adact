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

`inspect --ref s1e8` の結果:

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

## 4. 今後の検証方針

1. **案 A の検証**: `FlaUiElement.Children` の `FindAllChildren()` を `FindAllDescendants()` に変更し、電卓で snapshot を再取得。他のアプリ（Chrome、Notepad++ 等）でパフォーマンス影響を確認。
2. **案 B の検証**: `RawViewWalker` / `ControlViewWalker` を使って `ApplicationFrameInputSinkWindow` の内部要素を取得できるか確認。
3. **判断**: 案 A/B の検証結果をもとに、どの案を採用するか決定。いずれにせよ案 C（ドキュメント化）は併行して実施。

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
