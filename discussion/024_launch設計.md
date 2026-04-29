# 024 — `launch` コマンド設計

要件: [023_launch要件定義.md](023_launch要件定義.md)

## 1. 全体像

`launch` は UIA セッションに紐づかない、純粋なプロセス起動コマンド。Win32 / UWP の両系統を 1 コマンドで扱う。起動成功時は PID と解決済み実行ファイル情報のみ返し、attach は行わない。

## 2. UWP 判定と起動経路

| 経路 | 判定条件 | 起動手段 | PID 取得 |
| --- | --- | --- | --- |
| Win32 | 既定 | `Process.Start(ProcessStartInfo)` (`UseShellExecute=false`) | `Process.Id` |
| UWP | 入力文字列が `shell:AppsFolder\` で始まる | `IApplicationActivationManager.ActivateApplication(aumid, args, AO_NOERRORUI, out pid)` | COM 戻り値 `pid` |

UWP モードでは入力文字列から `shell:AppsFolder\` プレフィックスを除去した残りを **AUMID** として ActivateApplication に渡す。

## 3. Engine 層配置

- 既存 `src/Adact.Engine/UiaEngine.cs` (sealed partial 化済) に **partial で追加**:
  - `src/Adact.Engine/UiaEngine.Launch.cs` (新規)
- 公開 API:
  ```csharp
  Task<LaunchResult> LaunchAsync(LaunchRequest request, CancellationToken ct);
  ```
- 補助型:
  - `LaunchRequest`: 実行ファイルパス, 引数 (`IReadOnlyList<string>`), cwd?, env? (`IReadOnlyDictionary<string,string>`)
  - `LaunchResult`: `int Pid`, `string ProcessName`, `string? ExecutablePath` (UWP は AUMID または null)
- UWP の COM 呼び出し用 P/Invoke / ComImport 宣言は `src/Adact.Engine/NativeMethods.cs` に集約

### エラー

- ファイル不在 / `Win32Exception` / COM 失敗 → `Adact.Engine.Exceptions.LaunchFailedException` (新規) を throw
- 引数矛盾 (UWP + cwd/env) → `ArgumentException` (上位で `INVALID_ARGUMENT` にマップ)

## 4. MCP 層配置

- 新規 `src/Adact.Mcp.Common/WindowsTools.Launch.cs` (partial)
- ツール: `windows_launch`
- 引数:
  - `executable: string` (必須)
  - `args: string[]?` (任意)
  - `cwd: string?` (任意)
  - `env: Dictionary<string,string>?` (任意)
- バリデーション:
  - UWP モード判定後、`cwd` または `env` が指定されていれば `INVALID_ARGUMENT` ("unsupported with UWP launch")
- 戻り値: JSON content (`{ pid, processName, executablePath }`)
- エラーマッピング:
  - `LaunchFailedException` → `LAUNCH_FAILED`
  - `ArgumentException` → `INVALID_ARGUMENT`

`ToolErrors` に以下を追加:

```csharp
public const string LaunchFailed = "LAUNCH_FAILED";
```

`ToolErrors.TryMap` に `LaunchFailedException` のマッピングを追加。

## 5. CLI 層配置

- 新規 `src/Adact.Cli/Commands/LaunchCommand.cs`
- 構文: `adact launch <executable> [--cwd <dir>] [--env KEY=VALUE]... [-- <arg>...]`
- System.CommandLine で `--` 以降を raw arguments として受け取り、`ArgumentList` に渡す
- `--env` は `Action.AllowMultipleArgumentsPerToken = false` で繰り返し指定をリスト化、`KEY=VALUE` パース時は最初の `=` で分割
- `--server <url>` 既存共通オプションを踏襲
- 出力: 成功時に `{ pid, processName, executablePath }` を JSON 1 行で stdout 出力 (既存コマンドの出力規約に合わせる)

### 引数のクォーティング

- `Process.Start` には `ProcessStartInfo.ArgumentList` を使い、自動エスケープに任せる (Argument 文字列の手動結合はしない)。

## 6. 環境変数マージ規則

- `ProcessStartInfo.Environment` は呼び出し元プロセスの環境を **継承** している。
- `--env KEY=VALUE` は `Environment[KEY] = VALUE` として上書き/追加する。
- 削除セマンティクス (KEY を空にする等) は本リリースでは扱わない。

## 7. テスト計画

### Engine Unit (`tests/Adact.Engine.Tests/Unit/UiaEngineLaunchTests.cs`)

- 不在 exe (`X:\\nonexistent.exe`) → `LaunchFailedException`
- `cmd.exe` を `/c exit 0` で起動 → PID > 0、即終了するが PID 取得は成功
- cwd 指定 → 子プロセスの `Process.StartInfo.WorkingDirectory` 検証
- 環境変数指定 → スモーク (実プロセス起動して読み戻すか、StartInfo の検証のみ)
- UWP モード + cwd/env → `ArgumentException`

> 実プロセス起動を伴うテストでは Implementation 側で必ず PID を kill / WaitForExit してリソースリークを防ぐ。

### MCP Unit (`tests/Adact.Mcp.Common.Tests/Unit/WindowsToolsLaunchTests.cs`)

- UWP + cwd/env → `INVALID_ARGUMENT`
- `LaunchFailedException` のマッピング → `LAUNCH_FAILED`
- 成功パスのレスポンス整形 (Engine をモック)

### CLI Unit (`tests/Adact.Cli.Tests/Unit/LaunchCommandTests.cs`)

- `--env KEY=VALUE` パース (繰り返し指定、値側の `=` 含む)
- `-- arg1 arg2` の raw 引数取り込み
- `--cwd` パス透過
- 不正な `--env KEY` (`=` なし) → `UserError`

### E2E

不要 (実 UIA 不要、`Process.Start` で完結)。

## 8. Skill 同期

- `src/Adact.Cli/Skills/adact-cli/references/launch.md` を新規作成 (英語、既存スタイル: Synopsis / Arguments / Output / Examples / Error recovery)
- `src/Adact.Cli/Skills/adact-cli/SKILL.md` のコマンド表に `launch` を追記
- `tests/Adact.Cli.Tests/Unit/InstallCommandTests.cs` の `ExpectedDocumentedCommands` に追加
- `tests/Adact.Cli.Tests/Integration/InstallCommandIntegrationTests.cs` の `ExpectedFiles` に追加

## 9. auto-snapshot

- 対象外 (要件 §2 のとおり)。CLI に `--no-snapshot` 等は不要。

## 10. 実装順序

1. Engine 層 (`UiaEngine.Launch.cs` + `LaunchRequest`/`LaunchResult` + `LaunchFailedException` + `NativeMethods` の COM 宣言)
2. MCP 層 (`WindowsTools.Launch.cs` + `ToolErrors.LaunchFailed` + `TryMap` 追加)
3. CLI 層 (`LaunchCommand.cs` + `Program.cs` 登録)
4. Skill 同期
5. テスト追加

1 PR 内で一括コミット (Phase 8 Step 6 の前に割り込み Step として扱う = "Step 5.5")。

## 11. 残オープン項目

- UWP の `executablePath` 戻り値: AUMID をそのまま返すか null にするか → 実装時に COM の戻りで判断 (本リリースは AUMID を返す方向)
- `ApplicationActivationManager` の COM CLSID / IID は実装時に確定 (`{45BA127D-10A8-46EA-8AB7-56EA9078943C}` / `{2E941141-7F97-4756-BA1D-9DECDE894A3D}` を使用予定)
