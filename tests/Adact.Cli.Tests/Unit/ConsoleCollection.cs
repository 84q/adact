using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// Console.Out / Console.Error をグローバルにリダイレクトするテストを直列実行するための xUnit collection。
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ConsoleCollection
{
    /// <summary>collection 名コード。<c>[Collection(...)]</c> で参照される。</summary>
    public const string Name = "Console";
}