using System.Globalization;

namespace Adact.Engine.Snapshot;

/// <summary>
/// Formats and parses stable element ref IDs.
/// </summary>
public static class RefId
{
    /// <summary>
    /// Formats a ref ID as <c>s&lt;sid&gt;e&lt;eid&gt;</c>.
    /// </summary>
    public static string Format(int sessionId, int elementId)
        => $"s{sessionId}e{elementId}";

    /// <summary>
    /// Tries to parse a ref ID in <c>s&lt;sid&gt;e&lt;eid&gt;</c> form.
    /// </summary>
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
