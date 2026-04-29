using System.Globalization;
using System.Text.Json;

namespace Adact.Cli.Commands;

/// <summary>
/// CLI 各コマンドが MCP レスポンス JSON の読み出しで共通して使うヘルパ。
/// 型不一致やプロパティ不在は例外を投げずに null/default を返す設計。
/// </summary>
internal static class JsonHelpers
{
    /// <summary>
    /// オブジェクト上の文字列プロパティを取り出す。文字列以外の値は <see cref="JsonElement.ToString"/> の結果を返す。
    /// </summary>
    /// <param name="obj">読み出し対象の JSON 要素。</param>
    /// <param name="name">プロパティ名。</param>
    /// <returns>文字列表現。不在/null のときは null。</returns>
    public static string? GetStringOrNull(JsonElement obj, string name)
    {
        if (obj.ValueKind != JsonValueKind.Object) return null;
        if (!obj.TryGetProperty(name, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.String => v.GetString(),
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => v.ToString(),
        };
    }

    /// <summary>
    /// 数値 (または数値表現の文字列) プロパティを文字列として取り出す。TSV 出力用のヘルパ。
    /// </summary>
    /// <param name="obj">読み出し対象の JSON 要素。</param>
    /// <param name="name">プロパティ名。</param>
    /// <returns>InvariantCulture の 10 進表現。取得できない場合は null。</returns>
    public static string? GetIntAsStringOrNull(JsonElement obj, string name)
    {
        if (obj.ValueKind != JsonValueKind.Object) return null;
        if (!obj.TryGetProperty(name, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.Number => v.TryGetInt64(out var l)
                ? l.ToString(CultureInfo.InvariantCulture)
                : v.GetRawText(),
            JsonValueKind.String => v.GetString(),
            _ => null,
        };
    }

    /// <summary>オブジェクト上の Int32 プロパティを取り出す。型不一致 / 不在は null。</summary>
    /// <param name="obj">読み出し対象の JSON 要素。</param>
    /// <param name="name">プロパティ名。</param>
    /// <returns>Int32 として取得できた値、さもなくば null。</returns>
    public static int? GetIntOrNull(JsonElement obj, string name)
    {
        if (obj.ValueKind != JsonValueKind.Object) return null;
        if (!obj.TryGetProperty(name, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)) return i;
        return null;
    }
}
