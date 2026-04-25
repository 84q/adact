namespace Adact.Engine.Snapshot;

/// <summary>Ref ID 形式 <c>s&lt;sessionId&gt;g&lt;generation&gt;e&lt;elementId&gt;</c> を組み立てる/分解するユーティリティ。</summary>
public static class RefId
{
  public static string Format(int sessionId, int generation, int elementId)
      => $"s{sessionId}g{generation}e{elementId}";

  public static bool TryParse(string value, out int sessionId, out int generation, out int elementId)
  {
    sessionId = generation = elementId = 0;
    if (string.IsNullOrEmpty(value)) return false;
    if (value[0] != 's') return false;

    int gPos = value.IndexOf('g', 1);
    int ePos = gPos < 0 ? -1 : value.IndexOf('e', gPos + 1);
    if (gPos < 0 || ePos < 0) return false;

    if (!int.TryParse(value.AsSpan(1, gPos - 1), out sessionId)) return false;
    if (!int.TryParse(value.AsSpan(gPos + 1, ePos - gPos - 1), out generation)) return false;
    if (!int.TryParse(value.AsSpan(ePos + 1), out elementId)) return false;
    return true;
  }
}
