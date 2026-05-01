using Xunit;
using Xunit.Sdk;

namespace Adact.Mcp.Stdio.Tests;

internal sealed class InteractiveFactAttribute : FactAttribute
{
    public InteractiveFactAttribute()
    {
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
        var probe = Adact.Engine.InteractiveSessionGuard.Probe();
        if (probe.Ok) return;

        throw SkipException.ForSkip(probe.Message ?? "This test requires an interactive Windows desktop session.");
    }
}
