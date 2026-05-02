using Xunit;
using Xunit.Sdk;

namespace Adact.Tests.Common;

/// <summary>
/// インタラクティブ Windows デスクトップセッションが必要なテスト用の <see cref="FactAttribute"/>。
/// 外部サーバー（<c>ADACT_SERVER_URL</c>）が指定されている場合は常に実行可能とみなす。
/// </summary>
public sealed class InteractiveFactAttribute : FactAttribute
{
    /// <summary>
    /// インタラクティブセッションが必要なことを表す属性を初期化する。
    /// </summary>
    public InteractiveFactAttribute()
    {
        if (ExternalServerHelper.GetExternalServerUri() is not null) return;

        var probe = Adact.Engine.InteractiveSessionGuard.Probe();
        if (!probe.Ok)
        {
            Skip = probe.Message ?? "This test requires an interactive Windows desktop session.";
        }
    }
}

/// <summary>
/// インタラクティブ Windows デスクトップセッションの有無を判定するヘルパー。
/// </summary>
public static class InteractiveTestGuard
{
    /// <summary>
    /// インタラクティブ Windows デスクトップセッションでない場合にテストをスキップする。
    /// </summary>
    public static void SkipIfNotInteractive()
    {
        if (ExternalServerHelper.GetExternalServerUri() is not null) return;

        var probe = Adact.Engine.InteractiveSessionGuard.Probe();
        if (probe.Ok) return;

        throw SkipException.ForSkip(probe.Message ?? "This test requires an interactive Windows desktop session.");
    }
}
