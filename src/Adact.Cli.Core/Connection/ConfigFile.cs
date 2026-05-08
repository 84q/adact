using System.Text.Json.Serialization;

namespace Adact.Cli.Connection;

/// <summary>
/// </summary>
internal sealed record ConfigFile
{
    [JsonPropertyName("server")]
    public string? Server { get; init; }
}
