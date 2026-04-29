using System.Text.Json;
using System.Text.Json.Nodes;

using Adact.Engine.Exceptions;

using ModelContextProtocol.Protocol;

namespace Adact.Mcp.Common;

/// <summary>
/// 業務例外 → MCP <see cref="CallToolResult"/> (isError:true) への変換を担うヘルパー。
/// systemic な例外は変換せず、呼び出し側で再 throw して SDK に JSON-RPC InternalError として処理させる。
/// 詳細は 002_アーキテクチャ設計.md §8 参照。
/// </summary>
internal static class ToolErrors
{
    /// <summary>引数の不正 (必須キー不足・例外的な型を含む)。</summary>
    public const string InvalidArgument = "INVALID_ARGUMENT";
    /// <summary>未知 / 引退済みの <c>w&lt;n&gt;</c> が指定された場合。</summary>
    public const string InvalidWindowRef = "INVALID_WINDOW_REF";
    /// <summary>検索条件に一致する window が見つからなかった。</summary>
    public const string WindowNotFound = "WINDOW_NOT_FOUND";
    /// <summary>element ref が不正 / 未知 / 現 snapshot に存在しない。</summary>
    public const string RefNotFound = "REF_NOT_FOUND";
    /// <summary>element ref の文字列形式が不正 (例: <c>s1e2</c> 形式に従わない)。</summary>
    public const string InvalidRefFormat = "INVALID_REF_FORMAT";
    /// <summary>UIA を介した click / fill 他の要素操作が失敗した。</summary>
    public const string ElementInteractionFailed = "ELEMENT_INTERACTION_FAILED";
    /// <summary>UIA tree 走査中に例外が発生して snapshot 取得に失敗した。</summary>
    public const string SnapshotFailed = "SNAPSHOT_FAILED";
    /// <summary>アクティブ session が無い状態で sessionId を省略した呼び出し。</summary>
    public const string NoActiveSession = "NO_ACTIVE_SESSION";
    /// <summary>対象の session が見つからなかった。</summary>
    public const string NotFound = "NOT_FOUND";
    /// <summary>WindowPattern.Close / WM_CLOSE で閉じられなかった。</summary>
    public const string CloseFailed = "CLOSE_FAILED";
    /// <summary>Process.Kill に失敗した。</summary>
    public const string KillFailed = "KILL_FAILED";
    /// <summary>プロセス起動 (Process.Start / UWP ActivateApplication) に失敗した。</summary>
    public const string LaunchFailed = "LAUNCH_FAILED";
    /// <summary>現モード (stdio 等) でサポートされないツールを呼んだ。</summary>
    public const string LocalOnly = "LOCAL_ONLY";
    /// <summary>業務例外として提示したい不規則例外を包んだケース (例: daemon 停止失敗)。</summary>
    public const string InternalError = "INTERNAL_ERROR";
    /// <summary><c>windows_wait_for</c> / <c>windows_wait_for_window</c> がタイムアウト内に成功条件を満たせなかった。</summary>
    public const string WaitTimeout = "WAIT_TIMEOUT";

    /// <summary>業務例外なら <see cref="CallToolResult"/> を返し、それ以外は null。</summary>
    /// <param name="ex">マッピング対象の例外。</param>
    /// <returns>業務例外に対応する error result、またはマッピング不可のとき <c>null</c>。</returns>
    public static CallToolResult? TryMap(Exception ex)
    {
        return ex switch
        {
            WindowNotFoundException w => Error(WindowNotFound, w.Message),
            RefNotFoundException r => Error(RefNotFound, r.Message,
                new JsonObject { ["refId"] = r.RefId }),
            ElementInteractionException e => Error(ElementInteractionFailed, e.Message),
            SnapshotException s => Error(SnapshotFailed, s.Message),
            CloseFailedException c => Error(CloseFailed, c.Message),
            KillFailedException k => Error(KillFailed, k.Message),
            LaunchFailedException l => Error(LaunchFailed, l.Message),
            WaitTimeoutException t => Error(WaitTimeout, t.Message),
            _ => null,
        };
    }

    /// <summary>
    /// エラーコード / メッセージ / 任意のデータを <c>isError:true</c> の <see cref="CallToolResult"/> に整形する。
    /// text content は <c>"&lt;code&gt;: &lt;message&gt;"</c> 、structured content は <c>{ code, message, details? }</c>。
    /// </summary>
    /// <param name="code">上記定数のいずれかのエラーコード。</param>
    /// <param name="message">人間可読なメッセージ。</param>
    /// <param name="details">エラーと一緒に返したい補助情報 (例: <c>{ candidateCount: 2 }</c>)。</param>
    /// <returns><c>IsError = true</c> の <see cref="CallToolResult"/>。</returns>
    public static CallToolResult Error(string code, string message, JsonObject? details = null)
    {
        var text = $"{code}: {message}";
        var structured = new JsonObject
        {
            ["code"] = code,
            ["message"] = message,
        };
        if (details is not null) structured["details"] = details;

        return new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = text }],
            StructuredContent = JsonSerializer.SerializeToElement(structured),
        };
    }
}
