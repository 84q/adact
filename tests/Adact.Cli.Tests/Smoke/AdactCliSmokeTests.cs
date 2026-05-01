using Xunit;

namespace Adact.Cli.Tests.Smoke;

/// <summary>
/// L4 Smoke: daemon サブプロセス + adact CLI のラウンドトリップ最低限確認。
/// 実 GUI アプリ (calc 等) には依存しない。
/// </summary>
[Trait("Layer", "Smoke")]
[Collection("AdactCli")]
public class AdactCliSmokeTests
{
    private readonly AdactDaemonFixture _fixture;

    /// <summary>
    /// 共有 daemon フィクスチャを受け取る xUnit コンストラクタ。
    /// </summary>
    /// <param name="fixture">共有される <see cref="AdactDaemonFixture"/>。</param>
    public AdactCliSmokeTests(AdactDaemonFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// list-apps の stdout 先頭行が設計 §5.2 で規定された TSV ヘッダ (windowRef\tsessionId\t...) と一致することを確認する。
    /// CLI 出力スキーマの回帰を Smoke で検出するため。
    /// </summary>
    [Fact]
    public void ListApps_ReturnsTabSeparatedHeader()
    {
        var result = CliProcess.RunWithServer("list-apps", _fixture.BaseUrl);

        Assert.True(result.ExitCode == 0,
            $"list-apps exit={result.ExitCode}\nstdout: {result.Stdout}\nstderr: {result.Stderr}");

        var firstLine = result.Stdout
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        Assert.NotNull(firstLine);

        // 設計 §5.2 / §5.3: 列順は固定。
        Assert.Equal(
            "windowRef\tsessionId\tprocessName\tprocessId\tclassName\twindowTitle",
            firstLine);
    }

    /// <summary>
    /// 非ローカル URL を --server で渡した daemon-stop が CLI 段階で LOCAL_ONLY exit=2 となることを確認する。
    /// 設計 §3.4 / §6.3 の localhost ガードの回帰防止。
    /// </summary>
    [Fact]
    public void DaemonStop_NonLocalhostUrl_ReturnsLocalOnlyExit2()
    {
        // 設計 §3.4 / §6.3: 非ローカル URL を指定した daemon-stop は CLI 段階で reject。
        // RFC 5737 の TEST-NET-1 (192.0.2.0/24) を使用 → 実通信は発生しない。
        var result = CliProcess.Run("daemon-stop --server http://192.0.2.1:41300/mcp");

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("error " + Adact.Cli.Output.ErrorCodes.LocalOnly, result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>
    /// 未知タイトルに対する attach が、daemon から NOT_FOUND エラーを受け取って exit=1 となることを確認する。
    /// daemon の windows_attach 失敗パスと CLI のエラー伝達の低コスト検出。
    /// </summary>
    [Fact]
    public void Attach_UnknownTitle_ReturnsExit1WithError()
    {
        // NOTE: 実 UIA の列挙 (FlaUI 経由) に依存するため L4 寄りのテスト。
        // daemon が NOT_FOUND を返す経路 (windows_attach の失敗パス) の最小確認として残置している。
        // ロジック単体の検証は <see cref="Adact.Cli.Tests.Unit.AttachCommandTests"/> 側で実施。
        var result = CliProcess.RunWithServer(
            "attach w999999",
            _fixture.BaseUrl);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("error ", result.Stderr, StringComparison.Ordinal);
    }
}

/// <summary>
/// L4 Smoke のうち daemon を必要としないテスト。<c>--help</c> はローカル処理のため fixture を共有しない。
/// </summary>
[Trait("Layer", "Smoke")]
public class AdactCliHelpTests
{
    /// <summary>
    /// adact --help が exit=0 となり、主要サブコマンド名 (list-apps / attach) がヘルプに含まれることを確認する。
    /// CommandLine ヒエラルキーの回帰 (コマンド未登録など) の Smoke 検出。
    /// </summary>
    [Fact]
    public void Help_ReturnsZeroAndPrintsUsage()
    {
        // --help は daemon に接続しない。Collection 共有も不要なため独立クラスに切り出す。
        var result = CliProcess.Run("--help");

        Assert.Equal(0, result.ExitCode);
        // System.CommandLine 2.0 のヘルプには各サブコマンド名が含まれる。
        Assert.Contains("list-apps", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("attach", result.Stdout, StringComparison.Ordinal);
    }
}
