namespace Adact.Engine.Exceptions;

/// <summary>
/// <see cref="UiaEngine.AttachAsync(AttachQuery, CancellationToken)"/> 実行時に複数のウィンドウが
/// 同一クエリにマッチした場合に投げられる例外。曖昧さ解消用に候補リストを保持する。
/// </summary>
public sealed class AmbiguousAttachException : AdactException
{
    /// <summary>マッチに使用された <see cref="AttachQuery"/>。</summary>
    public AttachQuery Query { get; }

    /// <summary>クエリにマッチしたすべての候補ウィンドウ。</summary>
    public IReadOnlyList<WindowInfo> Candidates { get; }

    /// <summary>新しいインスタンスを初期化する。</summary>
    /// <param name="query">マッチに使用された <see cref="AttachQuery"/>。</param>
    /// <param name="candidates">クエリにマッチしたすべての候補ウィンドウ。</param>
    public AmbiguousAttachException(AttachQuery query, IReadOnlyList<WindowInfo> candidates)
        : base($"Multiple windows ({candidates.Count}) matched the attach query. Use ListWindowsAsync to disambiguate.")
    {
        Query = query;
        Candidates = candidates;
    }
}
