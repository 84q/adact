# 049 kill graceful shutdown

## 概念

### 背景・目的

現行 `kill` は `Process.Kill(entireProcessTree: true)` で即時強制終了する。
アプリがファイル保存等の後処理を行う余地がない。

### 方針

`kill` を graceful-first に変更する（破壊的変更）。

1. 対象プロセスの全トップレベルウィンドウに WM_CLOSE を送信
2. デフォルト 5000ms 待機
3. 生存していれば Process.Kill() でフォールバック
4. auto-detach

### オプション

- `--force`: WM_CLOSE をスキップし即座に Process.Kill（旧動作）
- `--timeout <ms>`: 待機時間をミリ秒で指定（デフォルト 5000）

### close-window との棲み分け

| コマンド | 対象 | 挙動 | 失敗時 |
|---|---|---|---|
| close-window | セッションウィンドウ 1 つ | WM_CLOSE のみ | CLOSE_FAILED |
| kill | プロセス全ウィンドウ | WM_CLOSE → 待機 → 強制終了 | 必ず終了する |

### 完了メッセージ

- graceful 終了: `killed (graceful)` / method: `graceful`
- タイムアウト後強制: `killed (forced after timeout)` / method: `forced_after_timeout`
- --force 使用: `killed (forced)` / method: `forced`

---

## 設計

### Engine 層

**新規型: `KillMethod` 列挙**

```
enum KillMethod { Graceful, Forced, ForcedAfterTimeout }
```

**IWindowSession 変更:**

```
- Task KillAsync(CancellationToken ct = default);
+ Task<KillMethod> KillAsync(bool force = false, int timeoutMs = 5000, CancellationToken ct = default);
```

**WindowSession.KillAsync 実装フロー:**

- `force = true`:
  1. PID 同一性検証（既存）
  2. `Process.Kill(entireProcessTree: true)`
  3. return `Forced`

- `force = false` (デフォルト):
  1. PID 同一性検証（既存）
  2. `EnumWindows` + `GetWindowThreadProcessId` で対象プロセスのトップレベルウィンドウ HWND を収集
  3. 各ウィンドウに `PostMessage(hwnd, WM_CLOSE, 0, 0)` 送信
  4. `process.WaitForExitAsync()` をタイムアウト付きで待機
     - `CancellationTokenSource.CreateLinkedTokenSource(ct)` + `CancelAfter(timeoutMs)`
  5. 期間内に終了 → return `Graceful`
  6. タイムアウト → `Process.Kill(entireProcessTree: true)` → return `ForcedAfterTimeout`

**P/Invoke:** `NativeMethods.cs` に `EnumWindows`, `PostMessage`, `GetWindowThreadProcessId`, `WM_CLOSE` すべて宣言済み。追加不要。

### MCP 層 (`WindowsTools.cs`)

**パラメータ追加:**

| パラメータ | 型 | デフォルト | 説明 |
|---|---|---|---|
| force | bool | false | true 時は即時強制終了 |
| timeoutMs | int? | null (= 5000) | graceful 待機時間（ミリ秒） |

**レスポンス JSON:**

```json
{ "sessionId": "s1", "killed": true, "detached": true, "method": "graceful" }
```

### CLI 層 (`KillCommand.cs`)

**オプション追加:**

| オプション | 型 | 説明 |
|---|---|---|
| `--force` | bool flag | WM_CLOSE スキップ、即死 |
| `--timeout` | int? | 待機時間 ms（デフォルト 5000） |

**出力:**

```
killed true
detached true
method graceful
```

### テスト

- Engine Unit テスト: `force=true` で従来通り kill されること（既存テストを更新）
- Engine Unit テスト: `force=false` のフロー検証は困難（EnumWindows/PostMessage はネイティブ）→ Smoke テストで検証
- MCP Unit テスト: force/timeoutMs パラメータが IWindowSession.KillAsync に渡されること
