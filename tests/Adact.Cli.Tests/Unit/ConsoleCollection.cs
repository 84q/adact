using Xunit;

namespace Adact.Cli.Tests.Unit;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ConsoleCollection
{
    public const string Name = "Console";
}