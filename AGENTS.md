# AGENTS

## Shell / Sandbox Notes

This repository has shown a Windows Codex shell sandbox issue where normal
`shell_command` calls can hang before the requested PowerShell command starts.
The symptom is that even trivial commands such as `Write-Output 'ping'` produce
no output and do not honor the requested short timeout.

Observed cause range:

- The hang occurs in the sandboxed shell preflight path, before the real command
  process is spawned.
- The long-lived PowerShell AST parser used by the command-safety layer can be
  involved.
- Killing the parser process may not recover the current Codex session.
- Escalated shell execution has worked normally in the same workspace.

Operational rules for agents:

- Avoid `multi_tool_use.parallel` for `shell_command` in this workspace.
- Prefer one shell command at a time.
- If a sandboxed shell command hangs or fails to start, do not retry the same
  sandboxed path repeatedly.
- Use `sandbox_permissions: "require_escalated"` for necessary shell commands
  after explaining that this avoids the known sandbox preflight hang.
- Keep `login: false` for PowerShell commands unless profile behavior is
  explicitly needed.
- Do not treat a no-output hang as evidence that the target command is slow; it
  may not have started at all.

When the Codex / VS Code extension host is restarted and sandboxed shell
execution is verified to work again, these rules can be relaxed, but parallel
shell execution should still be reintroduced cautiously.

## Multi-Agent Operating Model

このリポジトリでは、実装タスクを次の 3 役に分けて進める。

- **司令塔**: ユーザとの会話、要件整理、作業分解、実装担当・レビュー担当への委任、最終報告を担当する。
- **実装担当**: 司令塔から渡された範囲だけを実装・修正する。レビュー担当は呼ばない。
- **レビュー担当**: 変更差分を調査し、必要なテストを実行し、バグ・リグレッション・テスト不足・docs 不整合などを指摘する。原則としてファイル編集は行わない。

### 常時適用する基本ルール

- 司令塔はユーザとの対話を一元管理する。
- 司令塔は、実装や docs 更新を始める前に `.agents/skills/review-loop/SKILL.md` を参照する。
- 軽微な変更は、司令塔が直接実装してよい。ただしレビューと検証の扱いは省略しない。
- 司令塔は、役割の詳細が必要な場合に次の skill を参照する。
  - `.agents/skills/orchestrator/SKILL.md`
  - `.agents/skills/implementation/SKILL.md`
  - `.agents/skills/reviewer/SKILL.md`
- 実装担当には、担当範囲・参照資料・変更可能なファイル・完了条件を明示してから委任する。
- レビュー担当には、レビュー対象ファイル・レビュー観点・実行すべきテスト・「編集不要、調査と報告のみ」を明示してから委任する。
- docs 更新は実装完了条件に含める。コード・仕様・操作・設定・テスト方針に影響する変更では、関連 docs も同じループで更新・レビューする。
- 司令塔はレビュー指摘がゼロになるまで、または `.agents/skills/review-loop/SKILL.md` の停止条件に達するまで、実装とレビューのループを管理する。

### 未決定事項

以下はユーザへのヒアリングで決めていく。

- レビュー担当のレビュー粒度を、毎回フルレビューにするか、差分中心にするか。
- ループ停止回数を既定の 10 回から変更するか。
