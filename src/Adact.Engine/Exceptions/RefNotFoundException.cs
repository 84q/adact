namespace Adact.Engine.Exceptions;

/// <summary>
/// 指定された Ref ID を現セッションで解決できなかった場合に投げられる例外。
/// 形式不正・別セッション・現 snapshot に存在しない (再 snapshot が必要) などの理由を <see cref="Reason"/> に持つ。
/// </summary>
public sealed class RefNotFoundException : AdactException
{
    /// <summary>解決を試みた Ref ID。</summary>
    public string RefId { get; }

    /// <summary>解決失敗の理由 (任意)。例: <c>"malformed ref id"</c>、<c>"session mismatch"</c>。</summary>
    public string? Reason { get; }

    /// <summary>新しいインスタンスを初期化する。</summary>
    /// <param name="refId">解決を試みた Ref ID。</param>
    /// <param name="reason">解決失敗の任意の補足理由。</param>
    public RefNotFoundException(string refId, string? reason = null)
        : base($"Ref ID '{refId}' is not valid for this session{(reason is null ? "" : $": {reason}")}")
    {
        RefId = refId;
        Reason = reason;
    }
}
