# 残タスク整理

* 完了、却下時は、「終了タスク」に移動する
  * 取り消し線などは使わない
* 新規に追加する場合は、一番下に追加する
  * IDは最も大きいIDに+1したものとする。
  * IDは再利用しない（歯抜けを許容する）


| ID | 項目 | 内容 | 状態 | 備考 |
| :--- | :--- | :--- | :--- | :--- |
| 4 | `.adact/config.json` 拡充 | `defaultSnapshotDir` / `outputFormat` 等、接続先以外の永続設定を追加する。 | 未実装 | `server` フィールドは完了。個人設定とリポジトリ共有設定の分離も含む。discussion/009 §3.3 参照。 |
| 27 | 全サブコマンド自動統合テスト化 | discussion/032 で手動検証した内容（27コマンド）を自動テスト化。SampleApp を起動 → 操作 → 検証 → クリーンアップの一連を xUnit 化する。 | 未着手 | `Adact.Engine.Tests` または新規プロジェクトで実装。InteractiveTestGuard + CalculatorMutex パターンを参考に。 |
| 38 | 操作履歴・ログの永続化 | 現状は操作ログが stdout/stderr のみ。`.adact/history/` にタイムスタンプ付きでログを残し、デバッグ・再現性向上に役立てる。 | 未着手 | セキュリティ考慮（パスワード等の機密情報フィルタリング）が必要。 |
| 40 | relay コマンド| AI実行環境とテスト実行環境が遠い(直接つながっていない)場合に、途中のマシンを中継サーバとして利用するためのコマンド | 未着手 | |
| 7 | recipes 提供 | 電卓・メモ帳等の典型操作テンプレートを配布する。 | 未着手 | `adact-cli` Skill 内に含めるか、別 Skill/ディレクトリとして提供するか未決定。 |
| 12 | evaluate コマンド | UIA / アプリ固有操作の escape hatch として汎用実行コマンドを提供する。 | 未着手 | 安全性・API 境界を慎重に設計してから追加。採否未決定。 |
| 2 | `--verbose` 全コマンド展開 | `attach`/`click`/`fill` 等、すべての CLI コマンドで詳細ログを有効化できるようにする。 | 未着手 | 現状は `adact local --verbose` のみ対応。 |
| 33 | snapshot diff 機能 | `adact diff <file1> <file2>` で2つの snapshot の差分を表示する機能を追加。UI の変化を追跡するのに有用。 | 未着手 | テスト自動化やリグレッション検出に活用可能。 |
| 28 | 異なる DPI/スケーリング環境でのテスト | 125%, 150%, 200% 等のディスプレイスケーリングで `BoundingRectangle` の値が変わり、クリック位置がずれる可能性があることを検証する。 | 未着手 | `SetProcessDPIAware` 等の対応が必要か、DPI 非依存座標系の検討。 |
| 29 | UWP/Store アプリ対応テスト | Windows 電卓（新 `CalculatorApp`）等では UIA ツリー構造が異なり、既存操作パターンが通用しないケースを洗い出す。 | 未着手 | discussion/028 参照。UWP の `CoreWindow` 対応は部分的に実装済みだが、網羅的な検証が必要。 |
| 15 | OCR・Vision | UIA が弱いアプリに対して画像認識・OCR を併用する。 | 未着手 | 初期スコープ外だった領域。 |
| 13 | 認証・TLS・CORS | リモート daemon 運用時の保護方針を決め、必要最小限を実装する。 | 未着手 | リモート daemon をどの時点で本格サポート扱いにするか未決定。 |
| 14 | Dashboard | daemon / session / window / snapshot の状態を可視化する管理 UI。 | 未着手 | 運用・デバッグ向け。 |

# 終了タスク

| ID | 項目 | 内容 | 状態 | 備考 |
| :--- | :--- | :--- | :--- | :--- |
| 1 | モーダルダイアログ自動追随 | モーダルダイアログを検出し、操作対象として自動的に切り替える。 | 完了 | `SnapshotBuilder` で `ModalSiblings` を tree に追加済み。snapshot にモーダルが含まれるため、AI が ref を指定して操作可能。追加の「自動追随」ロジックは不要と判断。 |
| 3 | `REF_NOT_FOUND` 自動再 snapshot | 古い/消滅した ref に遭遇した際、AI 側判断・CLI ヒント・自動再 snapshot のどこまで担うかを実装する。 | 完了 | Engine は `REF_NOT_FOUND` を返すのみ。AI/ユーザ が snapshot 再取得を判断する責務分担で確定。CLI 側の自動リトライは不要と判断。 |
| 6 | `CalculatorMutex` 共通化 | 3 テストプロジェクトに重複している CalculatorMutex を共有テストヘルパーに集約する。 | 完了 | `Adact.Tests.Common` を新設し、`CalculatorMutex` / `InteractiveTestGuard` / `InteractiveFactAttribute` / `ExternalServerHelper` を一括集約。`Adact.Engine.Tests` の calc.exe テストにも `CalculatorMutex` を適用。 |
| 8 | 検証用サンプルアプリ | ADACT の主要操作を検証できる専用アプリを作成する。 | 完了 | 当面は既存アプリ（電卓・メモ帳・Chrome）で十分。modal / dynamic UI の再現性が必要になった場合に作成。技術選定（WinForms/WPF/Avalonia 等）も未決定。 |
| 18 | デフォルト接続先を Named Pipe に | MCP 接続時のデフォルト接続先を Named Pipe に変更する。`adact local` (stdio) は廃止。 | 完了 | `adact serve pipe` を新設し、デフォルト接続先とした。HTTP (`adact serve http`) はリモート用に残す。discussion/033 参照。 |
| 19 | Named Pipe 接続時の daemon 自動起動 | Named Pipe 接続時に ADACT daemon プロセスが存在しなければ自動的に立ち上げる。 | 完了 | `list-apps` / `launch` のみ自動起動対象。それ以外は `CONNECTION_FAILED` エラー。discussion/033 参照。 |
| 20 | daemon-stop の local 専用化 | `daemon-stop` コマンドを local（stdio）接続時のみ有効にし、127.0.0.1 等のリモート接続時は実行不可にする。 | 完了 | ID 18・19 とセットで検討。セキュリティ観点からリモート側の誤停止を防ぐ。 |
| 5 | `KillAsync` PID 再利用対策 | `Process.StartTime` 等を使い、意図しない別プロセス kill を防ぐ。 | 完了 | 調査の結果、通常シナリオ（数秒〜数分後の kill）では PID 再利用の実害は極めて少ない。StartTime 比較はベストエフォートに留まり絶対安全ではなく、真に堅牢にするにはプロセスハンドル保持が必要でコストが大きいため、優先度を下げて見送る。 |
| 11 | 失敗時スクリーンショット自動添付 | 操作失敗時に自動的にスクリーンショットを撮影し、エラー出力に添付する。 | 却下 | `REF_NOT_FOUND` 自動再 snapshot と同様の理由。失敗時のスクリーンショット撮影は AI/ユーザ が手動で判断・実行する責務分担とする。 |
| 16 | state 永続化 | daemon 再起動後も session / window / 設定を復元できるようにする。 | 却下 | 現状は daemon メモリ内状態のみ。 |
| 17 | README.md 作成 | OSS 公開を見越したトップレベルの README.md を作成する。 | 完了 | 内容（機能紹介、セットアップ、クイックスタート、ロードマップ等）は要検討。 |
| 32 | ContextMenu（右クリック）操作 | `right-click <ref>` コマンドを追加する。現状は `click` のみで右クリックメニューが開けない。 | 却下 | SampleApp の Tree & Menu タブに ContextMenu があるが検証不可。 |
| 41 | CLI 出力形式統一 | サブコマンドごとに JSON / TSV / snapshot / 1行 / 出力なし とバラバラだった CLI の stdout を統一する。 | 完了 | yaml風 / TSV風 / snapshot の 3 パターンに統一。すべて stdout に出力。`result` を必須化。`--json` は未導入。discussion/042 参照。 |
| 9 | 配布・インストール導線 | `adact` コマンド名だけで起動できるようにする。 | 完了 | .NET tool / 自己完結バイナリ / installer / wrapper 等の方式を比較して決定する。 |
| 39 | HTTP プロキシ対応 | `http_proxy` / `https_proxy` 環境変数または `.adact/config.json` 経由で HTTP プロキシを設定できるようにする。現状は `HttpClientTransport` がデフォルト `HttpClient` を使用しており、環境変数を無視する。 | 却下 | `AdactMcpClient.cs` で `HttpClientHandler` をカスタマイズし `Proxy` プロパティを設定する必要がある。MCP SDK 1.2.0 使用。 |
| 34 | XML ドキュメントコメント warning 解消 | `dotnet build` で毎回出る CS1574/CS1734 warning（10件以上）を解消する。同時に、今後同様の warning が発生しないように Skill またはガイドラインに記載する。 | 完了 | `SnapshotFileWriter.cs`（CS1734）、`SnapshotTreeFilter.cs`（CS1574）、`HttpHost.cs`（CS1570）等。CI 導入時に blocker になりうる。 |
| 35 | Adact.Cli と Adact.Cli.Client の Program.cs 重複解消 | 両プロジェクトで重複していた共通サブコマンド登録を `Adact.Cli.Core` の `RootCommandRegistration` に集約した。 | 完了 | `serve` / `daemon-stop` と runtime 初期化の差分は各 Program.cs に残し、`install` / `launch` の順序差分も維持。 |
| 36 | System.CommandLine beta5 → GA 移行 | 現在 `2.0.0-beta5.25306.1` を使用。`Recursive` 等の API が GA で確定しているか確認し、移行コストを見積もる。 | 完了 | 2.0.7 にアップデート。ビルド・Unit テストともに問題なし。 |
| 10 | FlaUI テストコード生成 | AI が探索した操作を FlaUI を用いた自動シナリオテストとして生成する。 | 完了 | Codegen / recipes / Skill 拡張と合わせて設計。出力形式（xUnit+FlaUI 直接 or 中間シナリオ定義）と AI 探索・テスト生成の責務境界も未決定。 |
| 37 | エラーコード一覧整備 | `ErrorCodes` クラスと実際のエラーメッセージの対応表を `docs/` に作成。AI やユーザーがエラーの意味を素早く理解できるようにする。 | 完了 | Skill SKILL.md に全19コード + 対処法表を配置。docs/spec/errors-and-output.md にも対処法列を追加。 |
| 21 | SampleApp 更新：close 拒否パターン | SampleApp のメイン MenuBar の File 配下に close 拒否を切り替えるチェック項目を追加し、`Closing` イベントで `close` 要求を拒否できるようにした。 | 完了 | discussion/032 参照。`MainWindow_MenuItem_File_BlockClose` を ON にするとウィンドウ close を拒否する。 |
| 22 | SampleApp 更新：MenuItem にサブメニュー | SampleApp のメイン MenuBar の View 配下に多階層サブメニューを追加し、メインメニュー上で入れ子メニューの検証を可能にした。 | 完了 | discussion/032 参照。`View > Layout > Navigation Pane > Favorites` などの階層を追加。 |
| 26 | Skill 更新：別ウィンドウ扱いの要素説明 | ツールチップ・メニューサブメニュー・ダイアログボックス等が UIA 上で「別ウィンドウ」として snapshot に現れることを Skill に記載する。 | 完了 | `references/popup-and-modal.md` に詳細を記載。SKILL.md・snapshots-and-inspection.md からリンク。`docs/spec/snapshot.md` に `isPopup` フィールド追加。 |
| 23 | FileDialog 操作の解決策検討 | `OpenFileDialog` / `SaveFileDialog` を ADACT で操作する方法（ファイル選択・キャンセル）を検討・実装する。 | 完了 | discussion/052 参照。Skill `references/file-dialog.md` にナビゲーションバー直接入力パターンを記載。 |
