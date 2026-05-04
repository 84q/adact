using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Adact.Cli.Output;

internal static class CliOutput
{
    public static KeyValuePair<string, string?> Field(string key, string? value)
        => new(key, value);

    public static void WriteYamlSuccess(
        IEnumerable<KeyValuePair<string, string?>>? metaFields,
        IEnumerable<KeyValuePair<string, string?>> bodyFields)
    {
        WriteYamlDocument(result: true, errorCode: null, metaFields, bodyFields);
    }

    public static void WriteYamlFailure(
        string errorCode,
        string message,
        string? hint = null,
        IEnumerable<KeyValuePair<string, string?>>? bodyFields = null)
    {
        var fields = new List<KeyValuePair<string, string?>> { Field("message", message) };
        if (!string.IsNullOrEmpty(hint))
        {
            fields.Add(Field("hint", hint));
        }
        if (bodyFields is not null)
        {
            fields.AddRange(bodyFields.Where(static f => f.Value is not null));
        }

        WriteYamlDocument(result: false, errorCode, metaFields: null, fields);
    }

    public static void WriteTsvResult(bool result, IEnumerable<string> header, IEnumerable<IEnumerable<string?>> rows)
    {
        Console.Out.WriteLine($"result: {result.ToString().ToLowerInvariant()}");
        Console.Out.WriteLine("---");
        Console.Out.WriteLine(string.Join('\t', header));
        foreach (var row in rows)
        {
            Console.Out.WriteLine(string.Join('\t', row.Select(static cell => cell ?? string.Empty)));
        }
    }

    public static void WriteSnapshotSuccess(string snapshotPath, IEnumerable<KeyValuePair<string, string?>> prefaceFields, string treeText)
    {
        Console.Out.WriteLine("result: true");
        Console.Out.WriteLine($"snapshotPath: {FormatScalar(snapshotPath)}");
        Console.Out.WriteLine("---");

        foreach (var field in prefaceFields.Where(static f => f.Value is not null))
        {
            Console.Out.WriteLine($"{field.Key}: {FormatScalar(field.Value)}");
        }

        Console.Out.WriteLine();
        Console.Out.Write(treeText);
        if (!treeText.EndsWith('\n'))
        {
            Console.Out.WriteLine();
        }
    }

    public static IReadOnlyList<KeyValuePair<string, string?>> JsonObjectToFields(JsonElement obj, params string[] preferredOrder)
    {
        if (obj.ValueKind != JsonValueKind.Object)
        {
            return [Field("value", FormatJsonValue(obj))];
        }

        var list = new List<KeyValuePair<string, string?>>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var name in preferredOrder)
        {
            if (obj.TryGetProperty(name, out var value))
            {
                list.Add(Field(name, FormatJsonValue(value)));
                seen.Add(name);
            }
        }

        foreach (var property in obj.EnumerateObject())
        {
            if (seen.Add(property.Name))
            {
                list.Add(Field(property.Name, FormatJsonValue(property.Value)));
            }
        }

        return list;
    }

    public static string FormatJsonValue(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Array or JsonValueKind.Object => value.GetRawText(),
            _ => value.ToString(),
        };

    private static void WriteYamlDocument(
        bool result,
        string? errorCode,
        IEnumerable<KeyValuePair<string, string?>>? metaFields,
        IEnumerable<KeyValuePair<string, string?>> bodyFields)
    {
        Console.Out.WriteLine($"result: {result.ToString().ToLowerInvariant()}");
        if (!string.IsNullOrEmpty(errorCode))
        {
            Console.Out.WriteLine($"error: {errorCode}");
        }
        if (metaFields is not null)
        {
            foreach (var field in metaFields.Where(static f => f.Value is not null))
            {
                Console.Out.WriteLine($"{field.Key}: {FormatScalar(field.Value)}");
            }
        }

        Console.Out.WriteLine("---");
        foreach (var field in bodyFields.Where(static f => f.Value is not null))
        {
            Console.Out.WriteLine($"{field.Key}: {FormatScalar(field.Value)}");
        }
    }

    private static string FormatScalar(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        if (bool.TryParse(value, out var boolValue))
        {
            return boolValue ? "true" : "false";
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
        {
            return value;
        }

        if (IsSafeBareScalar(value))
        {
            return value;
        }

        return "\"" + EscapeYamlDoubleQuoted(value) + "\"";
    }

    private static bool IsSafeBareScalar(string s)
    {
        foreach (var ch in s)
        {
            var ok = (ch >= 'A' && ch <= 'Z')
                  || (ch >= 'a' && ch <= 'z')
                  || (ch >= '0' && ch <= '9')
                  || ch == ' ' || ch == '_' || ch == '-' || ch == '.' || ch == '/' || ch == '\\';
            if (!ok) return false;
        }

        return true;
    }

    private static string EscapeYamlDoubleQuoted(string s)
    {
        var sb = new StringBuilder(s.Length + 8);
        foreach (var ch in s)
        {
            switch (ch)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (ch < 0x20 || ch == 0x7F)
                    {
                        sb.Append("\\u")
                          .Append(((int)ch).ToString("X4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                    break;
            }
        }

        return sb.ToString();
    }
}
