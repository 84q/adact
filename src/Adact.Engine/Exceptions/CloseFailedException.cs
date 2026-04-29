namespace Adact.Engine.Exceptions;

/// <summary>UIA WindowPattern.Close() / WM_CLOSE 経由のウィンドウクローズが失敗した。</summary>
public sealed class CloseFailedException : AdactException
{
    public CloseFailedException(string message) : base(message) { }
    public CloseFailedException(string message, Exception inner) : base(message, inner) { }
}
