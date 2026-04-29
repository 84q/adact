using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>
/// 設計: discussion/018_対話セッション判定.md §6 の Unit テスト。
/// 純関数 <see cref="InteractiveSessionGuard.Check(int, string?)"/> を観測値の組み合わせで検証する。
/// </summary>
[Trait("Layer", "Unit")]
public class InteractiveSessionGuardTests
{
    /// <summary>
    /// SessionId=0 かつ WindowStation=WinSta0 のサービスセッション相当状態では Ok=false となることを確認する。
    /// サービス上で誤って attach できてしまわないようにするため。
    /// </summary>
    [Fact]
    public void Check_GivenSession0AndWinSta0_ReturnsNotOk()
    {
        var result = InteractiveSessionGuard.Check(0, "WinSta0");
        Assert.False(result.Ok);
        Assert.NotNull(result.Message);
        Assert.Contains("SessionId=0", result.Message);
    }

    /// <summary>
    /// Service-* などの非対話 WindowStation を検出して Ok=false となることを確認する。
    /// </summary>
    [Fact]
    public void Check_GivenServiceWindowStation_ReturnsNotOk()
    {
        var result = InteractiveSessionGuard.Check(1, "Service-0x0-3e7$");
        Assert.False(result.Ok);
        Assert.NotNull(result.Message);
        Assert.Contains("Service-0x0-3e7$", result.Message);
    }

    /// <summary>
    /// 小文字表記の "winsta0" も対話 station として認識され Ok=true となることを確認する (大小文字不一致)。
    /// </summary>
    [Fact]
    public void Check_GivenLowercaseWinSta0_ReturnsOk()
    {
        var result = InteractiveSessionGuard.Check(1, "winsta0");
        Assert.True(result.Ok);
        Assert.Null(result.Message);
    }

    /// <summary>
    /// SessionId>0 + WinSta0 の RDP/コンソールセッションでは Ok=true となることを確認する。
    /// </summary>
    [Fact]
    public void Check_GivenRdpSessionAndWinSta0_ReturnsOk()
    {
        var result = InteractiveSessionGuard.Check(2, "WinSta0");
        Assert.True(result.Ok);
        Assert.Null(result.Message);
    }

    /// <summary>
    /// WindowStation 名が null のとき Ok=false となり、メッセージに <c>&lt;unknown&gt;</c> マーカーが含まれることを確認する。
    /// </summary>
    [Fact]
    public void Check_GivenNullWindowStation_ReturnsNotOk()
    {
        var result = InteractiveSessionGuard.Check(1, null);
        Assert.False(result.Ok);
        Assert.NotNull(result.Message);
        Assert.Contains("WindowStation=<unknown>", result.Message);
    }
}
