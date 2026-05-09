using System.Security.Cryptography;
using System.Text;

namespace Adact.Cli.Connection;

/// <summary>
/// </summary>
internal sealed class NamedPipeEndPoint
{
    public const string PipePrefix = @"\\.\pipe\";

    public const string AdactPipePrefix = "adact-";

    public const string DefaultSessionName = "default";

    public const int WorkspaceHashLength = 16;

    public string PipeName { get; }

    public string WorkspaceHash { get; }

    public string SessionName { get; }

    /// <summary>
    /// </summary>
    private NamedPipeEndPoint(string pipeName, string workspaceHash, string sessionName)
    {
        PipeName = pipeName;
        WorkspaceHash = workspaceHash;
        SessionName = sessionName;
    }

    /// <summary>
    /// Creates a named-pipe endpoint for the given workspace path.
    /// </summary>
    /// <param name="workspacePath">
    /// The workspace path used to derive the stable pipe hash.
    /// </param>
    /// <param name="sessionName">The optional logical session name appended to the pipe name.</param>
    /// <returns>The derived named-pipe endpoint.</returns>
    public static NamedPipeEndPoint FromWorkspacePath(string workspacePath, string? sessionName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(workspacePath);

        var hash = ComputeWorkspaceHash(workspacePath);
        var sess = string.IsNullOrEmpty(sessionName) ? DefaultSessionName : sessionName;
        var pipeName = $"{PipePrefix}{AdactPipePrefix}{hash}-{sess}";

        return new NamedPipeEndPoint(pipeName, hash, sess);
    }

    /// <summary>
    /// </summary>
    public static NamedPipeEndPoint Parse(string pipeName)
    {
        ArgumentException.ThrowIfNullOrEmpty(pipeName);

        if (!pipeName.StartsWith(PipePrefix + AdactPipePrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Invalid pipe name '{pipeName}'. Expected format: {PipePrefix}{AdactPipePrefix}{{hash}}-{DefaultSessionName}",
                nameof(pipeName));
        }

        var namePart = pipeName[(PipePrefix.Length + AdactPipePrefix.Length)..];
        var dashIndex = namePart.LastIndexOf('-');

        if (dashIndex < 0)
        {
            throw new ArgumentException(
                $"Invalid pipe name '{pipeName}'. Session name separator not found.",
                nameof(pipeName));
        }

        var hash = namePart[..dashIndex];
        var session = namePart[(dashIndex + 1)..];

        if (hash.Length != WorkspaceHashLength)
        {
            throw new ArgumentException(
                $"Invalid workspace hash length in pipe name '{pipeName}'. Expected {WorkspaceHashLength} characters.",
                nameof(pipeName));
        }

        return new NamedPipeEndPoint(pipeName, hash, session);
    }

    /// <summary>
    /// </summary>
    private static string ComputeWorkspaceHash(string workspacePath)
    {
        var normalized = Path.GetFullPath(workspacePath).ToLowerInvariant();
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        var hex = Convert.ToHexString(hash);
        return hex[..WorkspaceHashLength];
    }

    /// <summary>
    /// </summary>
    public static string ResolveWorkspacePath(string? startDir = null)
    {
        var dir = startDir ?? Environment.CurrentDirectory;

        var current = new DirectoryInfo(dir);
        while (current != null)
        {
            var adactDir = Path.Combine(current.FullName, ".adact");
            if (Directory.Exists(adactDir))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        return dir;
    }
}
