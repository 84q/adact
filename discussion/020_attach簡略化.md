# 020 attach 簡略化 (windowRef 専用化)

## 1. 要件

### 1.1 動機

`windows_attach` ツールおよび `adact attach` コマンドが、現在 2 つの入力経路 (windowRef / プロセス名等のクエリ) を受け付けており、結果として:

- `WindowsTools.AttachAsync` の本体が複雑 (経路分岐 + 経路ごとの idempotent 判定)
- `AttachQuery` / `AmbiguousAttachException` / `UiaEngine.FindMatchesAsync` 等の支援機構が必要
- ウィンドウ列挙 (発見) とアタッチ (選択) という 2 つの責務がツール内に同居

これを「`list-apps` = window 集合の発見」「`attach` = その中の 1 つを windowRef で選択」と分離し、設計を単純化する。

### 1.2 スコープ

- server (`Adact.Mcp.Common.WindowsTools.AttachAsync`)
- CLI (`Adact.Cli.Commands.AttachCommand`)
- Engine (`AttachQuery`, `UiaEngine` の attach 系 API, `AmbiguousAttachException`)
- 関連ユニット/結合/E2E テスト
- ドキュメント (`docs/`) および Skill (`.github/skills/adact-cli/`)

### 1.3 非目標

- `list-apps` の挙動・出力形式の変更
- windowRef の採番規則 (`w<n>`) や `WindowRefStore` の振る舞い変更
- `WindowKey` の構造変更 (引き続き list-apps が使用)

### 1.4 下位互換

公開前のプロジェクトのため、破壊的変更として進める。deprecated 経過期間は設けない。

## 2. 新仕様

### 2.1 MCP ツール `windows_attach`

| 観点 | 新仕様 |
| --- | --- |
| パラメータ | `windowRef: string` (required) |
| 削除パラメータ | `processName`, `windowTitle`, `className`, `processId` |
| 戻り値 | 変更なし: `sessionId`, `windowRef`, `windowInfo` |
| エラーコード | `INVALID_ARGUMENT` (形式不正), `INVALID_WINDOW_REF` (未知/引退済み), `WINDOW_NOT_FOUND` (HWND attach 失敗時に残す) |
| 削除エラーコード | `AMBIGUOUS_ATTACH` (発生し得なくなる) |

### 2.2 CLI `adact attach`

| 観点 | 新仕様 |
| --- | --- |
| 位置引数 | `<ref>` (必須、`w<n>` 形式) |
| 維持オプション | `--no-snapshot`, `--snapshot-dir`, `--server` |
| 削除オプション | `--process-name`, `--title`, `--process-id`, `--class-name` |
| バリデーション | ref が `w<n>` 形式に一致するか確認のみ |

### 2.3 内部 API

| API | 措置 |
| --- | --- |
| `Adact.Engine.AttachQuery` (record) | 削除 |
| `Adact.Engine.UiaEngine.AttachAsync(AttachQuery)` | 削除 |
| `Adact.Engine.UiaEngine.FindMatchesAsync(AttachQuery)` | 削除 |
| `Adact.Engine.UiaEngine.AttachByHandleAsync(nint)` | 維持 (新仕様の主経路) |
| `Adact.Engine.Exceptions.AmbiguousAttachException` | 削除 |
| `Adact.Engine.Exceptions.WindowNotFoundException` | 維持 (HWND ベースの attach 失敗で利用) |
| `Adact.Mcp.Common.WindowKey` | 維持 (list-apps が使用) |
| `Adact.Mcp.Common.WindowRefStore.SyncOrAssign` / `TryFindByKey` / `TryResolve` / `AssociateSession` | 維持 |

## 3. AttachAsync の新フロー

```
AttachAsync(windowRef, ct):
    取得: SessionStore.AcquireAsync (UIA 直列化)
    検証: windowRef が "w<n>" 形式か → 違反は INVALID_ARGUMENT
    解決: WindowRefStore.TryResolve(windowRef) → 失敗は INVALID_WINDOW_REF
    冪等: entry.SessionId が生きていれば既存 session を返す
    新規: UiaEngine.AttachByHandleAsync(entry.Key.Hwnd)
          → SessionStore.Register(session)
          → WindowRefStore.AssociateSession(windowRef, $"s{session.SessionId}")
    結果: { sessionId, windowRef, windowInfo } を CallToolResult で返す
    例外: ToolErrors.TryMap → 該当なしは LogError + 再 throw
```

クエリ経路 (`processName` 等) およびそれに伴う `WindowKey.From` / `TryFindByKey` 経由の冪等判定はすべて削除。

## 4. テスト変更計画

### 4.1 削除

- `tests/Adact.Engine.Tests/Unit/AttachQueryMatchesTests.cs`
- `tests/Adact.Engine.Tests/Integration/AttachQueryClassNameDisambiguationTests.cs`

### 4.2 書き換え (list-apps → windowRef 経由 attach に変更)

- `tests/Adact.Engine.Tests/Smoke/NotepadppSmokeTests.cs`
- `tests/Adact.Engine.Tests/IntegrationUia/CalculatorSnapshotTests.cs`
- `tests/Adact.Mcp.Stdio.Tests/WindowsToolsE2ETests.cs`
- `tests/Adact.Mcp.Http.Tests/E2E/CalculatorHttpE2ETests.cs`

### 4.3 既存テストでカバーが失われる観点

- query 経路の正常系: 削除されるため不要
- `AMBIGUOUS_ATTACH` の発火経路: 削除されるため不要
- `WindowNotFoundException` の query 文脈での発生: 削除されるため不要

## 5. docs / skill 更新計画

実装中に最新状態を確認して反映する対象:

- `docs/architecture/command-flows.md` の attach フロー記述
- `docs/api/` 配下の attach 関連リファレンス (要確認)
- `.github/skills/adact-cli/SKILL.md`、`references/attach.md` 等 (要確認)

## 6. 実装順序

1. Engine 層: `AttachQuery` / `AmbiguousAttachException` / `FindMatchesAsync` / `AttachAsync(AttachQuery)` 削除 + 該当ユニットテスト削除 → ビルド確認
2. MCP 層: `WindowsTools.AttachAsync` を windowRef-only に簡素化 → ユニットテスト確認
3. CLI 層: `AttachCommand` のオプションとバリデーション削除 → ユニットテスト確認
4. E2E / Smoke / Integration テスト書き換え → 該当テスト実行
5. docs / skill 更新 → リンク・整合性確認
6. `dotnet format adact.sln` で整形

各ステップ後に `dotnet build adact.sln` を走らせ、warning/error 0 を維持する。

## 7. 確定済み判断 (本 discussion で決定)

- 設計選択肢: A (server・CLI とも windowRef 専用化、CLI 内部での list-apps 合成は行わない)
- 下位互換: なし、破壊的変更
- AttachQuery 関連 API: 全削除
- CLI シグネチャ: `adact attach <ref>` (positional 必須) + `--no-snapshot` / `--snapshot-dir` / `--server`
- attach 成功時の自動 snapshot: 維持
- MCP `windows_attach` の `windowRef`: required (デフォルト null を外す)
