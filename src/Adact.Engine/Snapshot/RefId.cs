namespace Adact.Engine.Snapshot;

/// <summary>Ref ID 形式 <c>s&lt;sessionId&gt;e&lt;elementId&gt;</c> を組み立てる/分解するユーティリティ。</summary>
public static class RefId
{
    public static string Format(int sessionId, int elementId)
        => $"s{sessionId}e{elementId}";

    public static bool TryParse(string value, out int sessionId, out int elementId)
    {
        sessionId = elementId = 0;
        if (string.IsNullOrEmpty(value)) return false;
        if (value[0] != 's') return false;

        int ePos = value.IndexOf('e', 1);
        if (ePos < 0) return false;

        if (!uint.TryParse(value.AsSpan(1, ePos - 1), out var s)) return false;
        if (!uint.TryParse(value.AsSpan(ePos + 1), out var e)) return false;
        sessionId = (int)s;
        elementId = (int)e;
        return true;
    }
}
