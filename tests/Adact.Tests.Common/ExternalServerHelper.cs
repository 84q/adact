namespace Adact.Tests.Common;

/// <summary>Provides helper methods for tests.</summary>
public static class ExternalServerHelper
{
    /// <summary>Gets the Server Url Environment Variable value.</summary>
    public const string ServerUrlEnvironmentVariable = "ADACT_SERVER_URL";

    /// <summary>Gets the Get External Server Uri value.</summary>
    public static Uri? GetExternalServerUri(Func<string, string?>? getEnvironmentVariable = null)
    {
        var value = (getEnvironmentVariable ?? Environment.GetEnvironmentVariable)(ServerUrlEnvironmentVariable);
        return ResolveExternalServerUri(value, ServerUrlEnvironmentVariable);
    }

    /// <summary>Resolves the Resolve External Server Uri value.</summary>
    public static Uri? ResolveExternalServerUri(string? value, string variableName = ServerUrlEnvironmentVariable)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var url = value.Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"{variableName} must be an absolute http(s) URL, e.g. http://127.0.0.1:41300/mcp.");
        }

        return uri;
    }
}
