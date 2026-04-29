namespace Adact.Cli.Connection;

/// <summary>
/// 解決済みの MCP daemon 接続先 URL。設計 009 §3.2 / §3.4。
/// </summary>
internal sealed class ServerEndpoint
{
    /// <summary>接続先の絶対 URL。スキームは http/https のいずれか。</summary>
    public Uri Url { get; }

    /// <summary>
    /// 文字列ベースの localhost 判定 (DNS 解決はしない)。<c>daemon-stop</c> の guard 用。
    /// </summary>
    public bool IsLocalhost { get; }

    /// <summary>内部ファクトリ。外部からは <see cref="Parse(string)"/> 経由で生成する。</summary>
    /// <param name="url">接続先の絶対 URL。</param>
    /// <param name="isLocalhost">host 部が localhost を指すか (事前に <see cref="IsLocalhostHost"/> で計算済みの値)。</param>
    private ServerEndpoint(Uri url, bool isLocalhost)
    {
        Url = url;
        IsLocalhost = isLocalhost;
    }

    /// <summary>
    /// フル URL のみを受け付ける。スキームは http/https 必須。host が存在しないもの、
    /// ホスト名のみ (<c>192.168.1.10</c>) などは <see cref="InvalidUrlException"/>。
    /// </summary>
    /// <param name="raw">パース対象の URL 文字列。</param>
    /// <returns>解析済みの <see cref="ServerEndpoint"/>。</returns>
    /// <exception cref="InvalidUrlException">空文字列、不正な URL、未サポートスキーム、host 不在のいずれか。</exception>
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

    /// <summary>host 文字列が localhost を指しているか (127.0.0.1 / ::1 / localhost) を判定する。</summary>
    /// <param name="host"><see cref="Uri.Host"/> から取得した host 部。</param>
    /// <returns>localhost と見なせるなら true。</returns>
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
