# CLI / MCP 命名統一

## 背景

- CLI サブコマンドと MCP ツール名が乖離しており、統一したい
- MCP 側を CLI に寄せる方向で整理する

## 方針

| 項目 | 決定 |
|---|---|
| CLI 命名規則 | kebab-case（現行維持、変更なし） |
| MCP 命名規則 | `adact_` prefix + snake_case |
| 変換ルール | CLI の kebab-case → snake_case に変換し `adact_` を前置 |
| `daemon_stop` | MCP ツールから廃止（サーバー管理は CLI のみ） |
| 破壊的変更 | 許容（内部利用フェーズのため） |

## 命名規則の根拠

### CLI: kebab-case

- POSIX/GNU 系 CLI、dotnet CLI の業界標準
- Playwright CLI も同じ方針

### MCP: `adact_` prefix + snake_case

- MCP ツール名は snake_case が事実上の慣習（Playwright MCP: `browser_click` 等）
- prefix は他 MCP サーバーとの衝突回避に実用的（MCP Client は複数サーバー併用が一般的）
- `adact_` はプロダクト名で明確

### CLI 側の複合語（区切りなし）

`mousemove`, `mousedown`, `mouseup`, `mousewheel`, `keypress`, `keydown`, `keyup`, `doubleclick` は区切りなし。Playwright CLI も同じ方針（`mousemove`, `mousedown`, `dblclick` 等）を採用しており、業界慣習に沿っている。

## 対応表

| # | CLI | 現行 MCP | 新 MCP |
|---|---|---|---|
| 1 | `list-windows` | `windows_list_apps` | `adact_list_windows` |
| 2 | `attach` | `windows_attach` | `adact_attach` |
| 3 | `snapshot` | `windows_snapshot` | `adact_snapshot` |
| 4 | `click` | `windows_click` | `adact_click` |
| 5 | `fill` | `windows_fill` | `adact_fill` |
| 6 | `doubleclick` | `windows_dblclick` | `adact_doubleclick` |
| 7 | `hover` | `windows_hover` | `adact_hover` |
| 8 | `mousemove` | `windows_mouse_move` | `adact_mousemove` |
| 9 | `mousedown` | `windows_mouse_down` | `adact_mousedown` |
| 10 | `mouseup` | `windows_mouse_up` | `adact_mouseup` |
| 11 | `mousewheel` | `windows_mouse_wheel` | `adact_mousewheel` |
| 12 | `keypress` | `windows_press` | `adact_keypress` |
| 13 | `keydown` | `windows_key_down` | `adact_keydown` |
| 14 | `keyup` | `windows_key_up` | `adact_keyup` |
| 15 | `type` | `windows_type` | `adact_type` |
| 16 | `check` | `windows_check` | `adact_check` |
| 17 | `uncheck` | `windows_uncheck` | `adact_uncheck` |
| 18 | `select` | `windows_select` | `adact_select` |
| 19 | `focus` | `windows_focus` | `adact_focus` |
| 20 | `clear` | `windows_clear` | `adact_clear` |
| 21 | `scroll` | `windows_scroll_into_view` | `adact_scroll` |
| 22 | `resize-window` | `windows_resize` | `adact_resize_window` |
| 23 | `minimize-window` | `windows_minimize` | `adact_minimize_window` |
| 24 | `maximize-window` | `windows_maximize` | `adact_maximize_window` |
| 25 | `restore-window` | `windows_restore` | `adact_restore_window` |
| 26 | `inspect` | `windows_inspect` | `adact_inspect` |
| 27 | `screenshot` | `windows_screenshot` | `adact_screenshot` |
| 28 | `wait-for-element` | `windows_wait_for` | `adact_wait_for_element` |
| 29 | `wait-for-window` | `windows_wait_for_window` | `adact_wait_for_window` |
| 30 | `detach` | `windows_detach` | `adact_detach` |
| 31 | `close-window` | `windows_close` | `adact_close_window` |
| 32 | `kill` | `windows_kill` | `adact_kill` |
| 33 | `close-all` | `windows_close_all` | `adact_close_all` |
| 34 | `launch` | `windows_launch` | `adact_launch` |
| 35 | — | `daemon_stop` | **廃止** |

## CLI 専用コマンド（MCP 対応なし）

| CLI | 用途 |
|---|---|
| `serve` / `serve http` / `serve pipe` | daemon 起動（サーバー側のみ） |
| `daemon-stop` | daemon 停止（サーバー側のみ） |
| `install` | Skill ファイルのインストール |

## 参考: Playwright の命名

| Playwright CLI | Playwright MCP |
|---|---|
| `click` | `browser_click` |
| `type` | `browser_type` |
| `hover` | `browser_hover` |
| `press` | `browser_press_key` |
| `snapshot` | `browser_snapshot` |
| `mousemove` | (vision mode のみ) |
| `dblclick` | (MCP には未提供) |

---

## 実装設計

### 変更対象

| # | 対象 | 箇所数 | 作業内容 |
|---|---|---|---|
| 1 | MCP ツール属性 Name 値 | 35 | `[McpServerTool(Name = "...")]` の文字列置換 |
| 2 | CLI `DaemonStopCommand.cs` | 1 | `CallToolAsync("daemon_stop")` → `"adact_daemon_stop"` |
| 3 | テスト内文字列リテラル | ~50 | ツール名参照の更新 |
| 4 | `docs/` 内ドキュメント | 多数 | ツール名言及の更新 |
| 5 | `.agents/skills/` | 4 | ツール名言及の更新 |

### 実装方針

- すべて文字列リネーム（ロジック変更なし）
- 変換ルール: CLI の kebab-case → snake_case に変換し `adact_` を前置
- `daemon_stop` → `adact_daemon_stop`（リネームのみ、削除しない）

### 検証

- `dotnet build adact.sln` で警告ゼロ確認
- `dotnet test --filter "Layer=Unit|Layer=Integration"` で回帰確認
- 旧ツール名（`windows_*`, `daemon_stop`）がソース内に残っていないことをテキスト検索で確認
