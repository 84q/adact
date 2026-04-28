namespace Adact.Cli.Connection;

/// <summary>
/// 解決済みの MCP daemon 接続先 URL。設計 009 §3.2 / §3.4。
/// </summary>
internal sealed class ServerEndpoint
{
  public Uri Url { get; }

  /// <summary>
  /// 文字列ベースの localhost 判定 (DNS 解決はしない)。<c>daemon-stop</c> の guard 用。
  /// </summary>
  public bool IsLocalhost { get; }

  private ServerEndpoint(Uri url, bool isLocalhost)
  {
    Url = url;
    IsLocalhost = isLocalhost;
  }

  /// <summary>
  /// フル URL のみを受け付ける。スキームは http/https 必須。host が存在しないもの、
  /// ホスト名のみ (<c>192.168.1.10</c>) などは <see cref="InvalidUrlException"/>。
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
    // Uri.Host は IPv6 の場合 brackets を除去した形 ("::1") を返すが、
    // 念のため bracket 付きも許容する。
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
