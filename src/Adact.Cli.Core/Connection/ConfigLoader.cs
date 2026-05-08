using System.Text.Json;

namespace Adact.Cli.Connection;

/// <summary>
/// </summary>
internal static class ConfigLoader
{
    private const string AdactDirName = ".adact";
    private const string ConfigFileName = "config.json";

    /// <summary>
    /// </summary>
    /// <returns>
    /// </returns>
    public static string? FindServerFromConfig(string startDir)
    {
        ArgumentNullException.ThrowIfNull(startDir);

        var dir = new DirectoryInfo(startDir);
        while (dir is not null)
        {
            var adactDir = Path.Combine(dir.FullName, AdactDirName);
            if (Directory.Exists(adactDir))
            {
                var configPath = Path.Combine(adactDir, ConfigFileName);
                if (File.Exists(configPath))
                {
                    return ReadServerField(configPath);
                }
                return null;
            }
            dir = dir.Parent;
        }
        return null;
    }

    /// <summary>
    /// </summary>
    private static string? ReadServerField(string configPath)
    {
        string text;
        try
        {
            text = File.ReadAllText(configPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new ConfigParseException(
                $"Failed to read config file '{configPath}': {ex.Message}", ex);
        }

        ConfigFile? cfg;
        try
        {
            cfg = JsonSerializer.Deserialize<ConfigFile>(text);
        }
        catch (JsonException ex)
        {
            throw new ConfigParseException(
                $"Failed to parse config file '{configPath}': {ex.Message}", ex);
        }

        var server = cfg?.Server;
        return string.IsNullOrWhiteSpace(server) ? null : server;
    }
}
