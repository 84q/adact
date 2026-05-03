# 040: GitHub Actions / Release 設計

## 背景

将来の OSS 公開に向けて、ADACT の CI / Release 導線を最小構成で整えるための Requirement / Design 合意を整理する。

今回の主目的は **今すぐ public 化そのものを完了することではなく、公開を見据えて GitHub Actions と Release の設計方針を固めること** にある。README の公開導線更新は重要だが、実際に workflow と Release 導線を実装するタイミングでスコープに含める。

現行のテスト方針では、CI 常時実行候補は `Layer=Unit` と `Layer=Integration` であり、実アプリを扱う `IntegrationUia` / `Smoke` / `E2E` はローカル中心で扱う。この前提に合わせて GitHub Actions も段階的に導入する。

---

## この設計で決めること

- CI と Release の責務分担
- workflow の分割方針
- trigger の最小構成
- 初期配布対象と artifact 方針
- 手動実行 (`workflow_dispatch`) の位置づけ
- GitHub-hosted runner 利用時のコスト観点
- 将来拡張の余地

---

## この設計でまだ決めないこと

- workflow YAML の具体的な job / step 記述
- README の実際の更新内容
- 複数 OS 向け配布の詳細実装
- installer、code signing、配布チャネル追加の具体設計
- private repository や larger runner 利用時の詳細コスト試算

設計文書では方針レベルに留め、実装詳細は後続タスクで詰める。

---

## Requirement フェーズ

### 目的

- 主目的は public 化の実施ではなく、**CI / Release 設計を整えること** とする
- OSS 公開を見据え、最低限の品質確認と配布導線の土台を先に整える
- ただし初期段階では、複雑な多環境対応や署名・インストーラ整備までは含めない

### CI に求める役割

- pull request と `main` への反映時に、壊れていないことを機械的に確認する
- 常時実行する対象は Windows runner 上の build と高速テストに限定する
- テストは現行方針どおり `dotnet test --filter "Layer=Unit|Layer=Integration"` を実行対象とする
- 実 GUI 依存の `IntegrationUia` / `Smoke` / `E2E` は CI の初期スコープに含めない

### Release に求める役割

- タグ起点で再現可能な配布物を生成し、GitHub Release へ載せる
- 正式 Release 用の `v*` タグは、少なくとも `main` に反映済みで、CI 想定の build と `dotnet test --filter "Layer=Unit|Layer=Integration"` による品質確認を通したコミットに対して付与する前提とする
- 初期配布対象は `win-x64` のみとする
- 利用者に配る artifact は次の 2 種類とする
  - framework-dependent ZIP
  - self-contained ZIP
- Release notes は GitHub Release の auto-generated release notes を使う

### workflow 分割要件

- workflow は **`ci.yml` と `release.yml` の 2 分割** とする
- `ci.yml` は品質確認に責務を限定する
- `release.yml` は配布物生成と Release 導線に責務を限定する
- これにより、通常開発時の CI と配布作業の判断軸を分離する

### trigger 要件

#### `ci.yml`

- `pull_request`
- `push` on `main`
- `workflow_dispatch`

#### `release.yml`

- `push` tags `v*`
- `workflow_dispatch`

### 手動実行の位置づけ

- `workflow_dispatch` は両 workflow に用意してよい
- ただし Release 導線において正式な Release 作成の正とするのは **`v*` tag push** とする
- `release.yml` の `workflow_dispatch` は、本番公開の代替ではなく **artifact 確認用** の位置づけに留める
- `workflow_dispatch` 実行では正式な GitHub Release は作成しない前提とし、必要に応じて workflow artifact のみを確認対象とする

### コスト観点の要件

- 公開 OSS として標準の GitHub-hosted Windows runner を使う限り、初期導入は始めやすいという一般論を明記してよい
- 一方で、private repository 化や larger runner 利用時は、利用条件や課金条件を別途確認すべきである
- したがって初期設計では、無料または始めやすい標準導線を前提にしつつ、運用条件の変化時に再確認が必要であることを残す

### 将来拡張要件

- 将来は mac / linux 向け CLI 配布を拡張候補として残す
- installer 導入も拡張候補として残す
- code signing も拡張候補として残す
- ただし初回実装は `win-x64` ZIP 配布に集中し、拡張候補を先取り実装しない

### Requirement フェーズ結論

- 現段階の目的は public 化の完了ではなく、**CI / Release 設計の最小合意を固めること** とする
- workflow は `ci.yml` / `release.yml` の 2 本に分ける
- CI は Windows runner で build と `Layer=Unit|Layer=Integration` のテストに限定する
- Release は `main` に反映済みかつ CI 想定品質確認を通したコミットへの `v*` tag push を正式導線とし、`workflow_dispatch` は正式 Release を作らない artifact 確認用とする
- 初期配布対象は `win-x64` のみ、artifact は framework-dependent ZIP と self-contained ZIP の 2 種類とする
- Release notes は GitHub 標準の auto-generated release notes を使う
- README 更新は workflow / Release 導線を実装するタイミングでスコープに含める
- mac / linux 配布、installer、code signing は将来拡張として保持する

---

## Design フェーズ

Requirement フェーズの合意を前提に、workflow 間の責務分担と運用上の位置づけを Design として整理する。

### workflow 分割方針

#### `ci.yml` の責務

- 開発中の変更に対する継続的な品質確認を担う
- PR レビュー前後で「少なくとも build と高速テストは通る」状態を確認する
- Release 作成や配布物公開の責務は持たせない

#### `release.yml` の責務

- 配布向けビルド成果物を生成する
- GitHub Release に載せる artifact 導線を担う
- 正式版公開フローはタグ起点とし、日常的な CI とは分離する

この 2 分割により、通常開発と配布タイミングで求める判断・失敗時の切り分けを単純化する。

### CI 設計方針

- runner は Windows を前提とする
- 目的は「ADACT の現行実装が Windows 前提で build 可能であり、高速テストが通ること」の確認に置く
- テスト対象は `Layer=Unit|Layer=Integration` に限定し、現行の testing 方針と揃える
- GUI 実アプリ依存のテストは flaky 要因や runner 制約を増やすため、初期 CI には含めない

### Release 設計方針

- 正式リリースは `v*` 形式タグ push を単一の正規入口とする
- 正式 Release 用タグは、少なくとも `main` に反映済みで、`ci.yml` 想定の品質確認対象である build と `Layer=Unit|Layer=Integration` を通したコミットへ付ける前提とする
- `workflow_dispatch` による実行は、配布物の中身や job の成立性を事前確認するための補助導線とする
- 手動実行だけで正式 Release が作られる設計には寄せず、公開の基準をタグに固定する
- `workflow_dispatch` 実行では GitHub Release を新規作成せず、必要に応じて workflow artifact の確認に留める方針とする

### artifact 方針

- 初期配布対象は `win-x64` に固定する
- 配布形式は ZIP に統一し、利用者に次の 2 系統を提供する
  - framework-dependent ZIP: .NET ランタイム前提で軽量に配布するための形式
  - self-contained ZIP: ランタイム同梱で導入障壁を下げるための形式
- この 2 種類を並行提供することで、サイズと導入容易性の異なる需要を最小構成で両立する

### GitHub Release の扱い

- Release 本体は GitHub Release を利用する
- release notes は手書きテンプレートを必須にせず、auto-generated release notes を採用する
- まずは配布導線の成立を優先し、本文の磨き込みコストを抑える

### README / ドキュメント連携方針

- README 更新は今回の設計合意に含めない
- ただし実際に workflow / Release 導線を実装する際には、README の install / download 導線更新をスコープに含める
- `discussion/038_README要件定義.md` の「配布導線が整ったら README を更新する」という方針と整合させる

### コスト・運用観点

- 初期導入では、公開 OSS かつ標準 GitHub-hosted Windows runner 前提で始めるのが現実的である
- これにより self-hosted runner や特殊構成を前提にせずに導入できる
- ただし private repository 化、利用量増加、larger runner 採用などで前提は変わりうるため、その時点で別途確認する運用とする

### 将来拡張の残し方

- cross-platform CLI client の配布導線整備に合わせて mac / linux 向け artifact を将来追加しうる
- installer 追加により `adact` 単体起動導線を改善する余地を残す
- code signing は Windows 配布の信頼性向上施策として将来検討対象に残す
- ただし Design フェーズでは、これらを初回 workflow 設計へ混ぜず、別フェーズの拡張として扱う

### Design フェーズ結論

- `ci.yml` は品質確認、`release.yml` は配布導線という責務分離を採用する
- `ci.yml` は `pull_request` / `push` on `main` / `workflow_dispatch` を対象にし、Windows runner で build と `Layer=Unit|Layer=Integration` を回す方針とする
- `release.yml` は `push` tags `v*` と `workflow_dispatch` を対象にするが、正式 Release は `main` 反映済みかつ CI 想定品質確認済みコミットへの `v*` tag push を正とする
- `workflow_dispatch` は正式な GitHub Release を作らず、workflow artifact 確認のための補助導線として扱う
- 初期 artifact は `win-x64` 向け framework-dependent ZIP と self-contained ZIP の 2 種類に絞る
- GitHub Release では auto-generated release notes を用い、導入コストを抑える
- README 更新は実装時に合わせて扱い、将来拡張は mac / linux 配布、installer、code signing として別途育てる
