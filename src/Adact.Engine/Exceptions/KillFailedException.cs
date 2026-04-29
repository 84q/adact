namespace Adact.Engine.Exceptions;

/// <summary>Process.Kill() 経由でプロセス終了が失敗した。</summary>
public sealed class KillFailedException : AdactException
{
    /// <summary>メッセージのみを指定して新しいインスタンスを初期化する。</summary>
    /// <param name="message">人間可読のエラーメッセージ。</param>
    public KillFailedException(string message) : base(message) { }

    /// <summary>メッセージと内部例外を指定して新しいインスタンスを初期化する。</summary>
    /// <param name="message">人間可読のエラーメッセージ。</param>
    /// <param name="inner">この例外を引き起こした内部例外。</param>
    public KillFailedException(string message, Exception inner) : base(message, inner) { }
}
