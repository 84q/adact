using Xunit;
using Xunit.Sdk;

namespace Adact.Mcp.Http.Tests;

internal sealed class InteractiveFactAttribute : FactAttribute
{
    public InteractiveFactAttribute()
    {
        if (AdactHttpServerFixture.GetExternalServerUri() is not null) return;

        var probe = Adact.Engine.InteractiveSessionGuard.Probe();
        if (!probe.Ok)
        {
            Skip = probe.Message ?? "This test requires an interactive Windows desktop session.";
        }
    }
}

internal static class InteractiveTestGuard
{
    public static void SkipIfNotInteractive()
    {
        if (AdactHttpServerFixture.GetExternalServerUri() is not null) return;

        var probe = Adact.Engine.InteractiveSessionGuard.Probe();
        if (probe.Ok) return;

        throw SkipException.ForSkip(probe.Message ?? "This test requires an interactive Windows desktop session.");
    }
}
