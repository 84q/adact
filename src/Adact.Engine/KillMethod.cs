namespace Adact.Engine;

/// <summary>
/// Indicates how a process was terminated.
/// </summary>
public enum KillMethod
{
    /// <summary>
    /// The process exited after a graceful close request.
    /// </summary>
    Graceful,

    /// <summary>
    /// The process was forcibly terminated.
    /// </summary>
    Forced,

    /// <summary>
    /// The process was forcibly terminated after the graceful timeout.
    /// </summary>
    ForcedAfterTimeout,
}
