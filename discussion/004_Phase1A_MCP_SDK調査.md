# Phase 1-A MCP SDK 採用判断調査

> 前提: [001\_要件定義.md](001_要件定義.md) / [002\_アーキテクチャ設計.md](002_アーキテクチャ設計.md) / [003\_実装計画.md](003_実装計画.md)
> 目的: C# / .NET で MCP サーバー / クライアントを実装するための SDK を比較し、ADACT の採用方針を確定する。
> 調査日: 2026-04-25

***

## 1. 調査した SDK 候補

| # | 候補 | 種別 | 概要 |
| --- | --- | --- | --- |
| 1 | **`ModelContextProtocol`（公式 C# SDK）** | NuGet（公式） | Anthropic と Microsoft の共同メンテによる公式 SDK。 |
| 2 | コミュニティ実装（`mcpdotnet` 系などの旧 SDK） | OSS | 公式化以前の先行 OSS。現在は公式 SDK にメンテナーが合流済み。 |
| 3 | 自前最小実装（JSON-RPC + stdio + ツールスキーマ手書き） | 自作 | 仕様書（[modelcontextprotocol.io/specification](https://modelcontextprotocol.io/specification/)）から JSON-RPC 2.0 と stdio / Streamable HTTP を直接実装。 |

***

## 2. 候補 1: 公式 C# SDK `ModelContextProtocol`

### 2.1 基本情報

| 項目 | 値 |
| --- | --- |
| パッケージ ID（メイン） | `ModelContextProtocol` |
| 関連パッケージ | `ModelContextProtocol.Core` / `ModelContextProtocol.AspNetCore` |
| 最新バージョン | **1.2.0**（リリース: 約 1 か月前 = 2026-03 頃） |
| リリース回数 | 35 リリース |
| ターゲット | .NET 8.0 / .NET Standard 2.0（互換性広い） |
| ライセンス | **Apache-2.0** |
| リポジトリ | <https://github.com/modelcontextprotocol/csharp-sdk> |
| Star / Fork | 4.2k / 681 |
| Contributor | 63 名（うち Microsoft 中の人 `stephentoub` `halter73` `eiriktsarpalis` `jeffhandley` などコア .NET チーム在籍） |
| ダウンロード総数 | 約 7.7M（NuGet 公式統計） |
| メンテナンス | 直近 2 週間以内のコミットあり、PR / Issue ともに活発 |

### 2.2 トランスポート対応

公式ドキュメント `concepts/transports/transports.html` で確認：

| トランスポート | サーバー | クライアント | 備考 |
| --- | --- | --- | --- |
| **stdio** | `WithStdioServerTransport()` | `StdioClientTransport` | 子プロセス起動・stdin/stdout 経由 |
| **Streamable HTTP** | `WithHttpTransport()` + `MapMcp()`（`ModelContextProtocol.AspNetCore`） | `HttpClientTransport(TransportMode = StreamableHttp)` | 推奨。Stateful / Stateless 両対応、セッション再開（`ResumeSessionAsync`）あり |
| SSE（レガシー） | `EnableLegacySse = true` | `HttpTransportMode.Sse` | 既定で無効（`MCP9004` Obsolete） |
| In-memory（任意 `Stream`） | `StreamServerTransport` | `StreamClientTransport` | テストやプロセス内同居に有用 |

→ **ADACT が必要とする stdio / Streamable HTTP の両方を一級サポート**。クライアントには `AutoDetect` モードもあり、Streamable HTTP を試して未対応なら SSE にフォールバックする実装が組み込み済み。

### 2.3 ツールスキーマの記述方法

2 系統が用意されている：

1. **属性ベース（推奨）**
   * `[McpServerToolType]` をクラスに、`[McpServerTool]` をメソッドに付与
   * 引数の `[Description]` 属性が JSON Schema の説明にマッピングされる
   * DI 経由で任意のサービスを引数として受け取れる（`MonkeyService monkeyService` のように）
   * `WithToolsFromAssembly()` でアセンブリ内を自動スキャン
2. **プログラム的登録**
   * `McpServerTool.Create((string message) => ..., new() { Name = "echo" })` で動的に作成可能
   * ツールセットの動的更新にも対応（`listChanged` capability）

→ ADACT のツール（`windows_snapshot` `windows_click` 等）はメソッド + 属性で素直に書ける。引数は ref（string） / value（string） 等、JSON Schema が自動生成される領域に収まる。

### 2.4 セッション・ステート保持と Ref レジストリの共存

* DI が Microsoft.Extensions.Hosting / DependencyInjection をベースにしているため、**Ref レジストリは `Singleton` または `Scoped` サービスとして登録**して `[McpServerTool]` メソッドの引数で受け取れる。
* snapshot 単位での破棄（既存 Ref をクリアして再発行）も、レジストリ内部で `windows_snapshot` 呼び出し時に内部 Dictionary をクリアするだけで実装可能。SDK 側に介入されない。
* HTTP の Stateful モードで `Mcp-Session-Id` を使う場合は、サーバー側で Per-Session のスコープを切れる（`AspNetCoreMcpPerSessionTools` サンプルあり）。ADACT のリモート構成（CLI 1 台 ↔ サーバー 1 台）では Stateless でも問題ないと見込まれる。
* 結論: **ADACT 独自要件と衝突しない**。

### 2.5 メンテナンス状況・成熟度

* v1.0 を超え v1.2.0 が出ており API は安定フェーズに入っている（プレリリースを脱した）。
* Microsoft DevBlogs ([build-a-model-context-protocol-mcp-server-in-csharp](https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/)) で公式に紹介され、`learn.microsoft.com/dotnet/ai/quickstarts/build-mcp-server` に project template が組み込まれている。
* AOT 対応（`Makefile` に AOT 互換性の publish test あり）。
* Issue 数 115 / PR 48 と活発。直近 2 週間以内に複数のコミット。

### 2.6 依存関係

* メインパッケージ `ModelContextProtocol` は `Microsoft.Extensions.*`（Hosting / DI / Logging）系に依存。.NET 8 アプリには標準的なセットで重くない。
* HTTP 公開する場合のみ `ModelContextProtocol.AspNetCore` を追加（ASP.NET Core 依存）。Phase 3（stdio のみ）の段階では引き込み不要。
* LLM 連携用に `Microsoft.Extensions.AI` の型（`AIFunction`）に統合されているが、サーバー側だけ作る場合は気にしなくてよい。

***

## 3. 候補 2: コミュニティ実装

* 公式化以前は `mcpdotnet`（PederHP 氏ら）など複数の C# 実装が GitHub に存在した。
* 公式 SDK のコントリビューター一覧に `PederHP` が含まれていることから、**主要コミュニティ実装は公式 SDK にマージ済み**と判断される。
* 結論として、現時点で「公式 SDK と独立に積極メンテされ、かつ公式 SDK が満たせない要件を満たすコミュニティ SDK」は確認できなかった。
* 仮に公式 SDK が将来停滞しても、Apache-2.0 なのでフォーク・自社メンテ可能。

***

## 4. 候補 3: 自前最小実装の見積り

ADACT で必要な最小機能セットを自作した場合のスコープ：

| 機能 | 概算実装ボリューム | 留意点 |
| --- | --- | --- |
| JSON-RPC 2.0（要求 / 応答 / 通知 / バッチ廃止） | 中 | id 管理、エラーオブジェクト、`stdin`/`stdout` でのフレーミング |
| stdio トランスポート（行指向ではなくメッセージ指向） | 中 | バイトストリームの分割、`Newtonsoft` 不使用での高速化 |
| MCP プロトコル（`initialize` / capability ネゴ / `tools/list` / `tools/call` / `notifications/*` / cancellation / progress / ping） | 大 | 仕様 [2025-11-25](https://modelcontextprotocol.io/specification/2025-11-25/) 全体の追従が必要 |
| ツール JSON Schema 生成（C# 型 → JSON Schema） | 中 | 属性ベース or 手書き。`System.Text.Json` ベースで実装可 |
| Streamable HTTP（Phase 4 必須） | **大** | POST 応答を SSE で開きっぱなし、`Mcp-Session-Id`、resume、backpressure。仕様準拠の実装が重い |
| LSP/MCP クライアント側（Phase 5 必須） | 大 | プロキシで CLI ↔ サーバーへ HTTP MCP クライアントとして繋ぐ必要 |
| エラー / キャンセル / タイムアウト / pings | 中 | プロトコル準拠 |
| 仕様変更追従 | 継続 | MCP 仕様は半年単位で更新（2024-11-05 → 2025-03-26 → 2025-06-18 → 2025-11-25 と既に複数バージョン） |

### 工数感（粗い見立て）

* **Phase 3（stdio + ツール）の最小自作**: 数週間規模で動くものは作れる。ただし Cancellation や notifications の取りこぼしバグが入りやすい。
* **Phase 4（Streamable HTTP）の自作**: SSE フレーミング＋セッション管理＋backpressure を仕様通りにやると、さらに数週間〜1 か月以上の追加コストが見込まれる。
* **継続コスト**: 仕様改版のたびに追従する必要があり、ADACT 本体（UIA エンジン / Snapshot ビルダー）の開発時間を侵食する。

→ ADACT のコア価値は Windows UIA の抽象化であり、**MCP プロトコル実装は本質的差別化要因ではない**。自作は明確に費用対効果が悪い。

***

## 5. 評価サマリー

| 観点 | 公式 SDK | 自前実装 |
| --- | --- | --- |
| stdio 対応 | ◎ | ○（自作しやすい） |
| Streamable HTTP 対応 | ◎（Stateful/Stateless 両対応・resume あり） | △（実装重い） |
| SSE フォールバック | ◎ | × |
| ツールスキーマ | ◎（属性 + DI、自動生成） | △（自作） |
| Ref レジストリとの共存 | ◎（DI で素直に同居） | ◎（自作なので自由） |
| メンテ状況 | ◎（公式 + Microsoft 共同） | △（自分で全部背負う） |
| 仕様改版追従 | ◎（SDK 更新で吸収） | ×（自前で追従） |
| ライセンス | ◎ Apache-2.0 | n/a |
| 依存の重さ | ○（Hosting/DI 系のみ。HTTP は別パッケージ） | ◎ |
| ADACT のスコープへの集中度 | ◎ | ×（プロトコル実装に時間を取られる） |

***

## 6. 推奨する採用方針

### 結論

**`ModelContextProtocol` 公式 C# SDK（および必要に応じて `ModelContextProtocol.AspNetCore`）を採用する。**

### 採用パッケージ構成（Phase ごと）

| Phase | 追加パッケージ |
| --- | --- |
| Phase 3（`adact local` stdio MCP サーバー） | `ModelContextProtocol` + `Microsoft.Extensions.Hosting` |
| Phase 4（`adact serve` Streamable HTTP MCP サーバー） | 上記 + `ModelContextProtocol.AspNetCore` |
| Phase 5（`adact` プロキシ = stdio サーバー + HTTP クライアント） | 上記。クライアント側は `HttpClientTransport(StreamableHttp)` を利用 |

### 推奨理由（要点）

1. **必要なトランスポート（stdio / Streamable HTTP）を 1 つの SDK で網羅**しており、Phase 3〜5 を通じて同じプログラミングモデルでいける。
2. **属性ベースのツール宣言と DI 連携**により、ADACT の Ref レジストリや Session Manager をサービスとして自然に共存させられる（独自要件と衝突しない）。
3. **Anthropic + Microsoft 共同メンテの公式実装**で、メンテ停滞リスクが現実的に最も低い候補。Apache-2.0 で OSS 化方針とも整合する。
4. **仕様改版追従コストを SDK に外出し**できるため、ADACT 開発リソースを UIA エンジン側に集中できる。
5. 自前実装の優位性（軽量性）は、ADACT で求められる機能範囲（initialize / tools / notifications / streamable HTTP のセッション管理など）の前で霧散する。

***

## 7. 懸念点・補足

| 項目 | 内容 | 対応方針 |
| --- | --- | --- |
| API の安定度 | v1.2.0 が出ているが、過去 35 リリースで API 変更履歴あり（プレリリース時代に「APIs may change」と明記）。 | 採用バージョンを `csproj` でピン止め。Phase 3 / 4 / 5 着手のたびにリリースノートを確認してアップデート判断。 |
| Stateless 推奨 | HTTP サーバーはドキュメントで `Stateless = true` を強く推奨。 | ADACT もまず Stateless で組み、必要があれば Stateful に切替（resume が必要になった場合など）。 |
| SSE のレガシー化 | SSE は backpressure 欠如のため obsolete 化されつつある。 | ADACT は最初から **Streamable HTTP のみ**で実装。SSE は将来も使わない。 |
| Microsoft.Extensions.Hosting への依存 | DI / Hosting が前提。 | Phase 3 着手時のソリューション構成で素直に採用すれば問題なし。CLI 起動時の生成器コストは無視できる。 |
| ASP.NET Core 依存（HTTP 時） | `adact serve` は ASP.NET Core を取り込む。 | サーバー側は WebApplication ベースで構成する設計に Phase 4 で切替。CLI 側（プロキシ・local モード）は ASP.NET Core を引き込まずに済む。 |
| .NET ターゲット | SDK は .NET 8 / .NET Standard 2.0 をサポート。ADACT は .NET 8 想定なので問題なし。 | — |
| バックアップ計画 | 公式 SDK が将来不活発になった場合。 | Apache-2.0 のフォーク。`ModelContextProtocol.Core` のみに依存する書き方をしておけば差し替えコストは限定的。 |

### 要件定義書の更新点（参考）

* [001\_要件定義.md](001_要件定義.md) §10「未決事項: MCP SDK の採用方針」→ **本書をもって `ModelContextProtocol` 公式 C# SDK 採用で確定**。
* [002\_アーキテクチャ設計.md](002_アーキテクチャ設計.md) §5「MCP SDK: 公式 C# SDK を第一候補、なければ最小自前実装（未決）」→ **公式 SDK で確定**に置き換え可能。

***

## 8. Phase 1-A 完了条件チェック

| 完了条件 | 達成状況 |
| --- | --- |
| stdio と Streamable HTTP（または SSE）両方を満たせるか確認 | ✅ 公式 SDK が両方一級対応 |
| ADACT 独自要件（Ref レジストリ等）と衝突しないか確認 | ✅ DI 経由で素直に共存可 |
| 継続メンテナンスの見込み | ✅ Anthropic + Microsoft 共同メンテ、活発 |
| 採用方針の決定 | ✅ `ModelContextProtocol` 公式 SDK 採用 |
