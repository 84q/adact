namespace Adact.Engine.Exceptions;

/// <summary>UIA ツリー走査または JSON 構築中に発生した snapshot 失敗を表す例外。</summary>
public sealed class SnapshotException : AdactException
{
    /// <summary>メッセージのみを指定して新しいインスタンスを初期化する。</summary>
    /// <param name="message">人間可読のエラーメッセージ。</param>
    public SnapshotException(string message) : base(message) { }

    /// <summary>メッセージと内部例外を指定して新しいインスタンスを初期化する。</summary>
    /// <param name="message">人間可読のエラーメッセージ。</param>
    /// <param name="inner">この例外を引き起こした内部例外。</param>
    public SnapshotException(string message, Exception inner) : base(message, inner) { }
}
