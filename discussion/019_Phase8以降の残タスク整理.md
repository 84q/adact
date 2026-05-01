# Phase 8 以降の残タスク整理

> 前提: [001_要件定義.md](001_要件定義.md) / [003_実装計画.md](003_実装計画.md) / [008_要件再整理.md](008_要件再整理.md) / [010_Phase5_完了.md](010_Phase5_完了.md) / [011_ref安定化.md](011_ref安定化.md) / [014_Phase6_完了.md](014_Phase6_完了.md) / [015_Phase7要件定義.md](015_Phase7要件定義.md) / [016_Phase7設計.md](016_Phase7設計.md) / [017_Phase7_完了.md](017_Phase7_完了.md) / [027_操作ブロック検知.md](027_操作ブロック検知.md) / [028_UWPアプリsnapshot内部要素取得問題.md](028_UWPアプリsnapshot内部要素取得問題.md)
> 目的: Phase 7 完了時点で残っている受入タスク・安定化タスク・Phase 5/6 からの申し送りを棚卸しし、Phase 8 以降で追加すべき機能候補を整理する。
> **更新日: 2026-05-01** — Phase 8 実装完了と 027/028 の完了を反映。

---

## 1. Phase 7 完了時点の現状サマリ

### 1.1 確定済み

| Phase | 状態 | 内容 |
| --- | --- | --- |
| Phase 5 | 完了 | CLI 基本実装が完了。`list-apps` / `attach` / `snapshot` / `click` / `fill` に加え、当初 Phase 8 候補だった lifecycle 系 (`detach` / `close` / `kill` / `close-all` / `daemon-stop`) も前倒しで完了している。 |
| Phase 5 post-task | 完了 | Element Ref 安定化が完了。ref は generation なしの `s<sid>e<eid>` 形式に整理済み。 |
| Phase 6 | 完了 | `adact install --skills` による Skill 機構が完了。初期 Skill は 5 サブコマンド (`list-apps` / `attach` / `snapshot` / `click` / `fill`) を対象としている。 |
| Phase 7 | 完了 | snapshot 出力刷新が完了。CLI snapshot は `.txt` の Playwright Aria YAML 風テキストになり、MCP は raw JSON、CLI がフィルタ・整形を担う構成になった。AI クライアント手動スモークも完了（Codex / Claude Code / Copilot いずれも snapshot から ref を抽出して操作可能）。 |
| Phase 8-A | 完了 | `launch` / `wait-for` / `wait-for-window` / `press` / `type` を実装。AI がアプリ起動から基本操作まで CLI で一通り実行可能。 |
| Phase 8-B | 完了 | `screenshot` / `hover` / `keyboard` (`key-down` / `key-up`) / `mouse` (`mouse-move` / `mouse-down` / `mouse-up` / `mouse-wheel`) を実装。診断と低レベル操作の escape hatch を提供。 |
| Phase 8-C | 一部完了 | `select` (`select-option` 相当) と `inspect` (`get-value` 相当) を実装。`evaluate` は未実装。 |
| 027 | 完了 | 画面ロック/UAC 検知 (`OPERATION_BLOCKED`) を実装。操作失敗時 catch ブロックで検知し、明示的なエラーコードを返す。 |
| 028 | 完了 | UWP アプリ（電卓）の snapshot で内部要素が取得できない問題を解決。`CoreWindow` に対して `FindAllDescendants()` を使用し、重複排除を追加。 |

### 1.2 未完了

| 区分 | 内容 | 根拠 |
| --- | --- | --- |
| 旧 Phase 7 安定化 | Phase 7 が snapshot 出力刷新へ再定義されたため、旧 Phase 7 安定化項目の一部は未消化のまま残っている。 | [003_実装計画.md](003_実装計画.md) / [008_要件再整理.md](008_要件再整理.md) / [010_Phase5_完了.md](010_Phase5_完了.md) |

---

## 2. 残タスク一覧

### 2.1 確定済み: Phase 7 受入残

| タスク | 内容 | 状態 |
| --- | --- | --- |
| AI クライアント手動スモーク | 新 `.txt` snapshot を Phase 6 Skill 経由の AI クライアントが読み、ref を抽出して `click` / `fill` まで到達できることを確認する。 | **完了** — Codex / Claude Code / VS Code Copilot いずれも正常に動作。 |

### 2.2 確定済み: 旧 Phase 7 安定化の未消化

| タスク | 内容 | 状態 | 備考 |
| --- | --- | --- | --- |
| `AttachQuery.Hwnd` / `--hwnd` | window handle 直接指定で attach できるようにする。 | **未実装** | `attach` の既存フラグ体系に追加する想定。 |
| モーダルダイアログ検出/追随 | モーダルダイアログを検出し、操作対象として自動追随できるようにする。 | **部分実装** | snapshot 出力にはモーダル要素を含めるようになった（`SnapshotBuilder` で `ModalSiblings` を tree に追加）。自動的な操作対象切り替え（追随）は未実装。 |
| 画面ロック検知 | Windows デスクトップセッションがロックされている場合に、操作不能であることを明示的に返す。 | **完了** | [027](027_操作ブロック検知.md) で `OPERATION_BLOCKED` として実装。操作失敗時 catch ブロックで検知。 |
| 失敗時詳細ログ | click/fill/snapshot 失敗時に対象 ref、要素情報、例外情報を追えるようにする。 | **部分実装** | `WindowSession.RunSerializedAsync` で例外種別・ref・メッセージをログ出力。完全な構造化ログ（JSON 形式など）は未実装。 |
| 構造化ログ / `--verbose` | stderr ログを運用しやすくし、CLI から詳細ログを有効化できるようにする。 | **部分実装** | `adact local --verbose` で Debug レベル出力が有効化可能。他のコマンド（`attach`, `click` など）には `--verbose` 未対応。 |
| 失敗時スクリーンショット | 操作失敗時に状態確認用スクリーンショットを保存する。 | **未実装** | `screenshot` コマンドは実装済み（Phase 8-B）。失敗時の自動撮影・添付は未統合。 |
| snapshot 追加チューニング | 実利用で必要になった場合、重複属性省略・追加フィールド・長大値 truncate などを検討する。 | **対応不要** | 現状の `.txt` 出力で十分。UWP 対応（028）により要素数が増えたが、パフォーマンスは問題なし。 |

### 2.3 確定済み: Phase 5/6 からの申し送り

| タスク | 内容 | 状態 | 出所 |
| --- | --- | --- | --- |
| 接続先環境変数 | `.adact/config.json` と `--server` に加え、環境変数で daemon 接続先を指定できるようにする。 | **未実装** | Phase 5 |
| 認証 / TLS / CORS | リモート daemon 運用時の保護方針を決め、必要最小限を実装する。 | **未実装** | Phase 5 |
| `--format` | CLI 出力の Markdown / JSON 等の切替要否を検討する。 | **未実装** | Phase 5 |
| `REF_NOT_FOUND` 時の再 snapshot 方針 | 古い ref / 消滅 ref に遭遇したとき、AI 側判断・CLI ヒント・自動再 snapshot のどこまで担うか決める。 | **方針確定** | Phase 5 / Phase 6 |
| `.adact/config.json` 拡充 | 接続先以外の設定項目、個人設定とリポジトリ共有設定の分離、検索ルールを拡張する。 | **未実装** | Phase 5 |
| `KillAsync` の PID 再利用対策 | `Process.StartTime` 等を使い、意図しない別プロセス kill を防ぐ。 | **未実装** | Phase 5 |
| `CalculatorMutex` 共通化 | テストアセンブリ間で重複している Calculator mutex を共有テストヘルパへ集約する。 | **未実装** | Phase 5 |
| recipes | 電卓・メモ帳などの典型操作テンプレートを提供するか検討する。 | **未実装** | Phase 6 |
| MCP tool description 強化 | MCP 接続のみで AI クライアントが ADACT を理解できるよう、ツール description を強化する。 | **完了** | Phase 6 |
| Skill 対象拡張 | Phase 8 以降で CLI/MCP サブコマンドを追加した場合、`adact-cli` Skill の `references/<cmd>.md` と同期テストを更新する。 | **完了** | Phase 6 |

### 2.4 今後のタスク候補

| タスク候補 | 内容 | 整理先 |
| --- | --- | --- |
| cross-platform CLI client / 遠隔操作端末対応 | `adact serve` は Windows only のままとし、実際に GUI を操作しない CLI クライアント側・遠隔操作端末側では macOS / Linux でも起動できるようにする。パッケージング、target framework 分離、OS 制限の切り分けを含めて整理する。 | Phase 9 以降候補 |
| 動作確認用サンプルアプリ | 電卓等では確認しづらい項目や、既存アプリに関する AI の事前知識による検証の曖昧さを避けるため、ADACT の操作・snapshot・wait・keyboard/mouse・modal 等を検証できる専用サンプルアプリを作成する。 | Phase 8 または Phase 9 以降候補 |
| `adact` コマンド単体起動 | 現状の `/path/to/adact.exe` 前提をなくし、Playwright CLI のように `adact` だけで起動できる配布・インストール導線を整える。 | Phase 9 以降候補 |
| FlaUI テストコード生成 | シナリオに沿って AI がアプリケーションを探索しつつ、自動シナリオテストを生成するための起動方法、recipes、Skill 拡張、Codegen を整理する。 | Phase 9 以降候補 |

---

## 3. Phase 8 で追加すべき機能

### 3.1 確定済み: Phase 8 新規対象から除外するもの

| 機能 | 理由 |
| --- | --- |
| `close` | Phase 5 で実装済み。 |
| `detach` | Phase 5 で実装済み。 |
| `kill` | Phase 5 で実装済み。 |
| `close-all` | Phase 5 で実装済み。 |
| `daemon-stop` | Phase 5 で実装済み。 |

### 3.2 実装済み: Phase 8-A / 8-B / 8-C

| 機能 | 目的 | 状態 |
| --- | --- | --- |
| `launch` | アプリ起動。 | **完了** — Win32 / .NET / UWP（AUMID）対応。 |
| `wait-for` | 要素・状態の明示待機（ref または検索条件）。 | **完了** |
| `wait-for-window` | トップレベルウィンドウの出現待機。 | **完了** |
| `press` | 特定要素またはアクティブ要素へのキーコンボ押下。 | **完了** |
| `type` | テキスト入力の逐次入力。 | **完了** |
| `screenshot` | 画面状態の PNG 保存。 | **完了** — ウィンドウ全体または要素指定。 |
| `hover` | 要素 hover。 | **完了** |
| `keyboard` (`key-down` / `key-up`) | 低レベルキーボード操作。 | **完了** |
| `mouse` (`mouse-move` / `mouse-down` / `mouse-up` / `mouse-wheel`) | 低レベルマウス操作。 | **完了** |
| `select` | ComboBox / ListBox 等の選択操作（`--name` / `--index` / `--item-ref`）。 | **完了** — 当初 `select-option` として計画。 |
| `inspect` | 要素の詳細 UIA プロパティ取得（JSON 1 行出力）。 | **完了** — 当初 `get-value` として計画。 |

### 3.3 未実装: 次点候補

| 機能 | 目的 | 採否判断 |
| --- | --- | --- |
| `evaluate` | UIA / アプリ固有操作の escape hatch。 | 安全性・API 境界を慎重に設計してから追加する。未実装。 |

### 3.4 提案: Phase 8 に組み込む可能性がある候補

| 候補 | 目的 | 採否判断 |
| --- | --- | --- |
| 動作確認用サンプルアプリ | Phase 8-A / 8-B の `wait-for`、`press`、`type`、`screenshot`、`hover`、`keyboard`、`mouse`、modal 追随などを再現性高く検証する。 | Phase 8 の受入で既存アプリ（電卓・メモ帳・Chrome）で十分検証できたため、当面は不要。Phase 9 以降で必要になった場合に作成する。 |

---

## 4. Phase 8 完了サマリ

### 4.1 実装結果

| 分割 | スコープ | 状態 |
| --- | --- | --- |
| Phase 8-A | `launch` / `wait-for` / `wait-for-window` / `press` / `type` など操作基盤 | **完了** |
| Phase 8-B | `screenshot` / `hover` / `keyboard` / `mouse` など診断・低レベル操作 | **完了** |
| Phase 8-C | `select` / `inspect` / `evaluate` など読み取り・選択・拡張操作 | **`select`・`inspect` 完了** / `evaluate` は未実装 |

### 4.2 補足: 失敗時スクリーンショット

`screenshot` コマンド自体は Phase 8-B で完了したが、**失敗時の自動スクリーンショット撮影・添付**は未統合。次の Phase で検討する。

---

## 5. Phase 9 以降候補

### 5.1 提案

| 候補 | 内容 | 状態 | 備考 |
| --- | --- | --- | --- |
| Dashboard | daemon / session / window / snapshot の状態を可視化する管理 UI。 | 未着手 | 運用・デバッグ向け。 |
| OCR / Vision | UIA が弱いアプリに対して画像認識・OCR を併用する。 | 未着手 | 初期スコープ外だった領域。 |
| 安定セレクタ生成 | ref だけでなく、再実行可能なセレクタを生成する。 | 未着手 | Codegen と相性がよい。 |
| Codegen | AI / 人間の操作をテストコードとして残す。 | 未着手 | 初期要件の将来機能。 |
| state 永続化 | daemon 再起動後も session / window / 設定を復元できるようにする。 | 未着手 | 現状は daemon メモリ内状態のみ。 |
| recipes | 電卓・メモ帳などの典型操作テンプレートを配布する。 | 未着手 | Skill とは別 Skill 化するか、`references/` 拡張で吸収するか要設計。 |
| cross-platform CLI client | GUI 操作を行わない CLI クライアントや遠隔操作端末を macOS / Linux でも起動できるようにする。 | 未着手 | `adact serve` は Windows only のまま、client / server の target framework と OS ガードを分離する。 |
| 配布・インストール導線 | `/path/to/adact.exe` を指定せず、`adact` コマンド名だけで起動できるようにする。 | 未着手 | .NET tool、自己完結配布、PATH 追加、installer などの方式を比較する。 |
| 検証用サンプルアプリ | ADACT の主要操作を確認する専用アプリを提供する。 | 未着手 | 既存アプリ依存のスモークを減らし、AI の既知情報に依存しない検証を行う。当面は既存アプリで十分。 |
| FlaUI テストコード生成 | AI が探索した操作を、FlaUI を用いた自動シナリオテストとして生成する。 | 未着手 | Codegen / recipes / Skill 拡張と合わせて設計する。 |
| 失敗時スクリーンショット自動撮影 | 操作失敗時に自動的にスクリーンショットを撮影し、エラー出力に添付する。 | 未着手 | `screenshot` コマンドは実装済み。失敗時の自動連携が残タスク。 |
| `.adact/config.json` | 接続先・個人設定・検索ルールなどの永続設定。 | 未着手 | Phase 5 申し送り。 |
| 環境変数による接続先指定 | `ADACT_SERVER` などの環境変数で daemon 接続先を指定。 | 未着手 | Phase 5 申し送り。 |
| PID 再利用対策 | `KillAsync` で `Process.StartTime` を使った同一性検証。 | 未着手 | Phase 5 申し送り。 |
| `CalculatorMutex` 共通化 | 3 テストプロジェクトに重複している CalculatorMutex を集約。 | 未着手 | Phase 5 申し送り。技術的負債ではあるが動作に影響なし。 |

---

## 6. 決定事項と未決事項

### 6.1 確定済み

| 項目 | 決定 |
| --- | --- |
| Phase 5 の成果 | CLI 基本実装と lifecycle 系は完了済み。Phase 8 の新規対象に `close` / `detach` / `kill` / `close-all` / `daemon-stop` は含めない。 |
| Phase 6 の成果 | Skill 機構は完了済み。今後サブコマンドを追加・改名・削除した場合は Skill と同期テストを更新する。 |
| Phase 7 の成果 | snapshot 出力刷新は実装完了。`.txt` 形式と CLI 側フィルタ・整形の責務分担を前提にする。AI クライアント手動スモークも完了。 |
| Phase 8-A の成果 | `launch` / `wait-for` / `wait-for-window` / `press` / `type` は完了。 |
| Phase 8-B の成果 | `screenshot` / `hover` / `keyboard` / `mouse` は完了。 |
| Phase 8-C の成果 | `select` / `inspect` は完了。`evaluate` は未実装。 |
| 027 の成果 | 画面ロック/UAC 検知 (`OPERATION_BLOCKED`) は完了。 |
| 028 の成果 | UWP アプリ snapshot の内部要素取得は完了。 |
| Element Ref | `s<sid>e<eid>` 形式を継続する。generation 付き ref は旧 baseline のみに残る過去形式として扱う。 |
| `REF_NOT_FOUND` 時の方針 | Skill ドキュメントとエラーメッセージで「`adact snapshot` を再実行し、新しい ref を取得して retry」と案内する。自動再 snapshot は未実装。 |

### 6.2 提案（Phase 9 以降）

| 項目 | 提案 |
| --- | --- |
| 失敗時スクリーンショット | `screenshot` 実装後に失敗時添付と統合する。`WindowSession.RunSerializedAsync` の catch ブロックで自動撮影を検討。 |
| cross-platform CLI client | `adact serve` の Windows only は維持しつつ、GUI を直接操作しない CLI クライアント・遠隔操作端末側は macOS / Linux 対応を検討する。 |
| 動作確認用サンプルアプリ | 当面は既存アプリ（電卓・メモ帳・Chrome）で十分。Phase 9 以降で modal / dynamic UI の再現性が必要になった場合に作成する。 |
| `adact` コマンド単体起動 | 配布・インストール導線を整備し、コマンド名だけで起動できる状態を目指す。 |
| FlaUI テストコード生成 | AI 探索からテストコード生成へつなげる Codegen / recipes / Skill 拡張を将来機能として整理する。 |
| `.adact/config.json` | 接続先・個人設定・検索ルールなどの永続設定を整備する。 |
| PID 再利用対策 | `KillAsync` に `Process.StartTime` による同一性検証を追加する。 |
| `CalculatorMutex` 共通化 | 3 テストプロジェクトに重複している `CalculatorMutex` を共有テストヘルパーに集約する。 |

### 6.3 未決

| 項目 | 決めること |
| --- | --- |
| `evaluate` の採否 | 汎用 escape hatch として必要か、セキュリティ・保守性の観点で避けるか。 |
| 認証 / TLS / CORS | リモート daemon をどの時点で本格サポート扱いにするか。 |
| recipes の配置 | `adact-cli` Skill 内に含めるか、別 Skill / 別ディレクトリとして提供するか。 |
| cross-platform CLI client の範囲 | macOS / Linux で起動可能にする対象を CLI client のみに限定するか、MCP stdio / HTTP client 相当まで含めるか。 |
| target framework 分離 | Windows 専用の `adact serve` と cross-platform な CLI client を、プロジェクト分割・multi-target・コマンド分離のどの方式で実現するか。 |
| 配布方式 | `adact` コマンド単体起動を .NET tool、自己完結バイナリ、installer、npm-style wrapper のどの導線で提供するか。 |
| サンプルアプリの技術選定 | WinForms / WPF / Avalonia / WebView 等のどれで検証用サンプルアプリを作るか。UIA 対象としての安定性と cross-platform 方針をどう扱うか。 |
| サンプルアプリの検証項目 | snapshot / wait / keyboard / mouse / modal / focus / disabled state / dynamic UI のうち、初期版でどこまで含めるか。 |
| FlaUI テストコード生成の出力形式 | 生成対象を xUnit + FlaUI の直接コードにするか、recipes / 中間シナリオ定義からコード生成するか。 |
| AI 探索とテスト生成の責務境界 | Skill が探索手順を案内するだけにするか、ADACT 側に codegen コマンドを追加するか。 |
| `--format` の要否 | CLI 出力を Markdown / JSON で切り替える必要があるか。現状のテキスト出力で十分か。 |
| `--verbose` の全コマンド展開 | `local` 以外のコマンド（`attach`, `click` など）でも `--verbose` をサポートするか。 |

---

## 7. 次の着手順序案（Phase 9 以降）

### 7.1 提案

| 順序 | 内容 | 理由 |
| --- | --- | --- |
| 1 | 旧 Phase 7 安定化のうち `--hwnd` を実装する | タイトル以外の attach 手段を増やし、タイトル重複時の運用性を向上する。 |
| 2 | 失敗時スクリーンショット自動撮影 | `screenshot` コマンドは完成している。操作失敗時 catch ブロックへの統合でデバッグ体験を飛躍的に改善する。 |
| 3 | `.adact/config.json` と環境変数対応 | 接続先設定の永続化と、毎回 `--server` を指定しない運用性の向上。 |
| 4 | `--verbose` の全コマンド展開 | 現状は `local` のみ。`attach` / `click` などでも詳細ログを有効化できるようにする。 |
| 5 | PID 再利用対策 + `CalculatorMutex` 共通化 | 技術的負債の解消。動作に影響はないが、テストの堅牢性と保守性を向上する。 |
| 6 | Phase 9 新規機能の採否再判断 | `evaluate`、recipes、サンプルアプリ、Codegen など、実利用で不足が見えたものから優先する。 |
| 7 | cross-platform CLI client / 配布導線の設計 | `adact serve` の Windows only を維持しながら、遠隔操作端末と `adact` コマンド単体起動の実現方式を決める。 |
| 8 | FlaUI テストコード生成の構想整理 | Codegen / recipes / Skill 拡張の責務境界を整理し、安定セレクタ生成との関係を決める。 |
