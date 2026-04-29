# Ref IDs

ADACT は Windows UIA 要素や window を短い ref ID で参照します。ref は daemon process 内の一時 ID であり、永続的な selector ではありません。

## ID 形式

| 種類 | 形式 | 例 | 所有者 | 用途 |
| --- | --- | --- | --- | --- |
| Window Ref | `w<n>` | `w1` | `WindowRefStore` | top-level window を attach 対象として参照する |
| Session ID | `s<n>` | `s1` | `SessionStore` | attached window session を参照する |
| Element Ref | `s<sid>e<eid>` | `s1e7` | `WindowSession` / `RefRegistry` | snapshot 内の UIA 要素を click/fill 対象として参照する |

過去の discussion や旧 snapshot baseline には generation 付き `s<sid>g<gen>e<eid>` 形式が残っていることがあります。現行実装では generation は廃止済みで、新規 snapshot は `s<sid>e<eid>` を使います。

## Window Ref (`w<n>`)

| 項目 | 内容 |
| --- | --- |
| 発行 | `windows_list_apps` / `adact list-apps` 実行時に top-level window へ割り当てる |
| 安定性 | 同じ `WindowKey` には同じ `w<n>` を再利用する |
| 失効 | window が list から消えると retired になり、`windows_attach(windowRef)` で解決できなくなる |
| 主用途 | window title や process name が曖昧な場合に、一覧から選んで attach する |

`WindowKey` は HWND、process id、process start time など window 同一性を表す情報を使います。window title が変わっても同じ window とみなせるよう、list のたびに最新情報へ同期します。

## Session ID (`s<n>`)

| 項目 | 内容 |
| --- | --- |
| 発行 | attach 成功時に `UiaEngine` が単調増加で採番する |
| 保持 | daemon process 内の `SessionStore` |
| active session | 最後に attach した session が active になる |
| 失効 | `detach` / `close` / `kill` / `close-all` / `daemon-stop`、または daemon process 終了 |
| 再利用 | 同じ daemon process 内では再利用しない |

`windows_snapshot`, `windows_detach`, `windows_close`, `windows_kill` は `sessionId` を省略すると active session を使います。active session がない場合は `NO_ACTIVE_SESSION` です。

## Element Ref (`s<sid>e<eid>`)

| 項目 | 内容 |
| --- | --- |
| 発行 | `WindowSession.SnapshotAsync` が UIA tree を走査して各要素へ割り当てる |
| prefix | `s<sid>` により session を一意に特定する |
| 安定化 | RuntimeId が取れる要素は StableKey として使い、snapshot をまたいで同じ `eid` を再利用する |
| fallback | RuntimeId が取れない場合は同 snapshot 内の出現順 fallback を使う |
| 失効 | session 削除、daemon 終了、または現 snapshot に対応要素が存在しない場合 |

Element Ref は「直近 snapshot で確認できる要素」を操作するための一時 ID です。RuntimeId による安定化により、同一画面での連続 click/fill では ref が維持されやすくなっています。ただし virtualized list や要素再生成では失効する可能性があります。

## generation 付き形式の扱い

| 形式 | 現在の扱い |
| --- | --- |
| `s<sid>g<gen>e<eid>` | 過去形式。旧 baseline や古い discussion の記述として残ることがある |
| `generation` field | 現行 MCP / CLI 出力では廃止済み |
| snapshot file name の `gen-N` | 現行出力では廃止済み |

古い `.json` baseline を Phase 7 の text formatter に通した場合、入力 JSON 内の古い ref がそのまま出ることがあります。新規取得した snapshot では generation なし形式を使います。

## ライフサイクル

| 操作 | `windowRef` | `sessionId` | `elementRef` |
| --- | --- | --- | --- |
| `list-apps` | 発行・同期 | 既存 session があれば表示されることがある | 変化なし |
| `attach` | session と関連付け | 発行または既存 session を返す | snapshot 取得時に発行 |
| `snapshot` | 変化なし | 維持 | 現 snapshot の要素集合を更新 |
| `click` / `fill` | 変化なし | 維持 | 操作後 snapshot で更新。RuntimeId が同じなら再利用 |
| `detach` | session 関連を解除 | 削除 | 失効 |
| `close` / `kill` | session 関連を解除 | 削除 | 失効 |
| `daemon-stop` | daemon 終了で全消滅 | daemon 終了で全消滅 | daemon 終了で全消滅 |

## 失効時の考え方

| 状況 | 代表エラー | 対処 |
| --- | --- | --- |
| `w<n>` が unknown / retired | `INVALID_WINDOW_REF` | `list-apps` を再実行し、最新の `windowRef` を使う |
| `s<n>` が存在しない | `INVALID_ARGUMENT` または `NO_ACTIVE_SESSION` | `attach` し直す |
| `s<sid>e<eid>` が malformed | `INVALID_REF_FORMAT` または `REF_NOT_FOUND` | snapshot の ref をそのまま使う |
| element が現 snapshot に存在しない | `REF_NOT_FOUND` | `snapshot` を再取得し、新しい ref を選ぶ |
| daemon が再起動した | 各種 not found / connection 状態リセット | `list-apps` からやり直す |

## 安定化の方針

ADACT の Element Ref は Playwright MCP の `_ariaRef` に近い考え方で、同じ要素には同じ短い ref を再利用することを目指しています。現行実装の StableKey は次の優先順です。

| 優先度 | StableKey | 備考 |
| ---: | --- | --- |
| 1 | UIA RuntimeId | 主要対象アプリで安定性が確認されている |
| 2 | 出現順 fallback | RuntimeId が取れない場合の最小保証。同 snapshot 内では一意 |

将来的には親 path、ControlType、AutomationId、Name、child index などを組み合わせた合成 key を検討します。

## 参照

| 文書 | 内容 |
| --- | --- |
| [../../discussion/011_ref安定化.md](../../discussion/011_ref安定化.md) | generation 廃止と RuntimeId ベース安定化の設計 |
| [snapshot.md](snapshot.md) | snapshot 内での ref 表示形式 |
| [mcp-tools.md](mcp-tools.md) | MCP tools の ref 引数 |
