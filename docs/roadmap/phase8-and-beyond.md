# Phase 8 以降ロードマップ

この文書は [../../discussion/019_Phase8以降の残タスク整理.md](../../discussion/019_Phase8以降の残タスク整理.md) のストック版です。Phase 7 完了時点の現行実装を前提に、残タスクと候補を保守しやすい粒度で整理します。

## 現状サマリ

| Phase | 状態 | 内容 |
| --- | --- | --- |
| Phase 5 | 完了 | CLI 基本実装。`list-windows` / `attach` / `snapshot` / `click` / `fill` と lifecycle 系 (`detach` / `close-window` / `kill` / `close-all` / `daemon-stop`) を実装済み |
| Phase 5 post-task | 完了 | Element Ref 安定化。generation なし `s<sid>e<eid>` 形式へ移行済み |
| Phase 6 | 完了 | `adact install --skills` による Skill 機構を実装済み |
| Phase 7 | 実装完了 / 受入一部残 | CLI snapshot `.txt` 化、MCP raw JSON 化、CLI 側 filter/formatter への責務移譲が完了 |

現行の単一 `adact.exe` / `src/Adact.Cli` は Windows target です。`serve` / `local` は UIA を使うため対象 GUI と同じ対話 Windows セッション側で動く必要があり、この制約は cross-platform CLI client 検討後も維持します。

## 旧 Phase 7 安定化の残タスク

| タスク | 内容 | 備考 |
| --- | --- | --- |
| モーダルダイアログ追随 | modal dialog を検出し、操作対象として自然に追随する | 現行は snapshot への modal node 注入と一部テストがある |
| 画面ロック検知 | lock 状態など操作不能な desktop 状態を明示する | 起動時非対話判定とは別の動的検知 |
| 失敗時詳細ログ | click/fill/snapshot 失敗時に ref、要素情報、例外を追えるようにする | AI / 人間の復旧判断を助ける |
| 構造化ログ / `--verbose` | CLI / daemon の診断ログを運用しやすくする | `local --verbose` は存在するが全体整備は未完 |
| 失敗時スクリーンショット | 操作失敗時に状態確認用 screenshot を保存する | `screenshot` 実装後の統合が自然 |
| snapshot 追加チューニング | 重複属性省略、追加 field、長大値 truncate など | 実利用で必要になった場合に検討 |

## Phase 5 / 6 からの申し送り

| タスク | 内容 | 出所 |
| --- | --- | --- |
| 認証 / TLS / CORS | remote daemon 運用時の保護方針を決める | Phase 5 |
| `REF_NOT_FOUND` 時の再 snapshot 方針 | AI 側判断、CLI hint、自動再 snapshot の分担を決める | Phase 5 / 6 |
| `.adact/config.json` 拡充 | 接続先以外の設定、個人設定と repo 共有設定の分離、探索 rule の拡張 | Phase 5 |
| `KillAsync` の PID 再利用対策 | `Process.StartTime` 等で意図しない別 process kill を防ぐ | Phase 5 |
| recipes | Calculator / Notepad など典型操作テンプレートを提供する | Phase 6 |
| Skill 対象拡張 | CLI/MCP サブコマンド追加時に `adact-cli` Skill と同期テストを更新する | Phase 6 |

### Phase 8-A: 操作基盤

| 機能 | 目的 |
| --- | --- |
| `launch` | アプリ起動を CLI から扱い、attach 前の手作業を減らす |
| `wait-for-element` | window / element / state の明示待機で GUI 操作を安定化する |
| `keypress` | 特定要素または active element へのキー押下を扱う |
| `type` | fill では再現できない逐次入力や IME / keystroke 系検証を扱う |

### Phase 8-B: 診断・低レベル操作

| 機能 | 目的 |
| --- | --- |
| `screenshot` | 診断、失敗時添付、将来の Vision/OCR 連携の基礎にする |
| `hover` | tooltip や hover menu を扱う |
| `keyboard` | key down/up、複合 shortcut、押下維持を扱う |
| `mouse` | move/down/up など UIA 操作で届かない場面の escape hatch にする |

### Phase 8-C: 必要性を見て追加

| 機能 | 目的 | 採否判断 |
| --- | --- | --- |
| `select-option` | ComboBox / ListBox 等の選択操作 | 主要アプリで需要が見えてから |
| `get-value` | 要素値の明示取得 | snapshot で足りない読み取り需要が出た場合 |
| `evaluate` | UIA / アプリ固有操作の escape hatch | 安全性と API 境界を慎重に設計してから |

## Phase 9+ 候補

| 候補 | 内容 |
| --- | --- |
| Dashboard | daemon / session / window / snapshot の状態を可視化する管理 UI |
| OCR / Vision | UIA が弱いアプリに画像認識や OCR を併用する |
| 安定セレクタ生成 | 一時 ref ではなく再実行可能な selector を生成する |
| Codegen | AI / 人間の操作をテストコードとして残す |
| state 永続化 | daemon 再起動後も session / window / 設定を復元する |
| recipes | Calculator / Notepad など典型操作テンプレートを配布する |
| cross-platform CLI client | `adact serve` は Windows GUI セッション側のまま、GUI を直接操作しない CLI client 部分を分離・multi-target 化し、macOS / Linux の remote terminal でも起動可能にする |
| 検証用サンプルアプリ | snapshot / wait / keyboard / mouse / modal などを再現性高く検証できる専用アプリを用意する |
| `adact` 単体起動の配布導線 | `/path/to/adact.exe` ではなく `adact` だけで起動できる installer / .NET tool / PATH 導線を整える |
| FlaUI テストコード生成 | AI が探索した操作を FlaUI + xUnit などの自動シナリオテストへ生成する |

## 追加 4 件の位置づけ

| 候補 | 推奨位置 | 理由 |
| --- | --- | --- |
| cross-platform CLI client | Phase 9+ | Windows UIA 実体と GUI 非依存 client の project / target framework 分離、packaging、OS guard の設計が必要 |
| 検証用サンプルアプリ | Phase 8 または Phase 9+ | Phase 8 の受入で既存アプリだけでは不足する場合は並行着手が有効 |
| `adact` 単体起動の配布導線 | Phase 9+ | 機能 API より配布戦略の判断が主になる |
| FlaUI テストコード生成 | Phase 9+ | Codegen、recipes、安定 selector、Skill 拡張との関係整理が必要 |

## 次の着手順序案

| 順序 | 内容 | 理由 |
| ---: | --- | --- |
| 1 | Phase 7 AI クライアント手動スモーク | `.txt` snapshot 形式の実利用受入を閉じる |
| 2 | Phase 8-A 要件定義・設計 | `launch` / `wait-for-element` / `keypress` / `type` の責務境界を固める |
| 3 | `--hwnd` / 画面ロック検知 / `--verbose` | 操作基盤追加前に attach 能力と診断性を上げる |
| 4 | Phase 8-B 要件定義・設計 | `screenshot` と失敗時添付を合わせて設計する |
| 5 | Phase 8-C / Phase 9+ 再判断 | 実利用で不足が見えたものだけを追加する |
| 6 | 検証用サンプルアプリの要否判断 | 既存アプリ依存の smoke で不足する検証を補う |
| 7 | cross-platform CLI client / 配布導線設計 | Windows GUI セッション側 daemon と remote terminal 側 CLI client の分離を検討する |
| 8 | FlaUI テストコード生成構想 | Codegen / recipes / Skill 拡張の責務境界を整理する |

## 未決事項

| 項目 | 決めること |
| --- | --- |
| Phase 8 正式スコープ | 8-A / 8-B / 8-C の分割を採用するか |
| `launch` | 起動対象、working directory、env、起動後 attach の扱い |
| `wait-for-element` | window / element / text / value / disappearance の範囲 |
| `keypress` / `type` / `keyboard` | UIA pattern と Win32 input injection の使い分け |
| `mouse` | 座標指定、対象 window 外操作、DPI 補正、安全性 |
| `evaluate` | 汎用 escape hatch として採用するか |
| 認証 / TLS / CORS | remote daemon をどの時点で本格サポート扱いにするか |
| recipes | `adact-cli` Skill に含めるか、別 Skill にするか |
| cross-platform CLI client | GUI を直接操作しない client の範囲、project 分割、multi-target 方針 |
| 配布方式 | .NET tool、自己完結 binary、installer、PATH 追加など |
| サンプルアプリ | 技術選定と検証項目 |
| FlaUI テストコード生成 | 出力形式と AI 探索との責務境界 |
