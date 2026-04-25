using System.Reflection;
using Xunit;

namespace Adact.Mcp.Stdio.Tests;

[CollectionDefinition("UiaSerial", DisableParallelization = true)]
public class UiaSerialCollection { }

internal static class AdactExePath
{
  /// <summary>
  /// テストアセンブリの bin ディレクトリから相対的に Adact.Cli の出力 exe (adact.exe) を解決する。
  /// 環境変数 ADACT_EXE が設定されていればそれを優先する。
  /// </summary>
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
