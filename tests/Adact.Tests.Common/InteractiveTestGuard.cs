using Xunit;
using Xunit.Sdk;

namespace Adact.Tests.Common;

/// <summary>Marks tests that use the Interactive Fact Attribute attribute.</summary>
public sealed class InteractiveFactAttribute : FactAttribute
{
    /// <summary>Initializes a new instance of the Interactive Fact Attribute class.</summary>
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

/// <summary>Provides guard helpers for tests.</summary>
public static class InteractiveTestGuard
{
    /// <summary>Performs the Skip If Not Interactive operation.</summary>
    public static void SkipIfNotInteractive()
    {
        if (ExternalServerHelper.GetExternalServerUri() is not null) return;

        var probe = Adact.Engine.InteractiveSessionGuard.Probe();
        if (probe.Ok) return;

        throw SkipException.ForSkip(probe.Message ?? "This test requires an interactive Windows desktop session.");
    }
}
