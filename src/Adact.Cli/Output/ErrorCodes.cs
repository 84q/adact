namespace Adact.Cli.Output;

internal static class ErrorCodes
{
    public const string InvalidArgument = "INVALID_ARGUMENT";
    public const string InvalidRefFormat = "INVALID_REF_FORMAT";
    public const string InvalidWindowRef = "INVALID_WINDOW_REF";
    public const string NotFound = "NOT_FOUND";
    public const string NoActiveSession = "NO_ACTIVE_SESSION";
    public const string AmbiguousAttach = "AMBIGUOUS_ATTACH";
    public const string CloseFailed = "CLOSE_FAILED";
    public const string KillFailed = "KILL_FAILED";
    public const string Timeout = "TIMEOUT";
    public const string SnapshotFailed = "SNAPSHOT_FAILED";
    public const string ConnectionFailed = "CONNECTION_FAILED";
    public const string LocalOnly = "LOCAL_ONLY";
    public const string InternalError = "INTERNAL_ERROR";
}
