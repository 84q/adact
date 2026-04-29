namespace Adact.Cli.Connection;

/// <summary>
/// 接続先 URL が不正な形式 (フル URL でない / スキームが http/https でない / host 不在 など) の場合に
/// throw される例外。CLI 層で <c>INVALID_ARGUMENT</c> (exit 2) にマップされる。
/// </summary>
internal sealed class InvalidUrlException : Exception
{
    public InvalidUrlException(string message) : base(message) { }
}

/// <summary>
/// .adact/config.json の JSON parse / 読み込みに失敗した場合に throw される例外。
/// CLI 層で <c>INVALID_ARGUMENT</c> (exit 2) にマップされる。
/// </summary>
internal sealed class ConfigParseException : Exception
{
    public ConfigParseException(string message, Exception? inner = null) : base(message, inner) { }
}
