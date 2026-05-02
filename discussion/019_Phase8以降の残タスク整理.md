# 残タスク整理

* 終わったものは削除する。
  * 取り消し線などは使わない
* 新規に追加する場合は、一番下に追加する
  * IDは最も大きいIDに+1したものとする。
  * IDは再利用しない（歯抜けを許容する）


| ID | 項目 | 内容 | 状態 | 備考 |
| :--- | :--- | :--- | :--- | :--- |
| 1 | モーダルダイアログ自動追随 | モーダルダイアログを検出し、操作対象として自動的に切り替える。 | 部分実施 | `SnapshotBuilder` で `ModalSiblings` を tree に追加済み。操作対象の自動切り替え（追随）は未実装。discussion/011 参照。 |
| 2 | `--verbose` 全コマンド展開 | `attach`/`click`/`fill` 等、すべての CLI コマンドで詳細ログを有効化できるようにする。 | 未着手 | 現状は `adact local --verbose` のみ対応。 |
| 3 | `REF_NOT_FOUND` 自動再 snapshot | 古い/消滅した ref に遭遇した際、AI 側判断・CLI ヒント・自動再 snapshot のどこまで担うかを実装する。 | 方針確定 | 責務分担の方針は確定済み。実装未対応。discussion/011 参照。 |
| 4 | `.adact/config.json` 拡充 | `defaultSnapshotDir` / `outputFormat` 等、接続先以外の永続設定を追加する。 | 未実装 | `server` フィールドは完了。個人設定とリポジトリ共有設定の分離も含む。discussion/009 §3.3 参照。 |
| 7 | recipes 提供 | 電卓・メモ帳等の典型操作テンプレートを配布する。 | 未着手 | `adact-cli` Skill 内に含めるか、別 Skill/ディレクトリとして提供するか未決定。 |
| 9 | 配布・インストール導線 | `adact` コマンド名だけで起動できるようにする。 | 未着手 | .NET tool / 自己完結バイナリ / installer / wrapper 等の方式を比較して決定する。 |
| 10 | FlaUI テストコード生成 | AI が探索した操作を FlaUI を用いた自動シナリオテストとして生成する。 | 未着手 | Codegen / recipes / Skill 拡張と合わせて設計。出力形式（xUnit+FlaUI 直接 or 中間シナリオ定義）と AI 探索・テスト生成の責務境界も未決定。 |
| 11 | 失敗時スクリーンショット自動添付 | 操作失敗時に自動的にスクリーンショットを撮影し、エラー出力に添付する。 | 未着手 | `screenshot` コマンドは実装済み。失敗時の自動連携が残タスク。`WindowSession.RunSerializedAsync` の catch ブロックでの自動撮影を検討。 |
| 12 | evaluate コマンド | UIA / アプリ固有操作の escape hatch として汎用実行コマンドを提供する。 | 未着手 | 安全性・API 境界を慎重に設計してから追加。採否未決定。 |
| 13 | 認証・TLS・CORS | リモート daemon 運用時の保護方針を決め、必要最小限を実装する。 | 未着手 | リモート daemon をどの時点で本格サポート扱いにするか未決定。 |
| 14 | Dashboard | daemon / session / window / snapshot の状態を可視化する管理 UI。 | 未着手 | 運用・デバッグ向け。 |
| 15 | OCR・Vision | UIA が弱いアプリに対して画像認識・OCR を併用する。 | 未着手 | 初期スコープ外だった領域。 |
| 16 | state 永続化 | daemon 再起動後も session / window / 設定を復元できるようにする。 | 未着手 | 現状は daemon メモリ内状態のみ。 |
| 17 | README.md 作成 | OSS 公開を見越したトップレベルの README.md を作成する。 | 未着手 | 内容（機能紹介、セットアップ、クイックスタート、ロードマップ等）は要検討。 |
| 18 | デフォルト接続先を local に | MCP 接続時のデフォルト接続先を `local`（stdio）に変更する。 | 未着手 | 現状のデフォルトが TCP 等の場合の変更。 |
| 19 | local 接続時の daemon 自動起動 | local 接続時に ADACT daemon プロセスが存在しなければ自動的に立ち上げる。 | 未着手 | ID 18 とセットで検討。 |
| 21 | SampleApp 更新：close 拒否パターン | SampleApp に `Closing` イベントで `e.Cancel = true` するボタンを追加し、`close` コマンドが効かないパターンを検証できるようにする。 | 未着手 | discussion/032 参照。WPF/WinForms で `close` が効かないケースを再現。 |
| 22 | SampleApp 更新：MenuItem にサブメニュー | SampleApp の MenuBar（File/Edit/View）に入れ子サブメニューを追加し、多階層メニューの操作検証を可能にする。 | 未着手 | discussion/032 参照。現在は1階層のみ（Open/Save/Exit）。マウスオーバーで右側に展開される多階層メニューの UIA 構造検証にも必要。 |
| 23 | FileDialog 操作の解決策検討 | `OpenFileDialog` / `SaveFileDialog` を ADACT で操作する方法（ファイル選択・キャンセル）を検討・実装する。 | 未着手 | discussion/032 参照。ダイアログの button を `click` で押せるが、ファイルパスの入力方法が未定。 |
| 24 | ComboBox 選択要素取得のスキル化 | `inspect` で子 ListItem を確認する方法、または `snapshot` の `[selected]` フラグを使った選択確認方法を `adact-cli` Skill に記載する。 | 未着手 | discussion/032 参照。ComboBox 自身の inspect では `IsSelected` が出ず、子 ListItem の inspect が必要。 |
| 25 | Skill と実体の乖離解消 | `adact-cli` Skill ファイルが実際の CLI 仕様と乖離している箇所を修正する（例：Skill に `close` コマンドがない、`inspect --ref` のまま等）。 | 未着手 | discussion/031（global option 化）、本日の `inspect`/`detach` 変更等。Skill ファイルは `.agents/skills/adact-cli/references/` および `src/Adact.Cli.Core/Skills/adact-cli/references/` に存在。 |
| 26 | Skill 更新：別ウィンドウ扱いの要素説明 | ツールチップ・メニューサブメニュー・ダイアログボックス等が UIA 上で「別ウィンドウ」として snapshot に現れることを Skill に記載する。 | 未着手 | discussion/030、032 参照。Popup (`isPopup`)、Modal (`isModalDialog`) の概念を Skill の `snapshot` リファレンス等に追記。 |
| 27 | 全サブコマンド自動統合テスト化 | discussion/032 で手動検証した内容（27コマンド）を自動テスト化。SampleApp を起動 → 操作 → 検証 → クリーンアップの一連を xUnit 化する。 | 未着手 | `Adact.Engine.Tests` または新規プロジェクトで実装。InteractiveTestGuard + CalculatorMutex パターンを参考に。 |
| 28 | 異なる DPI/スケーリング環境でのテスト | 125%, 150%, 200% 等のディスプレイスケーリングで `BoundingRectangle` の値が変わり、クリック位置がずれる可能性があることを検証する。 | 未着手 | `SetProcessDPIAware` 等の対応が必要か、DPI 非依存座標系の検討。 |
| 29 | UWP/Store アプリ対応テスト | Windows 電卓（新 `CalculatorApp`）等では UIA ツリー構造が異なり、既存操作パターンが通用しないケースを洗い出す。 | 未着手 | discussion/028 参照。UWP の `CoreWindow` 対応は部分的に実装済みだが、網羅的な検証が必要。 |
| 30 | TreeView 展開操作サポート | TreeItem の `click` では子ノードが展開されない（WPF 仕様）。`expand` / `collapse` コマンドの追加、または `click` に `--expand` オプションを追加する。 | 未着手 | discussion/032 参照。現状は Expander ボタンを個別に `click` する必要がある。 |
| 31 | Slider/ProgressBar の値変更操作 | `RangeValuePattern` を使った `set-value` コマンドを追加し、Slider の値を直接設定できるようにする。 | 未着手 | SampleApp の Basic Controls タブに Slider があるが、値変更コマンドがない。 |
| 32 | ContextMenu（右クリック）操作 | `right-click <ref>` コマンドを追加する。現状は `click` のみで右クリックメニューが開けない。 | 未着手 | SampleApp の Tree & Menu タブに ContextMenu があるが検証不可。 |
| 33 | snapshot diff 機能 | `adact diff <file1> <file2>` で2つの snapshot の差分を表示する機能を追加。UI の変化を追跡するのに有用。 | 未着手 | テスト自動化やリグレッション検出に活用可能。 |
| 34 | XML ドキュメントコメント warning 解消 | `dotnet build` で毎回出る CS1574/CS1734 warning（10件以上）を解消する。同時に、今後同様の warning が発生しないように Skill またはガイドラインに記載する。 | 未着手 | `SnapshotFileWriter.cs`（CS1734）、`SnapshotTreeFilter.cs`（CS1574）、`HttpHost.cs`（CS1570）等。CI 導入時に blocker になりうる。 |
| 35 | Adact.Cli と Adact.Cli.Client の Program.cs 重複解消 | 両プロジェクトでほぼ同じサブコマンド登録コードがある。共通化またはコード生成を検討する。 | 未着手 | `BuildRoot()` の内容がほぼ同一。`LocalCommand` / `ServeCommand` / `DaemonStopCommand` の有無だけの差。 |
| 36 | System.CommandLine beta5 → GA 移行 | 現在 `2.0.0-beta5.25306.1` を使用。`Recursive` 等の API が GA で確定しているか確認し、移行コストを見積もる。 | 未着手 | discussion/031 参照。beta → GA で API の破壊的変更がある可能性。 |
| 37 | エラーコード一覧整備 | `ErrorCodes` クラスと実際のエラーメッセージの対応表を `docs/` に作成。AI やユーザーがエラーの意味を素早く理解できるようにする。 | 未着手 | `CONNECTION_FAILED`、`OPERATION_BLOCKED`、`REF_NOT_FOUND` 等の対処法を含む。 |
| 38 | 操作履歴・ログの永続化 | 現状は操作ログが stdout/stderr のみ。`.adact/history/` にタイムスタンプ付きでログを残し、デバッグ・再現性向上に役立てる。 | 未着手 | セキュリティ考慮（パスワード等の機密情報フィルタリング）が必要。 |

# 終了タスク

| ID | 項目 | 内容 | 状態 | 備考 |
| :--- | :--- | :--- | :--- | :--- |
| 6 | `CalculatorMutex` 共通化 | 3 テストプロジェクトに重複している CalculatorMutex を共有テストヘルパーに集約する。 | 完了 | `Adact.Tests.Common` を新設し、`CalculatorMutex` / `InteractiveTestGuard` / `InteractiveFactAttribute` / `ExternalServerHelper` を一括集約。`Adact.Engine.Tests` の calc.exe テストにも `CalculatorMutex` を適用。 |
| 8 | 検証用サンプルアプリ | ADACT の主要操作を検証できる専用アプリを作成する。 | 完了 | 当面は既存アプリ（電卓・メモ帳・Chrome）で十分。modal / dynamic UI の再現性が必要になった場合に作成。技術選定（WinForms/WPF/Avalonia 等）も未決定。 |
| 20 | daemon-stop の local 専用化 | `daemon-stop` コマンドを local（stdio）接続時のみ有効にし、127.0.0.1 等のリモート接続時は実行不可にする。 | 完了 | ID 18・19 とセットで検討。セキュリティ観点からリモート側の誤停止を防ぐ。 |
| 5 | `KillAsync` PID 再利用対策 | `Process.StartTime` 等を使い、意図しない別プロセス kill を防ぐ。 | 却下 | 調査の結果、通常シナリオ（数秒〜数分後の kill）では PID 再利用の実害は極めて少ない。StartTime 比較はベストエフォートに留まり絶対安全ではなく、真に堅牢にするにはプロセスハンドル保持が必要でコストが大きいため、優先度を下げて見送る。 |