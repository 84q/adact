namespace Adact.Tests.Common;

/// <summary>
/// テスト用外部サーバー接続情報のヘルパー。
/// </summary>
public static class ExternalServerHelper
{
    /// <summary>
    /// 外部サーバー URL を指定する環境変数名。
    /// </summary>
    public const string ServerUrlEnvironmentVariable = "ADACT_SERVER_URL";

    /// <summary>
    /// 環境変数 <c>ADACT_SERVER_URL</c> から外部サーバーの URI を取得する。
    /// </summary>
    public static Uri? GetExternalServerUri()
    {
        var value = Environment.GetEnvironmentVariable(ServerUrlEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(value)) return null;

        var url = value.Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"{ServerUrlEnvironmentVariable} must be an absolute http(s) URL, e.g. http://127.0.0.1:41300/mcp.");
        }

        return uri;
    }
}
