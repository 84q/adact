using System.Globalization;

namespace Adact.Engine.Snapshot;

/// <summary>Ref ID 形式 <c>s&lt;sessionId&gt;e&lt;elementId&gt;</c> を組み立てる/分解するユーティリティ。</summary>
public static class RefId
{
    /// <summary>セッション ID と要素 ID から Ref ID 文字列を組み立てる。</summary>
    /// <param name="sessionId">セッション ID。</param>
    /// <param name="elementId">セッション内で一意な要素 ID。</param>
    /// <returns><c>s{sessionId}e{elementId}</c> 形式の文字列。</returns>
    public static string Format(int sessionId, int elementId)
        => $"s{sessionId}e{elementId}";

    /// <summary>Ref ID 文字列を解析し、セッション ID と要素 ID へ分解する。</summary>
    /// <param name="value">解析対象の Ref ID 文字列 (例: <c>"s1e3"</c>)。</param>
    /// <param name="sessionId">解析成功時はセッション ID、失敗時は 0。</param>
    /// <param name="elementId">解析成功時は要素 ID、失敗時は 0。</param>
    /// <returns>解析に成功した場合 true。形式不正なら false。</returns>
    public static bool TryParse(string value, out int sessionId, out int elementId)
    {
        sessionId = elementId = 0;
        if (string.IsNullOrEmpty(value)) return false;
        if (value[0] != 's') return false;

        int ePos = value.IndexOf('e', 1);
        if (ePos < 0) return false;

        if (!uint.TryParse(value.AsSpan(1, ePos - 1), NumberStyles.None, CultureInfo.InvariantCulture, out var s)) return false;
        if (!uint.TryParse(value.AsSpan(ePos + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var e)) return false;
        if (s > int.MaxValue || e > int.MaxValue) return false;
        sessionId = (int)s;
        elementId = (int)e;
        return true;
    }
}
