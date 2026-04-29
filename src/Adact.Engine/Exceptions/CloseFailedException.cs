namespace Adact.Engine.Exceptions;

/// <summary>UIA WindowPattern.Close() / WM_CLOSE 経由のウィンドウクローズが失敗した。</summary>
public sealed class CloseFailedException : AdactException
{
    /// <summary>メッセージのみを指定して新しいインスタンスを初期化する。</summary>
    /// <param name="message">人間可読のエラーメッセージ。</param>
    public CloseFailedException(string message) : base(message) { }

    /// <summary>メッセージと内部例外を指定して新しいインスタンスを初期化する。</summary>
    /// <param name="message">人間可読のエラーメッセージ。</param>
    /// <param name="inner">この例外を引き起こした内部例外。</param>
    public CloseFailedException(string message, Exception inner) : base(message, inner) { }
}
