using System.CommandLine;

using Adact.Cli.Commands;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// CLI Client の <c>BuildRoot()</c> 相当ロジックの Unit テスト。
/// Adact.Cli.Client は AssemblyName が Adact.Cli と衝突するため直接参照できない。
/// 同等の RootCommandRegistration 呼び出しを再現してサブコマンド登録を検証する。
/// </summary>
[Trait("Layer", "Unit")]
public class ClientBuildRootTests
{
    /// <summary>CLI Client 相当の BuildRoot が正常にルートコマンドを構築できることを確認する。</summary>
    private static RootCommand BuildClientRoot()
    {
        var root = RootCommandRegistration.CreateRoot("ADACT - Cross-platform CLI Client");
        RootCommandRegistration.AddSharedCommands(root);
        RootCommandRegistration.AddInstallAndLaunchCommands(root, launchBeforeInstall: true);
        return root;
    }

    /// <summary>BuildRoot() が null でない RootCommand を返すことを確認する。</summary>
    [Fact]
    public void BuildRoot_ReturnsNonNullRootCommand()
    {
        var root = BuildClientRoot();

        Assert.NotNull(root);
        Assert.IsType<RootCommand>(root);
    }

    /// <summary>
    /// BuildRoot() が RootCommandRegistration.AddSharedCommands + AddInstallAndLaunchCommands で
    /// 登録されるサブコマンド数を返すことを確認する。
    /// AddSharedCommands = 32 コマンド、AddInstallAndLaunchCommands = 2 コマンド → 合計 34。
    /// </summary>
    [Fact]
    public void BuildRoot_RegistersExpectedSubcommandCount()
    {
        var root = BuildClientRoot();

        // AddSharedCommands (32) + install + launch (2) = 34
        Assert.Equal(34, root.Subcommands.Count);
    }

    /// <summary>BuildRoot() が代表的なサブコマンドを含むことを確認する。</summary>
    [Theory]
    [InlineData("click")]
    [InlineData("fill")]
    [InlineData("snapshot")]
    [InlineData("attach")]
    [InlineData("launch")]
    [InlineData("install")]
    public void BuildRoot_ContainsExpectedSubcommand(string commandName)
    {
        var root = BuildClientRoot();

        Assert.Contains(root.Subcommands, c => c.Name == commandName);
    }

    /// <summary>CLI Client は serve / daemon-stop を含まないことを確認する。</summary>
    [Theory]
    [InlineData("serve")]
    [InlineData("daemon-stop")]
    public void BuildRoot_DoesNotContainServerOnlyCommands(string commandName)
    {
        var root = BuildClientRoot();

        Assert.DoesNotContain(root.Subcommands, c => c.Name == commandName);
    }
}
