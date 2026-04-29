using Xunit;

namespace Adact.Mcp.Stdio.Tests;

/// <summary>
/// UIA を使う Stdio テストを直列実行するための xUnit collection 定義。
/// </summary>
[CollectionDefinition("UiaSerial", DisableParallelization = true)]
public class UiaSerialCollection { }

/// <summary>
/// adact.exe のパスを解決するヘルパー。
/// </summary>
internal static class AdactExePath
{
    /// <summary>
    /// テストアセンブリの bin ディレクトリから相対的に Adact.Cli の出力 exe (adact.exe) を解決する。
    /// 環境変数 ADACT_EXE が設定されていればそれを優先する。
    /// </summary>
    /// <returns>解決された adact.exe の絶対パス。</returns>
    /// <exception cref="FileNotFoundException">adact.exe が見つからない場合。</exception>
    public static string Resolve()
    {
        var env = Environment.GetEnvironmentVariable("ADACT_EXE");
        if (!string.IsNullOrEmpty(env) && File.Exists(env)) return env;

        // tests/Adact.Mcp.Stdio.Tests/bin/Debug/net10.0-windows/ -> ../../../../../src/Adact.Cli/bin/<config>/<tfm>/adact.exe
        var asmDir = Path.GetDirectoryName(typeof(AdactExePath).Assembly.Location)!;
        var tfm = new DirectoryInfo(asmDir).Name;          // net10.0-windows
        var config = new DirectoryInfo(asmDir).Parent!.Name; // Debug or Release
        var repoRoot = Path.GetFullPath(Path.Combine(asmDir, "..", "..", "..", "..", ".."));
        var exe = Path.Combine(repoRoot, "src", "Adact.Cli", "bin", config, tfm, "adact.exe");
        if (!File.Exists(exe))
            throw new FileNotFoundException($"Could not find adact.exe at {exe}. Build Adact.Cli first.");
        return exe;
    }
}
