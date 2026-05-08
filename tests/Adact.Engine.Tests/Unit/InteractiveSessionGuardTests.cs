using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>Contains tests for the Interactive Session Guard behavior.</summary>
[Trait("Layer", "Unit")]
public class InteractiveSessionGuardTests
{
    /// <summary>Performs the Check Given Session0 And Win Sta0 Returns Not Ok operation.</summary>
    [Fact]
    public void Check_GivenSession0AndWinSta0_ReturnsNotOk()
    {
        var result = InteractiveSessionGuard.Check(0, "WinSta0");
        Assert.False(result.Ok);
        Assert.NotNull(result.Message);
        Assert.Contains("SessionId=0", result.Message);
    }

    /// <summary>Performs the Check Given Service Window Station Returns Not Ok operation.</summary>
    [Fact]
    public void Check_GivenServiceWindowStation_ReturnsNotOk()
    {
        var result = InteractiveSessionGuard.Check(1, "Service-0x0-3e7$");
        Assert.False(result.Ok);
        Assert.NotNull(result.Message);
        Assert.Contains("Service-0x0-3e7$", result.Message);
    }

    /// <summary>Performs the Check Given Lowercase Win Sta0 Returns Ok operation.</summary>
    [Fact]
    public void Check_GivenLowercaseWinSta0_ReturnsOk()
    {
        var result = InteractiveSessionGuard.Check(1, "winsta0");
        Assert.True(result.Ok);
        Assert.Null(result.Message);
    }

    /// <summary>Performs the Check Given Rdp Session And Win Sta0 Returns Ok operation.</summary>
    [Fact]
    public void Check_GivenRdpSessionAndWinSta0_ReturnsOk()
    {
        var result = InteractiveSessionGuard.Check(2, "WinSta0");
        Assert.True(result.Ok);
        Assert.Null(result.Message);
    }

    /// <summary>Performs the Check Given Null Window Station Returns Not Ok operation.</summary>
    [Fact]
    public void Check_GivenNullWindowStation_ReturnsNotOk()
    {
        var result = InteractiveSessionGuard.Check(1, null);
        Assert.False(result.Ok);
        Assert.NotNull(result.Message);
        Assert.Contains("WindowStation=<unknown>", result.Message);
    }
}
