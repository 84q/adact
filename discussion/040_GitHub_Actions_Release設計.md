# 040: GitHub Actions / Release 設計

## 背景

将来の OSS 公開に向けて、ADACT の CI / Release 導線を最小構成で整えるための Requirement / Design 合意を整理する。

> **2026-05 更新**: 初版では `win-x64` のみを初期配布対象としていたが、Requirement 合意の更新により、Release 時の配布対象を Windows / Linux / macOS へ拡張する方針に見直した。既存の `ci.yml` / `release.yml` はすでに存在するが、本更新は multi-OS 配布と複数 job 構成への見直し合意を文書化するものであり、その workflow 反映実装はまだ行わない。

今回の主目的は **今すぐ public 化そのものを完了することではなく、公開を見据えて GitHub Actions と Release の設計方針を固めること** にある。README の公開導線更新は重要だが、実際に workflow と Release 導線を実装するタイミングでスコープに含める。

現行のテスト方針では、CI 常時実行候補は `Layer=Unit` と `Layer=Integration` であり、実アプリを扱う `IntegrationUia` / `Smoke` / `E2E` はローカル中心で扱う。この前提に合わせて GitHub Actions も段階的に導入する。

---

## この設計で決めること

- CI と Release の責務分担
- workflow の分割方針
- trigger の最小構成
- Release 時の配布対象と artifact 方針
- 手動実行 (`workflow_dispatch`) の位置づけ
- GitHub-hosted runner 利用時のコスト観点
- 将来拡張の余地

---

## この設計でまだ決めないこと

- 採用する action の細かな選定（例: Release 作成 action、archive / artifact 補助 action）
- workflow YAML の step 順序、条件分岐、outputs 定義などの具体記述
- `dotnet build` / `dotnet publish` の細かなコマンド引数、出力先パス、バージョン文字列組み立て方法
- artifact の受け渡し方法の実装詳細（upload/download の単位、命名、job 間の受け渡し方式）
- archive 作成スクリプトの具体実装（zip / tar.gz の生成手順、shell の使い分け）
- README の実際の更新内容
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
- 利用者に配る artifact は GitHub Release 上で各対象プラットフォーム向けに提供する
- Windows 向けは `Adact.Cli` を配布対象とし、`serve` を含める
- Windows の対象 RID は `win-x64` / `win-arm64` とする
- Linux 向けは `Adact.Cli.Client` を配布対象とし、`serve` は含めない
- Linux の対象 RID は `linux-x64` とする
- macOS 向けは `Adact.Cli.Client` を配布対象とし、`serve` は含めない
- macOS の対象 RID は `osx-arm64` のみとする
- 初回配布形式は self-contained のみに限定する
- 圧縮形式は Windows では `zip`、Linux / macOS では `tar.gz` を用いる
- Linux / macOS での達成条件は、`serve` なしの client として配布できることとする
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

- installer 導入も拡張候補として残す
- code signing も拡張候補として残す
- 既存リリースフローはできるだけ維持しつつ、配布対象の追加は最小限の差分で行う
- ただし初回実装では installer や code signing などの拡張候補を先取り実装しない

### Requirement フェーズ結論

- 現段階の目的は public 化の完了ではなく、**CI / Release 設計の最小合意を固めること** とする
- workflow は `ci.yml` / `release.yml` の 2 本に分ける
- CI は Windows runner で build と `Layer=Unit|Layer=Integration` のテストに限定する
- Release は `main` に反映済みかつ CI 想定品質確認を通したコミットへの `v*` tag push を正式導線とし、`workflow_dispatch` は正式 Release を作らない artifact 確認用とする
- Release artifact は GitHub Release で配布し、Windows では `Adact.Cli` (`serve` 含む) を `win-x64` / `win-arm64` 向けに、Linux / macOS では `Adact.Cli.Client` (`serve` なし) を `linux-x64` / `osx-arm64` 向けに提供する
- 初回配布形式は self-contained のみとし、圧縮形式は Windows では `zip`、Linux / macOS では `tar.gz` とする
- Release notes は GitHub 標準の auto-generated release notes を使う
- 現行 README には framework-dependent / self-contained の両配布や GitHub Releases 配布前提の旧記述が残りうるため、今回合意に合わせた同期更新が別途必要である
- README 更新は workflow / Release 導線を実装するタイミングでスコープに含める
- Linux / macOS での達成条件は `serve` なし client 配布の成立とし、installer と code signing は将来拡張として保持する

---

## Design フェーズ

Requirement フェーズの合意を前提に、workflow 間の責務分担と運用上の位置づけを Design として整理する。

### 今回更新した設計ポイント

- Release 配布対象を `win-x64` 単独から、`win-x64` / `win-arm64` / `linux-x64` / `osx-arm64` へ拡張した
- Windows は `Adact.Cli`、Linux / macOS は `Adact.Cli.Client` を配布対象に分けた
- 初回配布形式は self-contained のみに変更し、Linux / macOS の圧縮形式を `tar.gz` とした
- Windows の圧縮形式を `zip` と明示した
- 既存リリースフローは維持しつつ、Linux / macOS では `serve` なし client 配布を成立条件として扱う
- README は未更新であり、今回合意に合わせた同期更新が必要であることを明示した

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
- 既存リリースフローはできるだけ維持しつつ、配布対象の追加は `release.yml` の成果物生成対象を増やす方向で吸収する

### `release.yml` の構成方針

- `release.yml` 自体は維持し、その中で複数 job に分割する
- 基本構成は `package-win` / `package-linux` / `package-macos` / `create-release` とする
- `package-*` job はそれぞれの OS 向け publish・圧縮・workflow artifact 化までを担当する
- `create-release` job は各 `package-*` job の成果物を集約し、正式 Release 時のみ GitHub Release を作成する役割に限定する
- これにより、OS ごとの失敗箇所を job 単位で切り分けやすくし、将来の配布対象追加も `package-*` job の追加で拡張しやすくする

### runner 方針

- Release workflow の package job は各 OS ネイティブ runner を使う
- Windows 向けは Windows runner、Linux 向けは Linux runner、macOS 向けは macOS runner を割り当てる
- クロス OS publish を 1 台の runner に寄せず、各 OS の標準的なアーカイブ手段や実行環境差分をそのまま利用する
- `create-release` は成果物集約が主目的のため、特定 OS 依存を最小化した job として扱う

### project / build 対象の設計

- Windows 向け package job では `Adact.Cli` を publish 対象とし、`serve` を含む配布物を作る
- Linux / macOS 向け package job では `Adact.Cli.Client` を publish 対象とし、`serve` を含めない client 配布物を作る
- Windows は現行どおり solution 全体 build を前提にしてよい
- Linux / macOS では `adact.sln` 全体 build を前提にせず、対象 project を明示した build / publish を行う
- これにより、Windows 固有実装を含む project の影響を Linux / macOS package job へ波及させず、client 配布に必要な最小単位で成立性を確認できるようにする

### artifact 方針

- GitHub Release にはプラットフォーム別 artifact を掲載する
- Windows 向け:
  - 対象: `Adact.Cli`
  - 同梱範囲: `serve` を含む
  - 対象 RID: `win-x64` / `win-arm64`
  - 圧縮形式: `zip`
- Linux 向け:
  - 対象: `Adact.Cli.Client`
  - 同梱範囲: `serve` を含めない
  - 対象 RID: `linux-x64`
  - 圧縮形式: `tar.gz`
- macOS 向け:
  - 対象: `Adact.Cli.Client`
  - 同梱範囲: `serve` を含めない
  - 対象 RID: `osx-arm64`
  - 圧縮形式: `tar.gz`
- 初回配布形式は全プラットフォームで self-contained のみに限定する
- asset 名は self-contained 前提に簡略化し、`adact-{version}-{rid}.zip` または `adact-{version}-{rid}.tar.gz` を基本とする
- framework-dependent / self-contained を名前で併記する方式は今回採用しない
- Linux / macOS では「`serve` を含まない client 配布が成立すること」を完了条件として扱う

### CI 非対象の扱い

- `ci.yml` は今回は Windows のみの現状維持とする
- 今回の Design では、Linux / macOS 向け Release artifact を追加しても、常時実行のクロスプラットフォーム CI までは拡張しない
- したがって Linux / macOS runner の導入先は `release.yml` に限定し、`ci.yml` への multi-OS matrix 追加はスコープ外とする
- これは Release 配布導線の整備と、常時 CI の拡張を別判断に分離するためである

### GitHub Release の扱い

- Release 本体は GitHub Release を利用する
- release notes は手書きテンプレートを必須にせず、auto-generated release notes を採用する
- まずは配布導線の成立を優先し、本文の磨き込みコストを抑える

### README / ドキュメント連携方針

- README 更新は今回の設計合意に含めない
- ただし現行 README には framework-dependent / self-contained の両配布や GitHub Releases 配布前提の旧記述が残りうるため、本設計との不整合が一時的に存在しうる
- そのため、workflow / Release 導線の実装時には今回合意に合わせた README の同期更新を必須タスクとして扱う
- ただし実際に workflow / Release 導線を実装する際には、README の install / download 導線更新をスコープに含める
- `discussion/038_README要件定義.md` の「配布導線が整ったら README を更新する」という方針と整合させる

### コスト・運用観点

- 初期導入では、公開 OSS かつ標準 GitHub-hosted Windows runner 前提で始めるのが現実的である
- これにより self-hosted runner や特殊構成を前提にせずに導入できる
- ただし private repository 化、利用量増加、larger runner 採用などで前提は変わりうるため、その時点で別途確認する運用とする

### 将来拡張の残し方

- installer 追加により `adact` 単体起動導線を改善する余地を残す
- code signing は Windows 配布の信頼性向上施策として将来検討対象に残す
- framework-dependent 配布、追加 RID、installer、code signing などは将来拡張として扱う
- ただし Design フェーズでは、今回合意した配布対象以外を先取りして workflow 設計へ混ぜず、別フェーズの拡張として扱う

### Design フェーズ結論

- `ci.yml` は品質確認、`release.yml` は配布導線という責務分離を採用する
- `ci.yml` は `pull_request` / `push` on `main` / `workflow_dispatch` を対象にし、Windows runner で build と `Layer=Unit|Layer=Integration` を回す方針とする
- `release.yml` は `push` tags `v*` と `workflow_dispatch` を対象にするが、正式 Release は `main` 反映済みかつ CI 想定品質確認済みコミットへの `v*` tag push を正とする
- `workflow_dispatch` は正式な GitHub Release を作らず、workflow artifact 確認のための補助導線として扱う
- `release.yml` は単一 workflow のまま `package-win` / `package-linux` / `package-macos` / `create-release` の複数 job 構成へ拡張する
- package job は各 OS ネイティブ runner を使い、`create-release` は各 package job の成果物集約と Release 作成を担当する
- Windows は `Adact.Cli` を、Linux / macOS は `Adact.Cli.Client` を publish 対象とし、Linux / macOS では solution 全体ではなく project 指定 build / publish を採用する
- Release artifact は GitHub Release にプラットフォーム別で掲載し、Windows では `Adact.Cli` (`serve` 含む) を `win-x64` / `win-arm64` 向けに、Linux / macOS では `Adact.Cli.Client` (`serve` なし) を `linux-x64` / `osx-arm64` 向けに提供する
- 初回配布形式は self-contained のみに絞り、asset 名は `adact-{version}-{rid}.zip|tar.gz` ベースへ簡略化し、圧縮形式は Windows で `zip`、Linux / macOS で `tar.gz` を採用する
- GitHub Release では auto-generated release notes を用い、導入コストを抑える
- Linux / macOS 向け runner 導入は release workflow の範囲に留め、`ci.yml` へのクロスプラットフォーム CI 追加は今回スコープ外とする
- README は現時点では未同期の可能性があるため、実装時に今回合意へ合わせて更新する
- Linux / macOS は `serve` なし client 配布の成立をまず優先する
- 将来拡張は framework-dependent 配布、追加 RID、installer、code signing として別途育てる
