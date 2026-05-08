namespace Adact.Cli.Connection;

/// <summary>
/// </summary>
internal sealed class ServerEndpoint
{
    public Uri Url { get; }

    /// <summary>
    /// </summary>
    public bool IsLocalhost { get; }

    private ServerEndpoint(Uri url, bool isLocalhost)
    {
        Url = url;
        IsLocalhost = isLocalhost;
    }

    /// <summary>
    /// </summary>
    public static ServerEndpoint Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidUrlException(
                "Server URL is empty. Specify a full URL like 'http://127.0.0.1:41300/mcp'.");
        }

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            throw new InvalidUrlException(
                $"Invalid server URL '{raw}'. Must be a full URL (e.g. 'http://127.0.0.1:41300/mcp').");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidUrlException(
                $"Unsupported scheme '{uri.Scheme}' in server URL '{raw}'. Only http/https are supported.");
        }

        if (string.IsNullOrEmpty(uri.Host))
        {
            throw new InvalidUrlException(
                $"Server URL '{raw}' has no host component.");
        }

        return new ServerEndpoint(uri, IsLocalhostHost(uri.Host));
    }

    private static bool IsLocalhostHost(string host)
    {
        var stripped = host;
        if (stripped.Length >= 2 && stripped[0] == '[' && stripped[^1] == ']')
        {
            stripped = stripped[1..^1];
        }

        return string.Equals(stripped, "127.0.0.1", StringComparison.Ordinal)
            || string.Equals(stripped, "::1", StringComparison.Ordinal)
            || string.Equals(stripped, "localhost", StringComparison.OrdinalIgnoreCase);
    }
}
