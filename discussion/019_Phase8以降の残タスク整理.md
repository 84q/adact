# Phase 8 以降の残タスク整理

> 前提: [001_要件定義.md](001_要件定義.md) / [003_実装計画.md](003_実装計画.md) / [008_要件再整理.md](008_要件再整理.md) / [010_Phase5_完了.md](010_Phase5_完了.md) / [011_ref安定化.md](011_ref安定化.md) / [014_Phase6_完了.md](014_Phase6_完了.md) / [015_Phase7要件定義.md](015_Phase7要件定義.md) / [016_Phase7設計.md](016_Phase7設計.md) / [017_Phase7_完了.md](017_Phase7_完了.md)
> 目的: Phase 7 完了時点で残っている受入タスク・安定化タスク・Phase 5/6 からの申し送りを棚卸しし、Phase 8 以降で追加すべき機能候補を整理する。

---

## 1. Phase 7 完了時点の現状サマリ

### 1.1 確定済み

| Phase | 状態 | 内容 |
| --- | --- | --- |
| Phase 5 | 完了 | CLI 基本実装が完了。`list-apps` / `attach` / `snapshot` / `click` / `fill` に加え、当初 Phase 8 候補だった lifecycle 系 (`detach` / `close` / `kill` / `close-all` / `daemon-stop`) も前倒しで完了している。 |
| Phase 5 post-task | 完了 | Element Ref 安定化が完了。ref は generation なしの `s<sid>e<eid>` 形式に整理済み。 |
| Phase 6 | 完了 | `adact install --skills` による Skill 機構が完了。初期 Skill は 5 サブコマンド (`list-apps` / `attach` / `snapshot` / `click` / `fill`) を対象としている。 |
| Phase 7 | 実装完了 / 受入一部残 | snapshot 出力刷新が完了。CLI snapshot は `.txt` の Playwright Aria YAML 風テキストになり、MCP は raw JSON、CLI がフィルタ・整形を担う構成になった。 |

### 1.2 未完了

| 区分 | 内容 | 根拠 |
| --- | --- | --- |
| Phase 7 受入残 | 新 `.txt` snapshot を AI クライアントが読めるかの手動スモークが未完了。Claude Code / Codex CLI / VS Code Copilot 等が snapshot から ref を抽出し、`click` / `fill` を発行できることを確認する必要がある。 | [017_Phase7_完了.md](017_Phase7_完了.md) |
| 旧 Phase 7 安定化 | Phase 7 が snapshot 出力刷新へ再定義されたため、旧 Phase 7 安定化項目の一部は未消化のまま残っている。 | [003_実装計画.md](003_実装計画.md) / [008_要件再整理.md](008_要件再整理.md) / [010_Phase5_完了.md](010_Phase5_完了.md) |

---

## 2. 残タスク一覧

### 2.1 確定済み: Phase 7 受入残

| タスク | 内容 | 優先度 |
| --- | --- | --- |
| AI クライアント手動スモーク | 新 `.txt` snapshot を Phase 6 Skill 経由の AI クライアントが読み、ref を抽出して `click` / `fill` まで到達できることを確認する。 | 高 |

### 2.2 確定済み: 旧 Phase 7 安定化の未消化

| タスク | 内容 | 備考 |
| --- | --- | --- |
| `AttachQuery.Hwnd` / `--hwnd` | window handle 直接指定で attach できるようにする。 | `attach` の既存フラグ体系に追加する想定。 |
| モーダルダイアログ検出/追随 | モーダルダイアログを検出し、操作対象として自動追随できるようにする。 | Phase 7 完了メモでは `ModalDialogDetection` テスト修正のみ確認。全体完了は未確定のため継続追跡する。 |
| 画面ロック検知 | Windows デスクトップセッションがロックされている場合に、操作不能であることを明示的に返す。 | UIA 操作失敗の原因切り分けを改善する。 |
| 失敗時詳細ログ | click/fill/snapshot 失敗時に対象 ref、要素情報、例外情報を追えるようにする。 | AI / 人間双方の復旧判断を助ける。 |
| 構造化ログ / `--verbose` | stderr ログを運用しやすくし、CLI から詳細ログを有効化できるようにする。 | Phase 5 完了メモからの継続課題。 |
| 失敗時スクリーンショット | 操作失敗時に状態確認用スクリーンショットを保存する。 | `screenshot` コマンド実装後に統合するのが自然。 |
| snapshot 追加チューニング | 実利用で必要になった場合、重複属性省略・追加フィールド・長大値 truncate などを検討する。 | Phase 7 本体では `.txt` 化とサイズ削減まで完了。 |

### 2.3 確定済み: Phase 5/6 からの申し送り

| タスク | 内容 | 出所 |
| --- | --- | --- |
| 接続先環境変数 | `.adact/config.json` と `--server` に加え、環境変数で daemon 接続先を指定できるようにする。 | Phase 5 |
| 認証 / TLS / CORS | リモート daemon 運用時の保護方針を決め、必要最小限を実装する。 | Phase 5 |
| `--format` | CLI 出力の Markdown / JSON 等の切替要否を検討する。 | Phase 5 |
| `REF_NOT_FOUND` 時の再 snapshot 方針 | 古い ref / 消滅 ref に遭遇したとき、AI 側判断・CLI ヒント・自動再 snapshot のどこまで担うか決める。 | Phase 5 / Phase 6 |
| `.adact/config.json` 拡充 | 接続先以外の設定項目、個人設定とリポジトリ共有設定の分離、検索ルールを拡張する。 | Phase 5 |
| `KillAsync` の PID 再利用対策 | `Process.StartTime` 等を使い、意図しない別プロセス kill を防ぐ。 | Phase 5 |
| `CalculatorMutex` 共通化 | テストアセンブリ間で重複している Calculator mutex を共有テストヘルパへ集約する。 | Phase 5 |
| recipes | 電卓・メモ帳などの典型操作テンプレートを提供するか検討する。 | Phase 6 |
| MCP tool description 強化 | MCP 接続のみで AI クライアントが ADACT を理解できるよう、ツール description を強化する。 | Phase 6 |
| Skill 対象拡張 | Phase 8 以降で CLI/MCP サブコマンドを追加した場合、`adact-cli` Skill の `references/<cmd>.md` と同期テストを更新する。 | Phase 6 |

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

### 3.2 提案: 優先候補

| 機能 | 目的 | 優先理由 |
| --- | --- | --- |
| `launch` | アプリ起動。 | attach 前提の手作業を減らし、AI が開始から完了まで自律しやすくなる。 |
| `wait-for` | ウィンドウ・要素・状態の明示待機。 | GUI 操作の安定性に直結する。 |
| `press` | 特定要素またはアクティブ要素へのキー押下。 | click/fill だけでは足りない一般操作を補う。 |
| `type` | テキスト入力の逐次入力。 | fill では再現できない IME / キーストローク系の検証に必要。 |
| `screenshot` | 画面状態の画像保存。 | 診断、失敗時添付、将来の Vision/OCR 連携の基礎になる。 |
| `hover` | 要素 hover。 | ツールチップや hover メニューの操作に必要。 |
| `keyboard` | key down/up などの低レベルキーボード操作。 | 複合ショートカットや押下維持を扱う基盤。 |
| `mouse` | move/down/up などの低レベルマウス操作。 | UIA 操作だけでは届かないケースの escape hatch。 |

### 3.3 提案: 次点候補

| 機能 | 目的 | 採否判断 |
| --- | --- | --- |
| `select-option` | ComboBox / ListBox 等の選択操作。 | 主要アプリで需要が見えてから追加する。 |
| `get-value` | 要素の値取得。 | snapshot で足りない読み取り需要が出た場合に追加する。 |
| `evaluate` | UIA / アプリ固有操作の escape hatch。 | 安全性・API 境界を慎重に設計してから追加する。 |

### 3.4 提案: Phase 8 に組み込む可能性がある候補

| 候補 | 目的 | 採否判断 |
| --- | --- | --- |
| 動作確認用サンプルアプリ | Phase 8-A / 8-B の `wait-for`、`press`、`type`、`screenshot`、`hover`、`keyboard`、`mouse`、modal 追随などを再現性高く検証する。 | Phase 8 の受入で既存アプリだけでは不足する場合、Phase 8 と並行して作成する。 |

---

## 4. 推奨する Phase 8 分割案

### 4.1 提案

| 分割 | スコープ | 完了イメージ |
| --- | --- | --- |
| Phase 8-A | `launch` / `wait-for` / `press` / `type` など操作基盤 | AI がアプリ起動から基本操作までを CLI で一通り実行できる。 |
| Phase 8-B | `screenshot` / `hover` / `keyboard` / `mouse` など診断・低レベル操作 | 失敗時診断と、UIA 標準操作で届かない場面の escape hatch を提供する。 |
| Phase 8-C | `select-option` / `get-value` / `evaluate` など必要性を見て追加 | 実利用で不足が確認された読み取り・選択・拡張操作を追加する。 |

### 4.2 分割理由

| 観点 | 理由 |
| --- | --- |
| 受入のしやすさ | 操作基盤、診断・低レベル操作、追加的な高機能操作を分けることで、各 Phase の完了条件を明確にできる。 |
| Skill 同期 | サブコマンド追加ごとに Skill とテストを更新する必要があるため、一括追加より段階追加の方がレビューしやすい。 |
| 失敗時診断 | `screenshot` と失敗時スクリーンショット連携は Phase 8-B にまとめると責務が揃う。 |

---

## 5. Phase 9 以降候補

### 5.1 提案

| 候補 | 内容 | 備考 |
| --- | --- | --- |
| Dashboard | daemon / session / window / snapshot の状態を可視化する管理 UI。 | 運用・デバッグ向け。 |
| OCR / Vision | UIA が弱いアプリに対して画像認識・OCR を併用する。 | 初期スコープ外だった領域。 |
| 安定セレクタ生成 | ref だけでなく、再実行可能なセレクタを生成する。 | Codegen と相性がよい。 |
| Codegen | AI / 人間の操作をテストコードとして残す。 | 初期要件の将来機能。 |
| state 永続化 | daemon 再起動後も session / window / 設定を復元できるようにする。 | 現状は daemon メモリ内状態のみ。 |
| recipes | 電卓・メモ帳などの典型操作テンプレートを配布する。 | Skill とは別 Skill 化するか、`references/` 拡張で吸収するか要設計。 |
| cross-platform CLI client | GUI 操作を行わない CLI クライアントや遠隔操作端末を macOS / Linux でも起動できるようにする。 | `adact serve` は Windows only のまま、client / server の target framework と OS ガードを分離する。 |
| 配布・インストール導線 | `/path/to/adact.exe` を指定せず、`adact` コマンド名だけで起動できるようにする。 | .NET tool、自己完結配布、PATH 追加、installer などの方式を比較する。 |
| 検証用サンプルアプリ | ADACT の主要操作を確認する専用アプリを提供する。 | 既存アプリ依存のスモークを減らし、AI の既知情報に依存しない検証を行う。 |
| FlaUI テストコード生成 | AI が探索した操作を、FlaUI を用いた自動シナリオテストとして生成する。 | Codegen / recipes / Skill 拡張と合わせて設計する。 |

---

## 6. 決定事項と未決事項

### 6.1 確定済み

| 項目 | 決定 |
| --- | --- |
| Phase 5 の成果 | CLI 基本実装と lifecycle 系は完了済み。Phase 8 の新規対象に `close` / `detach` / `kill` / `close-all` / `daemon-stop` は含めない。 |
| Phase 6 の成果 | Skill 機構は完了済み。今後サブコマンドを追加・改名・削除した場合は Skill と同期テストを更新する。 |
| Phase 7 の成果 | snapshot 出力刷新は実装完了。`.txt` 形式と CLI 側フィルタ・整形の責務分担を前提にする。 |
| Phase 7 受入残 | AI クライアント手動スモークは未完了であり、Phase 7 完全受入前に確認する。 |
| Element Ref | `s<sid>e<eid>` 形式を継続する。generation 付き ref は旧 baseline のみに残る過去形式として扱う。 |

### 6.2 提案

| 項目 | 提案 |
| --- | --- |
| Phase 8-A | `launch` / `wait-for` / `press` / `type` を優先し、操作基盤を先に固める。 |
| Phase 8-B | `screenshot` / `hover` / `keyboard` / `mouse` をまとめ、診断と低レベル操作を追加する。 |
| Phase 8-C | `select-option` / `get-value` / `evaluate` は必要性を見て追加する。 |
| 安定化項目の扱い | `--hwnd`、画面ロック検知、詳細ログ、`--verbose` は Phase 8-A と並行または Phase 8-A 前の小 Phase として処理する。 |
| 失敗時スクリーンショット | `screenshot` 実装後に失敗時添付と統合する。 |
| cross-platform CLI client | `adact serve` の Windows only は維持しつつ、GUI を直接操作しない CLI クライアント・遠隔操作端末側は macOS / Linux 対応を検討する。 |
| 動作確認用サンプルアプリ | Phase 8 の操作追加と合わせて、既存アプリでは検証しづらい snapshot / wait / keyboard / mouse / modal を確認できる専用アプリを用意する。 |
| `adact` コマンド単体起動 | 配布・インストール導線を整備し、コマンド名だけで起動できる状態を目指す。 |
| FlaUI テストコード生成 | AI 探索からテストコード生成へつなげる Codegen / recipes / Skill 拡張を将来機能として整理する。 |

### 6.3 未決

| 項目 | 決めること |
| --- | --- |
| Phase 8 の正式スコープ | 8-A / 8-B / 8-C の分割を採用するか、別の単位にするか。 |
| `launch` の仕様 | 起動対象の指定方式、working directory、環境変数、既存プロセス検出、起動後 attach の扱い。 |
| `wait-for` の仕様 | 待機対象を window / element / text / value / disappearance のどこまで含めるか。 |
| `press` / `type` / `keyboard` の責務境界 | UIA ValuePattern / TextPattern と Win32 input injection の使い分け。 |
| `mouse` の安全性 | 座標指定を許す範囲、対象 window 外操作の扱い、リモート・DPI 環境での補正。 |
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

---

## 7. 次の着手順序案

### 7.1 提案

| 順序 | 内容 | 理由 |
| --- | --- | --- |
| 1 | Phase 7 AI クライアント手動スモーク | Phase 7 受入残を先に閉じる。 |
| 2 | Phase 8-A 要件定義・設計 | `launch` / `wait-for` / `press` / `type` の仕様境界を固める。 |
| 3 | 旧 Phase 7 安定化のうち `--hwnd` / 画面ロック検知 / `--verbose` を組み込む | 操作基盤追加前に診断性と attach 能力を底上げする。 |
| 4 | Phase 8-B 要件定義・設計 | `screenshot` と失敗時スクリーンショット連携を合わせて設計する。 |
| 5 | Phase 8-C / Phase 9+ の採否再判断 | 実利用で不足が見えた機能だけを追加する。 |
| 6 | 動作確認用サンプルアプリの要否判断 | Phase 8 の受入で既存アプリだけでは不足する検証項目を洗い出す。 |
| 7 | cross-platform CLI client / 配布導線の設計 | `adact serve` の Windows only を維持しながら、遠隔操作端末と `adact` コマンド単体起動の実現方式を決める。 |
| 8 | FlaUI テストコード生成の構想整理 | Codegen / recipes / Skill 拡張の責務境界を整理し、安定セレクタ生成との関係を決める。 |
