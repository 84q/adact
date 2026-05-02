# オプション順番の柔軟化（グローバルオプション化）

## 背景

ADACT CLI では、`--server` オプションを各サブコマンド（`snapshot`, `list-apps`, `click` 等）に個別に定義している。
このため、以下のような使い分けが発生していた：

| コマンド | 結果 |
|---------|------|
| `adact snapshot --server xxx` | ✅ 動作 |
| `adact --server xxx snapshot` | ❌ エラー（`'--server' was not matched`）|

## 目的

`--server` オプションを **グローバルオプション** として定義し、サブコマンドの前後どちらに置いても認識されるようにする。

## 技術選定

### 調査結果

`System.CommandLine`（現行バージョン `2.0.0-beta5.25306.1`）には `Option.Recursive = true` プロパティがあり、`RootCommand` に追加したオプションを全サブコマンドで認識させることができる。

検証済み：
- `Option.Recursive = true` が `2.0.0-beta5.25306.1` でコンパイル・動作することを確認
- 最小テストプロジェクトで `adact --server xxx snapshot` 相当のパースが成功することを確認

### 採用方針

**`RootCommand` に `--server` を `Recursive = true` で定義し、各サブコマンドの個別定義を削除する。**

## 設計

### 変更前

```
RootCommand
├── snapshot [Options: --server, --sid, --filter, ...]
├── list-apps [Options: --server, ...]
├── click [Options: --server, --ref, ...]
├── ... (各コマンドに個別に --server を定義)
├── local (Options: なし)
└── serve (Options: なし)
```

### 変更後

```
RootCommand [Options: --server (Recursive = true)]
├── snapshot [Options: --sid, --filter, ...]
├── list-apps [Options: ...]
├── click [Options: --ref, ...]
├── ... (各コマンドから --server を削除)
├── local (Options: --server ※ヘルプに表示されるが無視)
└── serve (Options: --server ※ヘルプに表示されるが無視)
```

### 変更対象ファイル

| ファイル | 変更内容 |
|---------|---------|
| `src/Adact.Cli/Program.cs` | `RootCommand` に `--server` オプション（`Recursive = true`）を追加 |
| `src/Adact.Cli.Core/Commands/CommandHelpers.cs` | `CreateServerOption()` を `CreateGlobalServerOption()` に変更、または新設。`Recursive = true` を設定 |
| `src/Adact.Cli.Core/Commands/*Command.cs`（約30個） | 各コマンドから `CreateServerOption()` の呼び出しと `AddOption(server)` を削除 |
| `src/Adact.Cli/Commands/LocalCommand.cs` | ヘルプに「`--server` は無視される」注記を追加（Description 等で対応） |
| `src/Adact.Cli/Commands/ServeCommand.cs` | ヘルプに「`--server` は無視される」注記を追加（Description 等で対応） |
| テストファイル | `--server` オプションの定義箇所が変わるため、テストコードを修正 |

### パラメータ取得方法の変更

各コマンドのハンドラ内で `server` オプションの値を取得する方法：

**変更前:**
```csharp
var server = CommandHelpers.CreateServerOption();
cmd.AddOption(server);
cmd.SetHandler(async (string? serverArg, ...) => { ... }, server, ...);
```

**変更後:**
```csharp
// RootCommand 側で定義済み。各コマンドでは個別定義不要。
// ハンドラ内で parseResult から取得、または RootCommand の Option インスタンスを参照
cmd.SetHandler(async (ParseResult parseResult, ...) => {
    var serverArg = parseResult.GetValueForOption(globalServerOption);
    ...
}, ...);
```

ただし、`SetHandler` のシグネチャ変更が各コマンドで必要となるため、実装時に注意。

### LocalCommand / ServeCommand の対応

`Recursive = true` により `--server` は `local` / `serve` のヘルプにも表示される。
以下の対応を行う：

1. **ヘルプ注記**: `local` / `serve` の Description に「`--server` オプションは無視されます」という注記を追加
2. **実装上**: ハンドラ内で `serverArg` が指定されていても無視する（または warning を出す）

## 影響範囲

### 破壊的変更

**なし。** CLI のインターフェースは後方互換性を維持する。
- `adact snapshot --server xxx` → 引き続き動作
- `adact --server xxx snapshot` → 新たに動作（追加機能）

### ヘルプ表示の変化

`adact local --help` / `adact serve --help` の出力に `--server` オプションが追加される。
ただし、Description に「無視される」注記を入れることで、ユーザーの混乱を防ぐ。

## テスト方針

1. **ビルド確認**: 全プロジェクトが正常にビルドすること
2. **単体テスト**: 既存のテストが通ること（Option の定義箇所変更に伴うテストコード修正を含む）
3. **手動確認**:
   - `adact --server xxx snapshot` が正常に動作すること
   - `adact snapshot --server xxx` が引き続き動作すること
   - `adact local --help` / `adact serve --help` に `--server` の注記が含まれること
