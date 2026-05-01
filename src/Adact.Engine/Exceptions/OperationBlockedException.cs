namespace Adact.Engine.Exceptions;

/// <summary>
/// UIA 操作がデスクトップ状態によってブロックされた場合に throw される例外。
/// </summary>
public sealed class OperationBlockedException : AdactException
{
    /// <summary>
    /// ブロック理由と内部例外を指定して新しいインスタンスを初期化する。
    /// </summary>
    /// <param name="reason">ブロック理由。</param>
    /// <param name="innerException">元の操作例外。</param>
    public OperationBlockedException(string reason, Exception innerException)
        : base($"operation blocked: {reason}", innerException)
    {
    }
}
