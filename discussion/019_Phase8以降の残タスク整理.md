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
| 5 | `KillAsync` PID 再利用対策 | `Process.StartTime` 等を使い、意図しない別プロセス kill を防ぐ。 | 保留 | 調査の結果、通常シナリオ（数秒〜数分後の kill）では PID 再利用の実害は極めて少ない。StartTime 比較はベストエフォートに留まり絶対安全ではなく、真に堅牢にするにはプロセスハンドル保持が必要でコストが大きいため、優先度を下げて見送る。 |
| 6 | `CalculatorMutex` 共通化 | 3 テストプロジェクトに重複している CalculatorMutex を共有テストヘルパーに集約する。 | 完了 | `Adact.Tests.Common` を新設し、`CalculatorMutex` / `InteractiveTestGuard` / `InteractiveFactAttribute` / `ExternalServerHelper` を一括集約。`Adact.Engine.Tests` の calc.exe テストにも `CalculatorMutex` を適用。 |
| 7 | recipes 提供 | 電卓・メモ帳等の典型操作テンプレートを配布する。 | 未着手 | `adact-cli` Skill 内に含めるか、別 Skill/ディレクトリとして提供するか未決定。 |
| 8 | 検証用サンプルアプリ | ADACT の主要操作を検証できる専用アプリを作成する。 | 未着手 | 当面は既存アプリ（電卓・メモ帳・Chrome）で十分。modal / dynamic UI の再現性が必要になった場合に作成。技術選定（WinForms/WPF/Avalonia 等）も未決定。 |
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
| 20 | daemon-stop の local 専用化 | `daemon-stop` コマンドを local（stdio）接続時のみ有効にし、127.0.0.1 等のリモート接続時は実行不可にする。 | 未着手 | ID 18・19 とセットで検討。セキュリティ観点からリモート側の誤停止を防ぐ。 |
| 21 | オプションの順番の柔軟化 | 現状は、 "list-apps --server xxx" は OK だが、 "--server xxx list-apps" は NG | 未着手 |  |
