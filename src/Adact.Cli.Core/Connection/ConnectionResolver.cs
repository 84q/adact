namespace Adact.Cli.Connection;

/// <summary>
/// </summary>
internal static class ConnectionResolver
{
    /// <summary>
    /// </summary>
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

        return null;
    }

    /// <summary>
    /// </summary>
    public static NamedPipeEndPoint ResolveNamedPipeEndpoint(string? cwd = null)
    {
        var workspacePath = NamedPipeEndPoint.ResolveWorkspacePath(cwd);
        return NamedPipeEndPoint.FromWorkspacePath(workspacePath);
    }
}
