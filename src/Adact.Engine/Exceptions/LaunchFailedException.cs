namespace Adact.Engine.Exceptions;

/// <summary>プロセス起動に失敗した (実行ファイル不在 / Process.Start 失敗 / COM エラー等)。設計 024 §3。</summary>
public sealed class LaunchFailedException : AdactException
{
    /// <summary>メッセージのみを指定して新しいインスタンスを初期化する。</summary>
    /// <param name="message">人間可読のエラーメッセージ。</param>
    public LaunchFailedException(string message) : base(message) { }

    /// <summary>メッセージと内部例外を指定して新しいインスタンスを初期化する。</summary>
    /// <param name="message">人間可読のエラーメッセージ。</param>
    /// <param name="inner">この例外を引き起こした内部例外。</param>
    public LaunchFailedException(string message, Exception inner) : base(message, inner) { }
}
