namespace Adact.Cli.Connection;

/// <summary>
/// 接続先 URL の解決ロジック。設計 009 §3.1。
/// 優先度: <c>--server</c> > <c>.adact/config.json</c> > 既定 (<see cref="DefaultUrl"/>)。
/// </summary>
internal static class ConnectionResolver
{
    /// <summary>何も指定がないときに使われる既定接続先。ローカル HTTP daemon。</summary>
    public const string DefaultUrl = "http://127.0.0.1:41300/mcp";

    /// <summary>
    /// 接続先を解決する。
    /// </summary>
    /// <param name="explicitServer">CLI <c>--server</c> フラグの値。null/空白なら未指定扱い。</param>
    /// <param name="cwd">起点ディレクトリ。null なら <see cref="Environment.CurrentDirectory"/>。</param>
    /// <returns>解決済みの <see cref="ServerEndpoint"/>。</returns>
    /// <exception cref="InvalidUrlException">URL 形式が不正な場合。</exception>
    /// <exception cref="ConfigParseException">.adact/config.json の parse / IO に失敗した場合。</exception>
    public static ServerEndpoint Resolve(string? explicitServer, string? cwd = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitServer))
        {
            return ServerEndpoint.Parse(explicitServer);
        }

        var startDir = cwd ?? Environment.CurrentDirectory;
        var fromConfig = ConfigLoader.FindServerFromConfig(startDir);
        if (!string.IsNullOrWhiteSpace(fromConfig))
        {
            return ServerEndpoint.Parse(fromConfig);
        }

        return ServerEndpoint.Parse(DefaultUrl);
    }
}
