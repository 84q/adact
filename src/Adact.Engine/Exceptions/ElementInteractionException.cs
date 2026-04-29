namespace Adact.Engine.Exceptions;

/// <summary>
/// Click / Fill 等の要素インタラクションが UIA 内部で失敗した際に投げられる例外。
/// 失敗した操作名 (<see cref="Operation"/>) と Ref ID (<see cref="RefId"/>) を保持する。
/// </summary>
public sealed class ElementInteractionException : AdactException
{
    /// <summary>失敗対象の要素を示す Ref ID (例: <c>"s1e3"</c>)。</summary>
    public string RefId { get; }

    /// <summary>失敗した操作名 (例: <c>"click"</c>、<c>"fill"</c>)。</summary>
    public string Operation { get; }

    /// <summary>新しいインスタンスを初期化する。</summary>
    /// <param name="refId">失敗対象の Ref ID。</param>
    /// <param name="operation">失敗した操作名。</param>
    /// <param name="message">操作失敗の詳細メッセージ。</param>
    public ElementInteractionException(string refId, string operation, string message)
        : base($"Failed to {operation} on ref '{refId}': {message}")
    {
        RefId = refId;
        Operation = operation;
    }

    /// <summary>新しいインスタンスを初期化する (内部例外つき)。</summary>
    /// <param name="refId">失敗対象の Ref ID。</param>
    /// <param name="operation">失敗した操作名。</param>
    /// <param name="message">操作失敗の詳細メッセージ。</param>
    /// <param name="inner">原因となった内部例外。</param>
    public ElementInteractionException(string refId, string operation, string message, Exception inner)
        : base($"Failed to {operation} on ref '{refId}': {message}", inner)
    {
        RefId = refId;
        Operation = operation;
    }
}
