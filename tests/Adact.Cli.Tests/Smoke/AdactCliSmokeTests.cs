using Xunit;

namespace Adact.Cli.Tests.Smoke;

/// <summary>Contains tests for the Adact Cli Smoke behavior.</summary>
[Trait("Layer", "Smoke")]
[Collection("AdactCli")]
public class AdactCliSmokeTests
{
    private readonly AdactDaemonFixture _fixture;

    /// <summary>Initializes a new instance of the Adact Cli Smoke Tests class.</summary>
    public AdactCliSmokeTests(AdactDaemonFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Performs the List Apps Returns Tab Separated Header operation.</summary>
    [Fact]
    public void ListApps_ReturnsTabSeparatedHeader()
    {
        var result = CliProcess.RunWithServer("list-windows", _fixture.BaseUrl);

        Assert.True(result.ExitCode == 0,
            $"list-windows exit={result.ExitCode}\nstdout: {result.Stdout}\nstderr: {result.Stderr}");

        var lines = result.Stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var separatorIdx = Array.IndexOf(lines, "---");
        Assert.True(separatorIdx >= 0, "Missing '---' separator in list-windows output.");
        var headerLine = lines.Skip(separatorIdx + 1).FirstOrDefault();
        Assert.NotNull(headerLine);

        Assert.Equal(
            "windowRef\tsessionId\tprocessName\tprocessId\tclassName\twindowTitle",
            headerLine);
    }

    /// <summary>Performs the Daemon Stop Non Localhost Url Returns Local Only Exit2 operation.</summary>
    [Fact]
    public void DaemonStop_NonLocalhostUrl_ReturnsLocalOnlyExit2()
    {
        var result = CliProcess.Run("daemon-stop --server http://192.0.2.1:41300/mcp");

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("error: " + Adact.Cli.Output.ErrorCodes.LocalOnly, result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Performs the Attach Unknown Title Returns Exit1 With Error operation.</summary>
    [Fact]
    public void Attach_UnknownTitle_ReturnsExit1WithError()
    {
        var result = CliProcess.RunWithServer(
            "attach w999999",
            _fixture.BaseUrl);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("error:", result.Stdout, StringComparison.Ordinal);
    }
}

/// <summary>Contains tests for the Adact Cli Help behavior.</summary>
[Trait("Layer", "Smoke")]
public class AdactCliHelpTests
{
    /// <summary>Performs the Help Returns Zero And Prints Usage operation.</summary>
    [Fact]
    public void Help_ReturnsZeroAndPrintsUsage()
    {
        var result = CliProcess.Run("--help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("list-windows", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("attach", result.Stdout, StringComparison.Ordinal);
    }
}
