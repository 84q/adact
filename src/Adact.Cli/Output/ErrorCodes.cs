namespace Adact.Cli.Output;

/// <summary>
/// CLI が stderr に書き出すエラーコードの定数集合。設計 docs/spec/errors-and-output.md。
/// </summary>
/// <remarks>
/// daemon (MCP server) 側で発生したエラーは <c>code</c> プロパティ経由でそのまま CLI に伝搬し、
/// <see cref="McpResponse.TryReportError"/> で stderr に書き出される。
/// </remarks>
internal static class ErrorCodes
{
    /// <summary>引数の形式・組合せが不正。CLI exit code は 2 (UserError)。</summary>
    public const string InvalidArgument = "INVALID_ARGUMENT";

    /// <summary>Element Ref (<c>s&lt;sid&gt;e&lt;eid&gt;</c>) の形式が不正。</summary>
    public const string InvalidRefFormat = "INVALID_REF_FORMAT";

    /// <summary>Window Ref (<c>w&lt;n&gt;</c>) の形式が不正。</summary>
    public const string InvalidWindowRef = "INVALID_WINDOW_REF";

    /// <summary>指定された ref / session / window が見つからない。</summary>
    public const string NotFound = "NOT_FOUND";

    /// <summary>active session が存在しないのに省略形の操作を要求された。</summary>
    public const string NoActiveSession = "NO_ACTIVE_SESSION";

    /// <summary>WindowPattern.Close が失敗した。</summary>
    public const string CloseFailed = "CLOSE_FAILED";

    /// <summary>Process.Kill が失敗した。</summary>
    public const string KillFailed = "KILL_FAILED";

    /// <summary>UIA / 操作のタイムアウト。</summary>
    public const string Timeout = "TIMEOUT";

    /// <summary>snapshot 取得処理が失敗した。</summary>
    public const string SnapshotFailed = "SNAPSHOT_FAILED";

    /// <summary>daemon に接続できない。CLI exit code は 3 (ConnectionFailed)。</summary>
    public const string ConnectionFailed = "CONNECTION_FAILED";

    /// <summary>localhost 限定の操作を非ローカル接続で実行しようとした (例: <c>daemon-stop</c>)。</summary>
    public const string LocalOnly = "LOCAL_ONLY";

    /// <summary>デスクトップがロック / UAC / ウィンドウ非アクティブなどで操作がブロックされた。</summary>
    public const string OperationBlocked = "OPERATION_BLOCKED";

    /// <summary>その他の内部エラー。</summary>
    public const string InternalError = "INTERNAL_ERROR";
}
