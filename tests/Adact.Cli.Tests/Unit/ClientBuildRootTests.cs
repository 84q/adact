using System.CommandLine;

using Adact.Cli.Commands;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Client Build Root behavior.</summary>
[Trait("Layer", "Unit")]
public class ClientBuildRootTests
{
    private static RootCommand BuildClientRoot()
    {
        var root = RootCommandRegistration.CreateRoot("ADACT - Cross-platform CLI Client");
        RootCommandRegistration.AddSharedCommands(root);
        RootCommandRegistration.AddInstallAndLaunchCommands(root, launchBeforeInstall: true);
        return root;
    }

    /// <summary>Performs the Build Root Returns Non Null Root Command operation.</summary>
    [Fact]
    public void BuildRoot_ReturnsNonNullRootCommand()
    {
        var root = BuildClientRoot();

        Assert.NotNull(root);
        Assert.IsType<RootCommand>(root);
    }

    /// <summary>Performs the Build Root Registers Expected Subcommand Count operation.</summary>
    [Fact]
    public void BuildRoot_RegistersExpectedSubcommandCount()
    {
        var root = BuildClientRoot();

        // AddSharedCommands (32) + install + launch (2) = 34
        Assert.Equal(34, root.Subcommands.Count);
    }

    /// <summary>Performs the Build Root Contains Expected Subcommand operation.</summary>
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

    /// <summary>Performs the Build Root Does Not Contain Server Only Commands operation.</summary>
    [Theory]
    [InlineData("serve")]
    [InlineData("daemon-stop")]
    public void BuildRoot_DoesNotContainServerOnlyCommands(string commandName)
    {
        var root = BuildClientRoot();

        Assert.DoesNotContain(root.Subcommands, c => c.Name == commandName);
    }
}
