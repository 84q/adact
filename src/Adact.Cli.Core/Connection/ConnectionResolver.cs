namespace Adact.Cli.Connection;

/// <summary>
/// 接続先の解決ロジック。設計 033。
/// 優先度: <c>--server</c>（HTTPモード）> Named Pipe（デフォルト）。
/// </summary>
internal static class ConnectionResolver
{
    /// <summary>
    /// 接続先を解決する。
    /// --server 指定時: HTTP モードで接続
    /// --server 未指定時: Named Pipe に接続
    /// </summary>
    /// <param name="explicitServer">CLI <c>--server</c> フラグの値。null/空白なら未指定扱い。</param>
    /// <param name="cwd">起点ディレクトリ。null なら <see cref="Environment.CurrentDirectory"/>。</param>
    /// <returns>解決済みの <see cref="ServerEndpoint"/>（HTTPモード時）。--server未指定時はnull。</returns>
    /// <exception cref="InvalidUrlException">URL 形式が不正な場合。</exception>
    /// <exception cref="ConfigParseException">.adact/config.json の parse / IO に失敗した場合。</exception>
    public static ServerEndpoint? ResolveHttpEndpoint(string? explicitServer, string? cwd = null)
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

        // --server未指定時はnullを返す（Named Pipeを使用）
        return null;
    }

    /// <summary>
    /// Named Pipe エンドポイントを取得する。
    /// </summary>
    /// <param name="cwd">起点ディレクトリ。null なら <see cref="Environment.CurrentDirectory"/>。</param>
    /// <returns>Named Pipe エンドポイント。</returns>
    public static NamedPipeEndPoint ResolveNamedPipeEndpoint(string? cwd = null)
    {
        var workspacePath = NamedPipeEndPoint.ResolveWorkspacePath(cwd);
        return NamedPipeEndPoint.FromWorkspacePath(workspacePath);
    }
}
