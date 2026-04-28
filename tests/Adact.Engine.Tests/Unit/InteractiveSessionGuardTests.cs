using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>
/// 設計: discussion/018_対話セッション判定.md §6 の Unit テスト。
/// 純関数 <see cref="InteractiveSessionGuard.Check(int, string?)"/> を観測値の組み合わせで検証する。
/// </summary>
[Trait("Layer", "Unit")]
public class InteractiveSessionGuardTests
{
  [Fact]
  public void Check_GivenSession0AndWinSta0_ReturnsNotOk()
  {
    var result = InteractiveSessionGuard.Check(0, "WinSta0");
    Assert.False(result.Ok);
    Assert.NotNull(result.Message);
    Assert.Contains("SessionId=0", result.Message);
  }

  [Fact]
  public void Check_GivenServiceWindowStation_ReturnsNotOk()
  {
    var result = InteractiveSessionGuard.Check(1, "Service-0x0-3e7$");
    Assert.False(result.Ok);
    Assert.NotNull(result.Message);
    Assert.Contains("Service-0x0-3e7$", result.Message);
  }

  [Fact]
  public void Check_GivenLowercaseWinSta0_ReturnsOk()
  {
    var result = InteractiveSessionGuard.Check(1, "winsta0");
    Assert.True(result.Ok);
    Assert.Null(result.Message);
  }

  [Fact]
  public void Check_GivenRdpSessionAndWinSta0_ReturnsOk()
  {
    var result = InteractiveSessionGuard.Check(2, "WinSta0");
    Assert.True(result.Ok);
    Assert.Null(result.Message);
  }

  [Fact]
  public void Check_GivenNullWindowStation_ReturnsNotOk()
  {
    var result = InteractiveSessionGuard.Check(1, null);
    Assert.False(result.Ok);
    Assert.NotNull(result.Message);
    Assert.Contains("WindowStation=<unknown>", result.Message);
  }
}
