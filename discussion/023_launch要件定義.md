# 023 — `launch` コマンド要件定義

Phase 8 の追加コマンドとして `launch` (アプリケーション起動) を導入する。本書は要件のみを記録し、技術選定・実装方法は設計書に分離する。

## 1. 目的・ユースケース

| ユースケース | 説明 |
| --- | --- |
| AI エージェントの "ステップゼロ" | エージェントが操作対象アプリを自分で起動し、そのまま操作フローに入る |
| 起動 + 後続操作のワンライナー化 | 既存の「起動 → `windows_list-apps` で探す → `windows_attach`」を 1 コマンド呼び出しで完結させる (※起動と attach は分離。下記参照) |
| クリーン状態からの再現 | テスト・デモ用途で、毎回未起動状態からスタートさせる |

## 2. スコープ

### 含む

- 通常の Win32 / .NET 実行ファイルの起動 (例: `notepad.exe`, `C:\\Tools\\foo.exe`)
- フルパス指定または PATH 探索
- コマンドライン引数の指定
- 作業ディレクトリ (cwd) の指定
- 環境変数の追加・上書き (`KEY=VALUE` 複数)
- UWP / Microsoft Store アプリの起動 (`shell:AppsFolder\<AUMID>` 形式)

### 含まない (本コマンドのスコープ外)

| 機能 | 理由 |
| --- | --- |
| 起動後の **attach** | launch は PID と実行ファイル情報のみ返し、attach は呼び出し側が `windows_attach` を別途呼ぶ。複数ウィンドウ・別プロセス昇格 (Office, UWP の `ApplicationFrameHost` 等) のあいまい解決を本コマンドに含めない |
| 起動完了の **待機** | `WaitForInputIdle` 等のウィンドウ可視化待ちはしない。即座に PID を返して終了。後続フローは必要に応じて呼び出し側が `windows_attach` のリトライ等で吸収する |
| **auto-snapshot** | attach をしないため snapshot 対象が無い |
| **管理者権限への昇格** (UAC ポップアップ) | UAC ダイアログ自体が UIA で操作不能 (Secure Desktop) であり、自動化フローと矛盾する |
| URI 起動 (`ms-settings:` / `https://` 等) | スコープを Win32 / UWP プロセス起動に限定 |
| シェル経由起動 (`cmd /c …`) | 起動後の PID 追跡が困難になるため見送り |

## 3. 入力要件

| 入力 | 必須 | 説明 |
| --- | --- | --- |
| 実行ファイルパス | 必須 | フルパス、相対パス、または PATH 探索対象の実行ファイル名 |
| コマンドライン引数 | 任意 | 0 個以上、複数指定可。スペース・引用符を含む引数も透過 |
| 作業ディレクトリ (cwd) | 任意 | 既定: 呼び出し元プロセスの cwd |
| 環境変数 | 任意 | `KEY=VALUE` 形式で 0 個以上。継承中の変数を上書きする |
| UWP モード指定 | 任意 (実装で自動判定可) | `shell:AppsFolder\<AUMID>` 形式の場合は UWP/Store アプリ起動として扱う |

### 排他制約

- UWP モード (UseShellExecute 系で起動が必要なケース) では、環境変数および cwd の指定を技術的に適用しきれない。指定された場合は `INVALID_ARGUMENT` ("unsupported with UWP launch") を返す。

## 4. 振る舞い

1. 入力検証 (実行ファイルパスの存在確認は **任意**。`Process.Start` の失敗を許容するため必須としない。ただし PATH 探索失敗・ファイル不在は `LAUNCH_FAILED` でエラーとする)
2. プロセス起動 (待機なし)
3. 起動成功時: PID・プロセス名・解決済み実行ファイルフルパス を含むレスポンスを返す
4. 起動失敗時: エラーレスポンスを返す

### 待機方針

- 起動完了は **待たない**。`Process.Start` が成功して PID が得られた時点で OK とする。
- 結果として「起動直後に異常終了するアプリ」を区別できないが、本コマンドではそれを許容する。
- 後続の `windows_attach` 等が UIA でウィンドウを見つけられるかどうかで間接的に判定する。

### 権限継承

- daemon (`adact serve`) が管理者権限で動作している場合、`launch` で起動した子プロセスも管理者権限を継承する (Windows の標準的なプロセス継承挙動)。
- daemon が標準ユーザーで動いている状態から子プロセスを管理者で起動する手段は提供しない (本コマンドは UAC 昇格を扱わない)。

## 5. 出力 (返り値)

| フィールド | 内容 |
| --- | --- |
| `pid` | 起動したプロセス ID |
| `processName` | プロセス名 (実行ファイル basename、拡張子付き) |
| `executablePath` | PATH 探索後の解決済みフルパス。UWP モードでは AUMID を返す (もしくは null)。詳細は設計で確定 |

attach 関連の情報 (window_ref, session_id 等) は返さない。

## 6. エラー方針

| 状況 | エラーコード (案) |
| --- | --- |
| 実行ファイルが見つからない / 起動失敗 (Process.Start が例外) | `LAUNCH_FAILED` |
| UWP モードで env / cwd が指定された等の引数矛盾 | `INVALID_ARGUMENT` |
| その他想定外例外 | 既存マッピングに従う |

エラーコードの厳密な命名・配置は設計書 (024) で確定する。

## 7. 公開面

- CLI: `adact launch <executable> [args...] [--cwd <dir>] [--env KEY=VALUE]...`
- MCP: `windows_launch`

両方を提供する。

## 8. テスト方針 (要件レベル)

- Engine / MCP / CLI すべてで Unit テストを追加する。
- 実機 UIA 起動を伴う E2E は追加しない (FlaUI 不要、`Process.Start` のみで完結するため Unit で十分検証可能)。
- 確認すべき主な観点:
  - PATH 探索 (notepad のような名前のみ指定)
  - フルパス指定
  - 引数・cwd・環境変数の伝搬 (実プロセスを起動して環境を読み戻すスモークが妥当)
  - 不在ファイル指定で `LAUNCH_FAILED`
  - UWP モードでの env/cwd 競合で `INVALID_ARGUMENT`

## 9. オープン項目 (設計フェーズへ持ち越し)

- UWP 起動の具体的実装手段 (`Process.Start` の `UseShellExecute=true` か、Windows API 経由の `IApplicationActivationManager` か)
- UWP モードでの `pid` 取得手段 (`UseShellExecute=true` 経路では `Process.Id` が取れない場合の代替)
- 環境変数のマージ規則 (継承を残すか、指定値のみで起動するか)
- 引数のクォーティング (空白を含む引数の安全な伝搬)
- エラーコードの最終命名・既存 `ToolErrors` との整合
- 配置先 (Engine: `WindowSession.cs` 系列か別レイヤーか。`launch` は session に紐づかない)
