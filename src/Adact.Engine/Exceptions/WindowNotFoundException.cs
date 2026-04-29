namespace Adact.Engine.Exceptions;

/// <summary>
/// <see cref="UiaEngine.AttachAsync(AttachQuery, CancellationToken)"/> 等で
/// <see cref="AttachQuery"/> にマッチするウィンドウが現在見つからなかった場合に投げられる例外。
/// </summary>
public sealed class WindowNotFoundException : AdactException
{
    /// <summary>マッチに使用された <see cref="AttachQuery"/>。</summary>
    public AttachQuery Query { get; }

    /// <summary>新しいインスタンスを初期化する。</summary>
    /// <param name="query">マッチに使用された <see cref="AttachQuery"/>。</param>
    public WindowNotFoundException(AttachQuery query)
        : base($"No window matched the attach query: {Describe(query)}")
    {
        Query = query;
    }

    /// <summary>診断メッセージ用に <see cref="AttachQuery"/> を人間可読な形式へ整形する。</summary>
    /// <param name="q">対象クエリ。</param>
    /// <returns>
    /// "processName=..., windowTitle=..., pid=..." の各要素を ", " 区切りで連結した文字列。
    /// null のフィールドは省略される。
    /// なお現状の実装では <see cref="AttachQuery.ClassName"/> は出力対象に含めない。
    /// 出力すべきフィールドが 1 つもない場合は <c>"(empty)"</c>。
    /// </returns>
    private static string Describe(AttachQuery q)
    {
        var parts = new List<string>();
        if (q.ProcessName is not null) parts.Add($"processName=\"{q.ProcessName}\"");
        if (q.WindowTitle is not null) parts.Add($"windowTitle=\"{q.WindowTitle}\"");
        if (q.ProcessId is not null) parts.Add($"pid={q.ProcessId}");
        return parts.Count == 0 ? "(empty)" : string.Join(", ", parts);
    }
}
