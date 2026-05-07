# ADACT ドキュメント

ADACT (AI-driven Desktop Application CLI Tools) は、AI エージェントと人間が同じ `adact <subcommand>` CLI を使って Windows デスクトップアプリを読み取り、操作し、将来的にテストへ残すためのツール群です。Playwright MCP / Playwright Agent CLI の「snapshot と ref による構造的操作」を Windows UI Automation (UIA) に持ち込み、ブラウザではなく WPF / WinForms / UWP / Win32 などのデスクトップアプリを対象にします。

現在の主インターフェースは MCP 直接利用ではなく `adact <subcommand>` CLI です。CLI は `adact serve` で起動した HTTP MCP daemon に接続し、daemon が UIA 経由で対象 Windows アプリを操作します。

設計説明は図から読み始められるようにしています。全体構成は [architecture/overview.md](architecture/overview.md)、クラス間の関係は [architecture/class-responsibilities.md](architecture/class-responsibilities.md)、操作時の時系列は [architecture/command-flows.md](architecture/command-flows.md)、snapshot/ref の流れは [architecture/snapshot-pipeline.md](architecture/snapshot-pipeline.md) を参照してください。

## サイトマップ

| カテゴリ | 文書 | 内容 |
| --- | --- | --- |
| アーキテクチャ | [architecture/overview.md](architecture/overview.md) | 全体像、コンポーネント間の関係、主インターフェース |
| アーキテクチャ | [architecture/runtime-modes.md](architecture/runtime-modes.md) | `adact <sub>` / `adact serve` の違い |
| アーキテクチャ | [architecture/components.md](architecture/components.md) | 各プロジェクトと主要クラスの責務 |
| アーキテクチャ | [architecture/class-responsibilities.md](architecture/class-responsibilities.md) | 層別の主要クラス責務、保持する状態、呼び出し先、依存方向 |
| アーキテクチャ | [architecture/command-flows.md](architecture/command-flows.md) | CLI subcommand から MCP tool、Store、Engine、CLI 出力までの処理フロー |
| アーキテクチャ | [architecture/snapshot-pipeline.md](architecture/snapshot-pipeline.md) | raw JSON 生成、ref 登録、CLI `.txt` snapshot 変換の詳細設計 |
| 仕様 | [spec/cli.md](spec/cli.md) | CLI サブコマンド、共通フラグ、出力形式 |
| 仕様 | [spec/mcp-tools.md](spec/mcp-tools.md) | MCP tools (`windows_*`, `adact_daemon_stop`) の仕様 |
| 仕様 | [spec/ref-ids.md](spec/ref-ids.md) | `windowRef` / `sessionId` / `elementRef` の形式とライフサイクル |
| 仕様 | [spec/snapshot.md](spec/snapshot.md) | Engine/MCP raw JSON と CLI `.txt` snapshot の責務分担 |
| 仕様 | [spec/errors-and-output.md](spec/errors-and-output.md) | exit code、stderr、stdout、MCP error の規約 |
| 開発 | [development/testing.md](development/testing.md) | テスト構成、Layer Trait、実行コマンド、実アプリ E2E 注意点 |
| 開発 | [development/troubleshooting.md](development/troubleshooting.md) | 代表的な失敗と復旧手順 |
| ロードマップ | [roadmap/phase8-and-beyond.md](roadmap/phase8-and-beyond.md) | Phase 8 以降の残タスクと候補 |

## 読み始めガイド

| 読者 | 最初に読む文書 | 次に読む文書 |
| --- | --- | --- |
| ADACT を使って Windows アプリを操作したい人 | [spec/cli.md](spec/cli.md) | [development/troubleshooting.md](development/troubleshooting.md) |
| AI クライアントや MCP 連携を理解したい人 | [architecture/overview.md](architecture/overview.md) | [spec/mcp-tools.md](spec/mcp-tools.md) |
| 実装に入る開発者 | [architecture/components.md](architecture/components.md) | [architecture/class-responsibilities.md](architecture/class-responsibilities.md)、[architecture/command-flows.md](architecture/command-flows.md)、[development/testing.md](development/testing.md) |
| snapshot / ref まわりを直す人 | [architecture/snapshot-pipeline.md](architecture/snapshot-pipeline.md) | [spec/ref-ids.md](spec/ref-ids.md)、[spec/snapshot.md](spec/snapshot.md) |
| 次フェーズの設計をする人 | [roadmap/phase8-and-beyond.md](roadmap/phase8-and-beyond.md) | [../discussion/019_Phase8以降の残タスク整理.md](../discussion/019_Phase8以降の残タスク整理.md) |

## discussion/ との関係

`discussion/` は検討過程、設計判断、完了メモを残す場所です。`docs/` はそこから現行実装に合う安定情報だけを抜き出したストック情報です。古い discussion と現行実装に差分がある場合、この docs では現行実装を優先します。

主な参照元:

| 文書 | 位置づけ |
| --- | --- |
| [../discussion/001_要件定義.md](../discussion/001_要件定義.md) | 初期要件と成功条件 |
| [../discussion/008_要件再整理.md](../discussion/008_要件再整理.md) | CLI 主インターフェース方針の再整理 |
| [../discussion/010_Phase5_完了.md](../discussion/010_Phase5_完了.md) | CLI client / HTTP daemon / lifecycle 実装の完了記録 |
| [../discussion/011_ref安定化.md](../discussion/011_ref安定化.md) | generation なし Element Ref への移行 |
| [../discussion/014_Phase6_完了.md](../discussion/014_Phase6_完了.md) | `adact install --skills` の完了記録 |
| [../discussion/017_Phase7_完了.md](../discussion/017_Phase7_完了.md) | CLI snapshot `.txt` 化と責務分担の完了記録 |
| [../discussion/018_対話セッション判定.md](../discussion/018_対話セッション判定.md) | `NO_INTERACTIVE_SESSION` と exit 4 の設計 |
