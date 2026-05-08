# E2: select 複数選択拡張・mouse-wheel バリデーション

## 背景と目的

### select 複数選択

- AI エージェントが MultiSelect 対応の ListBox 等で複数アイテムを選択するシナリオがある
- 現行の `adact_select` / `adact select` は単一アイテムのみ選択可能（`SelectionItemPattern.Select()` のみ）
- 既存選択への追加・削除も必要

### mouse-wheel バリデーション

- `deltaX=0, deltaY=0` で呼び出すとサイレント no-op で成功を返す
- AI エージェントやユーザーの指定ミスを見落とす可能性がある
- CLI のサブコマンドオプションパース時点で早期検知したい

## 概念の確定事項

### select

#### 操作モデル: 3つのモード

| モード | UIA 操作 | 説明 |
|--------|----------|------|
| デフォルト（フラグなし） | 最初のアイテム: `Select()` → 2番目以降: `AddToSelection()` | 指定アイテムのみを選択状態にする（他は解除） |
| `--add` | 全アイテム: `AddToSelection()` | 既存の選択を維持したまま追加 |
| `--remove` | 全アイテム: `RemoveFromSelection()` | 既存の選択から除外 |

#### パラメータの複数指定

- `name` / `index` / `itemRef` をそれぞれ配列として複数指定可能にする
- **排他制約は維持**: 同一種類のパラメータのみ指定可能（混在不可）
  - `--name "A" --name "B"` → OK
  - `--name "A" --index 2` → エラー

#### 事前チェック

- `--add` / `--remove` モードの場合、対象コントロールの `SelectionPattern.CanSelectMultiple` を事前チェック
- 非対応コントロールでは明確なエラーメッセージを返す（UIA 例外をそのまま伝播しない）

### mouse-wheel

#### バリデーション

- `deltaX == 0 && deltaY == 0` の場合、バリデーションエラーを返す
- エラーは CLI サブコマンドのオプションパース時点で発生させる（MCP 到達前に検知）
- MCP ツール側にも同一のバリデーションを設置（CLI 以外の呼び出し元への対応）

---

## 設計

### 新規型

#### SelectionMode (enum)

`Replace` / `Add` / `Remove` の 3 値。デフォルトは `Replace`。

#### SelectionTarget (判別型)

識別方法（Name / Index / ItemRef）を内包する型。ファクトリメソッドで生成。

### API シグネチャ変更

#### Engine (`IWindowSession`)

現行:
```
SelectAsync(refId, name?, index?, itemRef?, ct)
```

変更後:
```
SelectAsync(refId, SelectionTarget[], SelectionMode, ct)
```

#### MCP ツール (`adact_select`)

パラメータ:
- `ref`: string（必須、変更なし）
- `name`: string[]?（配列化、**破壊的変更**）
- `index`: int[]?（配列化）
- `itemRef`: string[]?（配列化）
- `add`: bool = false（新規）
- `remove`: bool = false（新規）

#### CLI (`adact select`)

オプション:
- `--name` 複数指定可能
- `--index` 複数指定可能
- `--item-ref` 複数指定可能
- `--add` フラグ（新規）
- `--remove` フラグ（新規）

### バリデーション配置

| チェック | CLI | MCP | Engine |
|----------|-----|-----|--------|
| `name`/`index`/`itemRef` の排他制約（同一種類のみ） | ✅ | ✅ | ✅ |
| `--add` と `--remove` の同時指定禁止 | ✅ | ✅ | ✅ |
| `CanSelectMultiple` 事前チェック（Add/Remove モード時） | — | — | ✅ |
| 複数アイテム + Replace モードの動作 | — | — | ✅（最初 Select → 以降 AddToSelection） |

### mouse-wheel バリデーション

| チェック | CLI | MCP |
|----------|-----|-----|
| `deltaX == 0 && deltaY == 0` の拒否 | ✅ | ✅ |

エラーコード: 既存の `INVALID_ARGUMENT` を使用。

### テスト計画

#### select

- 単一アイテム Replace モード（現行と同じ動作の回帰テスト）
- 複数アイテム Replace モード（最初 Select → 以降 AddToSelection の検証）
- Add モード（AddToSelection の呼び出し検証）
- Remove モード（RemoveFromSelection の呼び出し検証）
- Add/Remove で CanSelectMultiple=false の場合のエラー
- パラメータ排他制約のバリデーション（混在→エラー）
- `--add` + `--remove` 同時指定→エラー
- 空配列（アイテム未指定）→エラー

#### mouse-wheel

- `deltaX=0, deltaY=0` でエラー（CLI・MCP 両方）
- 正常ケースは変更なし（既存テスト維持）
