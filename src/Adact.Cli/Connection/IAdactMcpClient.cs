using ModelContextProtocol.Protocol;

namespace Adact.Cli.Connection;

internal interface IAdactMcpClient : IAsyncDisposable
{
    ValueTask<CallToolResult> CallToolAsync(
        string name,
        IReadOnlyDictionary<string, object?>? arguments,
        CancellationToken cancellationToken);
}
