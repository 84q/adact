# Named Pipe / HTTP 統合設計

## 1. 背景・目的

### 現状の課題
- `adact local` (stdio MCP server) は MCP client からの直接接続用だが、現在使用頻度が低い
- HTTP daemon (`adact serve`) はリモート接続に便利だが、ローカル専用の高速通信手段がない
- Windows では Named Pipe を使ったローカル専用通信が可能で、TCP より高速・セキュア

### 目的
- stdio (`adact local`) を廃止し、Named Pipe / HTTP の二本柱に整理する
- デフォルトを Named Pipe にし、ローカル使用時は高速な通信を実現
- リモート使用時は HTTP を明示的に選択可能にする

## 2. Named Pipe 仕様

### 2.1 パイプ名形式

```
\\.\pipe\adact-{workspaceHash}-default
```

- **workspaceHash**: `.adact` ディレクトリまたはカレントディレクトリの SHA1 ハッシュ（16文字）
- **default**: セッション名（現状は単一セッションのみサポート、将来拡張用）

### 2.2 通信プロトコル

- **トランスポート**: Named Pipe (Windows)
- **プロトコル**: MCP (Model Context Protocol) over JSON-RPC
- **メッセージ形式**: JSON Lines (`\n` 区切り)
- **エンコーディング**: UTF-8

### 2.3 セキュリティ

- Named Pipe はローカルマシン内でのみアクセス可能
- 外部からのネットワークアクセスは不可能
- 同一ユーザー・同一ワークスペースでのみ接続可能

## 3. `adact serve` サブコマンド

### 3.1 コマンド体系

```bash
# HTTP モード（リモート接続可能）
adact serve http [--host <ip>] [--port <n>]
  --host: バインドする IP アドレス（デフォルト: 127.0.0.1）
  --port: ポート番号（デフォルト: 41300）

# Named Pipe モード（ローカル専用）
adact serve pipe
  パイプ名は自動生成（ワークスペースハッシュを使用）
```

### 3.2 動作詳細

| モード | トランスポート | リモート接続 | デフォルト接続先 |
|-------|--------------|------------|----------------|
| http | TCP (HTTP) | 可能 | `--server` で明示 |
| pipe | Named Pipe | 不可 | デフォルト |

## 4. 接続先解決

### 4.1 CLI クライアントの接続フロー

```
1. --server オプションが指定されている場合
   → HTTP モードで接続

2. --server オプションがない場合
   → Named Pipe に接続を試行
   → 接続できなければエラー（CONNECTION_FAILED）
```

### 4.2 接続先優先順位

| 優先度 | 入力 | 動作 |
|-------|------|------|
| 1 | `--server <url>` | HTTP モードで接続 |
| 2 | （なし） | Named Pipe に接続 |

### 4.3 Named Pipe 接続失敗時のエラー

```
error CONNECTION_FAILED
message No ADACT server is running. Run 'adact serve pipe|http' first, or use commands that auto-start (list-apps, launch).
hint Run 'adact serve pipe' to start the server with named pipe transport (local), or 'adact serve http' for remote access.
```

## 5. 自動起動仕様

### 5.1 自動起動対象コマンド

| コマンド | 未起動時の動作 |
|---------|--------------|
| `list-apps` | **自動起動**（`adact serve pipe` を spawn） |
| `launch` | **自動起動**（`adact serve pipe` を spawn） |
| `install` | 無関係（server 不要） |
| `serve` | サーバー起動（接続不要） |
| `daemon-stop` | エラー（停止対象が必要） |
| その他全て | **エラー**（CONNECTION_FAILED） |

### 5.2 自動起動の検知方法

1. Named Pipe に接続を試行
2. 接続失敗 → `adact serve pipe` を spawn（detached モード）
3. stdout から起動完了メッセージを待機
4. 再接続を試行（リトライ上限あり）

### 5.3 起動完了メッセージ

```
### Success
Daemon listening on \\.\pipe\adact-{workspaceHash}-default
<EOF>
```

## 6. `daemon-stop` 仕様

### 6.1 停止可能なモード

| モード | 停止可否 | 方法 |
|-------|---------|------|
| HTTP | **不可** | 廃止（セキュリティ上の理由） |
| Named Pipe | **可** | `adact daemon-stop` で同一ワークスペースのみ停止 |

### 6.2 停止時の挙動

- Named Pipe 経由で `daemon_stop` ツールを呼び出し
- 同一ワークスペース（同じパイプ名）のみ停止可能
- 他のワークスペースのサーバーは停止不可

### 6.3 HTTP モードでの daemon-stop

```
error LOCAL_ONLY
message daemon-stop is not supported for HTTP mode. Use Ctrl+C to stop the server.
hint For HTTP server, stop the process manually or use task management tools.
```

## 7. 廃止・変更機能

### 7.1 廃止される機能

| 機能 | 理由 | 代替手段 |
|-----|------|---------|
| `adact local` | stdio モード廃止 | `adact serve pipe` |

### 7.2 変更される機能

| 機能 | 変更前 | 変更後 |
|-----|-------|-------|
| `adact serve` | HTTP のみ | `adact serve http` / `adact serve pipe` のサブコマンド化 |
| デフォルト接続先 | HTTP (`127.0.0.1:41300`) | Named Pipe |
| `daemon-stop` | HTTP localhost 専用 | Named Pipe 専用、HTTP は停止不可 |

## 8. 実装タスク

### Phase 1: Named Pipe 基盤実装

1. **Named Pipe Server 実装**
   - `Adact.Cli.Server.NamedPipe` 名前空間を新規作成
   - `NamedPipeHost` クラス（`HttpHost` と同等の機能）
   - パイプ名生成ロジック（ワークスペースハッシュ計算）

2. **Named Pipe Client 実装**
   - `Adact.Cli.Connection.NamedPipeClient` クラス
   - MCP プロトコル over Named Pipe

### Phase 2: コマンド変更

3. **`adact serve` サブコマンド化**
   - `ServeCommand` を `ServeHttpCommand` / `ServePipeCommand` に分割
   - `Program.cs` のサブコマンド登録更新

4. **`adact local` 削除**
   - `LocalCommand.cs` 削除
   - `Program.cs` からの登録削除
   - 関連テスト削除

### Phase 3: 接続ロジック変更

5. **接続先解決の変更**
   - `ConnectionResolver.Resolve()` を修正
   - デフォルトを Named Pipe に変更
   - `--server` 指定時のみ HTTP モード

6. **自動起動実装**
   - `list-apps`, `launch` コマンドに自動起動ロジックを追加
   - `Adact.Cli.DaemonSpawner` クラスを新規作成

### Phase 4: daemon-stop 変更

7. **`daemon-stop` 変更**
   - HTTP モードではエラーを返す
   - Named Pipe モードでのみ停止処理を実行
   - 同一ワークスペース判定を追加

### Phase 5: ドキュメント・テスト

8. **ドキュメント更新**
   - `docs/spec/cli.md` を更新
   - `docs/architecture/runtime-modes.md` を更新
   - Skill ファイルを更新

9. **テスト追加・更新**
   - Named Pipe 接続の Unit/Integration テスト
   - 自動起動の E2E テスト
   - `local` コマンド削除に伴うテスト削除

## 9. 影響範囲

### 9.1 影響を受けるファイル

| カテゴリ | ファイル |
|---------|---------|
| Server | `src/Adact.Cli.Server/HttpHost.cs` |
| Connection | `src/Adact.Cli.Core/Connection/AdactMcpClient.cs`, `ConnectionResolver.cs` |
| Commands | `src/Adact.Cli/Commands/ServeCommand.cs`, `LocalCommand.cs` (削除), `DaemonStopCommand.cs` |
| Program | `src/Adact.Cli/Program.cs` |
| Tests | `tests/Adact.Cli.Tests/`, `tests/Adact.Mcp.Stdio.Tests/` (削除または変更) |

### 9.2 互換性

- **Breaking Change**: `adact local` が使用不可になる
- **Breaking Change**: デフォルト接続先が HTTP から Named Pipe に変更
- **Migration**: `adact serve pipe` を使用するように変更が必要

## 10. 将来拡張

### 10.1 複数セッション
現状は `default` セッション名固定。将来 `-s=<name>` で複数セッションをサポート可能。

### 10.2 Linux/macOS 対応
現状は Windows のみ。将来 Unix Domain Socket で Linux/macOS に展開可能。

## 関連ドキュメント

- `docs/spec/cli.md` - CLI 仕様
- `docs/architecture/runtime-modes.md` - ランタイムモード
- `docs/architecture/command-flows.md` - コマンドフロー
- `019_Phase8以降の残タスク整理.md` - 本設計に基づくタスク管理

## 設計決定事項

| 項目 | 決定 |
|-----|------|
| デフォルト接続先 | Named Pipe |
| stdio (`local`) | 廃止 |
| HTTP モード | 維持（リモート用） |
| 自動起動対象 | `list-apps`, `launch` のみ |
| daemon-stop | Named Pipe のみ対応、HTTP は不可 |
| ワークスペース特定 | `.adact` またはカレントディレクトリ |
