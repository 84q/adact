using System.Text.Json.Serialization;

namespace Adact.Cli.Connection;

/// <summary>
/// .adact/config.json の deserialize 用 record。Phase 5 では <c>server</c> のみ扱う (009 設計 §3.3)。
/// </summary>
internal sealed record ConfigFile
{
    [JsonPropertyName("server")]
    public string? Server { get; init; }
}
