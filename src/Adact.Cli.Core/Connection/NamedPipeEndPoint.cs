using System.Security.Cryptography;
using System.Text;

namespace Adact.Cli.Connection;

/// <summary>
/// Named Pipe 接続先を表すエンドポイント。
/// パイプ名形式: \\.\pipe\adact-{workspaceHash}-default
/// 設計: discussion/033_NamedPipe_HTTP_統合設計.md §2.1
/// </summary>
internal sealed class NamedPipeEndPoint
{
    /// <summary>Named Pipe のプレフィックス。</summary>
    public const string PipePrefix = @"\\.\pipe\";

    /// <summary>パイプ名のプレフィックス。</summary>
    public const string AdactPipePrefix = "adact-";

    /// <summary>デフォルトのセッション名。</summary>
    public const string DefaultSessionName = "default";

    /// <summary>ワークスペースハッシュの長さ（16文字）。</summary>
    public const int WorkspaceHashLength = 16;

    /// <summary>完全なパイプ名（例: \\.\pipe\adact-{hash}-default）。</summary>
    public string PipeName { get; }

    /// <summary>ワークスペースハッシュ部分。</summary>
    public string WorkspaceHash { get; }

    /// <summary>セッション名。</summary>
    public string SessionName { get; }

    /// <summary>
    /// 内部コンストラクタ。<see cref="FromWorkspacePath(string, string?)" /> 経由で生成する。
    /// </summary>
    private NamedPipeEndPoint(string pipeName, string workspaceHash, string sessionName)
    {
        PipeName = pipeName;
        WorkspaceHash = workspaceHash;
        SessionName = sessionName;
    }

    /// <summary>
    /// ワークスペースパスから Named Pipe エンドポイントを生成する。
    /// </summary>
    /// <param name="workspacePath">
    /// ワークスペースの基点ディレクトリ。.adact ディレクトリの親、またはカレントディレクトリ。
    /// </param>
    /// <param name="sessionName">セッション名。null の場合は "default"。</param>
    /// <returns>生成された <see cref="NamedPipeEndPoint" />。</returns>
    public static NamedPipeEndPoint FromWorkspacePath(string workspacePath, string? sessionName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(workspacePath);

        var hash = ComputeWorkspaceHash(workspacePath);
        var sess = string.IsNullOrEmpty(sessionName) ? DefaultSessionName : sessionName;
        var pipeName = $"{PipePrefix}{AdactPipePrefix}{hash}-{sess}";

        return new NamedPipeEndPoint(pipeName, hash, sess);
    }

    /// <summary>
    /// パイプ名文字列から Named Pipe エンドポイントを解析する。
    /// </summary>
    /// <param name="pipeName">パイプ名（例: \\.\pipe\adact-{hash}-default）。</param>
    /// <returns>解析された <see cref="NamedPipeEndPoint" />。</returns>
    /// <exception cref="ArgumentException">パイプ名の形式が不正な場合。</exception>
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
    /// ワークスペースパスの SHA1 ハッシュ（先頭16文字）を計算する。
    /// </summary>
    /// <param name="workspacePath">ワークスペースパス。</param>
    /// <returns>16文字の16進数ハッシュ文字列。</returns>
    private static string ComputeWorkspaceHash(string workspacePath)
    {
        var normalized = Path.GetFullPath(workspacePath).ToLowerInvariant();
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA1.HashData(bytes);
        var hex = Convert.ToHexString(hash);
        return hex[..WorkspaceHashLength];
    }

    /// <summary>
    /// ワークスペースパスを探索し、.adact ディレクトリがあるか確認する。
    /// 見つからない場合はカレントディレクトリを返す。
    /// </summary>
    /// <param name="startDir">探索開始ディレクトリ。</param>
    /// <returns>ワークスペースの基点ディレクトリパス。</returns>
    public static string ResolveWorkspacePath(string? startDir = null)
    {
        var dir = startDir ?? Environment.CurrentDirectory;

        // .adact ディレクトリを親方向に探索
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

        // .adact が見つからない場合はカレントディレクトリを使用
        return dir;
    }
}
