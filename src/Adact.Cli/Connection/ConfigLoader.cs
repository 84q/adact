using System.Text.Json;

namespace Adact.Cli.Connection;

/// <summary>
/// 接続先設定ファイル (.adact/config.json) の探索とパースを担当する。
/// 設計 009 §3.1 / §3.3。
/// </summary>
internal static class ConfigLoader
{
    private const string AdactDirName = ".adact";
    private const string ConfigFileName = "config.json";

    /// <summary>
    /// <paramref name="startDir"/> から親ディレクトリへ向かって <c>.adact/config.json</c> を再帰探索し、
    /// 最初に見つかった file の <c>server</c> フィールドを返す。
    /// </summary>
    /// <param name="startDir">探索開始ディレクトリ (通常は cwd)。</param>
    /// <returns>
    /// 見つかった server 文字列。<c>.adact/</c> が存在しない、ファイルが存在しない、
    /// もしくは <c>server</c> が null/空文字なら null。
    /// </returns>
    /// <exception cref="ConfigParseException">JSON parse / IO 失敗時。</exception>
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
                // .adact/ が見つかったらそこで停止 (git 流の探索打ち切り) し、
                // config.json 不在なら null fallback。
                return null;
            }
            dir = dir.Parent;
        }
        return null;
    }

    /// <summary>
    /// 指定パスの config.json を読み、<c>server</c> フィールドを取り出す。
    /// </summary>
    /// <param name="configPath">読み込み対象のファイル絶対パス。</param>
    /// <returns><c>server</c> フィールドの値。未設定/空文字列のときは null。</returns>
    /// <exception cref="ConfigParseException">IO もしくは JSON parse に失敗した場合。</exception>
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
