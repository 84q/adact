using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Defines a shared test collection.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ConsoleCollection
{
    /// <summary>Gets the Name value.</summary>
    public const string Name = "Console";
}
