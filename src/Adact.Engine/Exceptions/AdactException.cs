namespace Adact.Engine.Exceptions;

/// <summary>ADACT で発生するすべての独自例外の基底。</summary>
public abstract class AdactException : Exception
{
    /// <summary>メッセージのみを指定して新しいインスタンスを初期化する。</summary>
    /// <param name="message">人間可読のエラーメッセージ。</param>
    protected AdactException(string message) : base(message) { }

    /// <summary>メッセージと内部例外を指定して新しいインスタンスを初期化する。</summary>
    /// <param name="message">人間可読のエラーメッセージ。</param>
    /// <param name="inner">この例外を引き起こした内部例外。</param>
    protected AdactException(string message, Exception inner) : base(message, inner) { }
}
