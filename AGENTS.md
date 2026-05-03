# AGENTS

## Multi-Agent Operating Model

このリポジトリでは、実装タスクを次の 4 役に分けて進める。

- **Orchestrator**: ユーザとの会話、要件整理、作業分解、Implementation / Reviewer / Research への委任、最終報告を担当する。自分で実装コードを書かず、勝手に判断しない。
- **Implementation**: Orchestrator から渡された範囲だけを実装・修正する。Reviewer は呼ばない。
- **Reviewer**: 変更差分を調査し、必要なテストを実行し、バグ・リグレッション・テスト不足・docs 不整合などを指摘する。原則としてファイル編集は行わない。
- **Research**: コードベース調査と Web 調査を統合し、Orchestrator から委任された調査対象を構造化された事実として報告する。ファイル編集や実装コードの生成は行わない。

### 常時適用する基本ルール

- Orchestrator はユーザとの対話を一元管理する。
- Orchestrator は、実装や docs 更新を始める前に `.agents/skills/review-loop/SKILL.md` を参照する。
- Orchestrator は自分で実装コードを書かない。実装は Implementation エージェントに委任する。
- Orchestrator は、役割の詳細が必要な場合に次のエージェント定義を参照する。
  - `.opencode/agents/orchestrator.md`
  - `.opencode/agents/implementation.md`
  - `.opencode/agents/reviewer.md`
  - `.opencode/agents/research.md`
- Implementation には、担当範囲・参照資料・変更可能なファイル・完了条件を明示してから委任する。
- Reviewer には、レビュー対象ファイル・レビュー観点・実行すべきテスト・「編集不要、調査と報告のみ」を明示してから委任する。
- Research には、調査対象・観点・範囲を明示してから委任する。並列調査が可能な場合は複数を同時に起動する。
- docs 更新は実装完了条件に含める。コード・仕様・操作・設定・テスト方針に影響する変更では、関連 docs も同じループで更新・レビューする。
- Orchestrator はレビュー指摘がゼロになるまで、または `.agents/skills/review-loop/SKILL.md` の停止条件に達するまで、実装とレビューのループを管理する。
