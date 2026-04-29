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

    public AdactCliSmokeTests(AdactDaemonFixture fixture)
    {
        _fixture = fixture;
    }

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

    [Fact]
    public void DaemonStop_NonLocalhostUrl_ReturnsLocalOnlyExit2()
    {
        // 設計 §3.4 / §6.3: 非ローカル URL を指定した daemon-stop は CLI 段で reject。
        // RFC 5737 の TEST-NET-1 (192.0.2.0/24) を使用 → 実通信は発生しない。
        var result = CliProcess.Run("daemon-stop --server http://192.0.2.1:41300/mcp");

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("error " + Adact.Cli.Output.ErrorCodes.LocalOnly, result.Stderr, StringComparison.Ordinal);
    }

    [Fact]
    public void Attach_UnknownTitle_ReturnsExit1WithError()
    {
        // NOTE: 実 UIA の列挙 (FlaUI 経由) に依存するため L4 寄りのテスト。
        // daemon が NOT_FOUND を返す経路 (windows_attach の失敗パス) の最小確認として残置している。
        // ロジック単体の検証は <see cref="Adact.Cli.Tests.Unit.AttachCommandTests"/> 側で実施。
        var result = CliProcess.RunWithServer(
            "attach --title __adact_nonexistent_window__",
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
