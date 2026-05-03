using Adact.Cli.Connection;
using Adact.Cli.Daemon;
using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="DaemonSpawner"/> の Unit テスト。
/// パイプ存在確認・サーバー起動の分岐を検証する。
/// </summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class DaemonSpawnerTests
{
    /// <summary>
    /// パイプが存在し、応答する場合、true を返すことを確認する。
    /// </summary>
    [Fact]
    public async Task EnsureServerRunning_PipeExistsAndResponding_ReturnsTrue()
    {
        var origCheck = DaemonSpawner.IsServerRunningAsync;
        var origSpawn = DaemonSpawner.SpawnServerAsync;
        try
        {
            DaemonSpawner.IsServerRunningAsync = static (_, _, _) => Task.FromResult(true);
            var spawnCalled = false;
            DaemonSpawner.SpawnServerAsync = (_, _, _) =>
            {
                spawnCalled = true;
                return Task.FromResult(true);
            };

            var result = await DaemonSpawner.EnsureServerRunningAsync(CancellationToken.None);

            Assert.True(result);
            Assert.False(spawnCalled);
        }
        finally
        {
            DaemonSpawner.IsServerRunningAsync = origCheck;
            DaemonSpawner.SpawnServerAsync = origSpawn;
        }
    }

    /// <summary>
    /// パイプがない場合、サーバーを起動することを確認する。
    /// </summary>
    [Fact]
    public async Task EnsureServerRunning_PipeNotExists_SpawnsServer()
    {
        var origCheck = DaemonSpawner.IsServerRunningAsync;
        var origSpawn = DaemonSpawner.SpawnServerAsync;
        try
        {
            DaemonSpawner.IsServerRunningAsync = static (_, _, _) => Task.FromResult(false);
            var spawnCalled = false;
            DaemonSpawner.SpawnServerAsync = (_, _, _) =>
            {
                spawnCalled = true;
                return Task.FromResult(true);
            };

            var result = await DaemonSpawner.EnsureServerRunningAsync(CancellationToken.None);

            Assert.True(result);
            Assert.True(spawnCalled);
        }
        finally
        {
            DaemonSpawner.IsServerRunningAsync = origCheck;
            DaemonSpawner.SpawnServerAsync = origSpawn;
        }
    }

    /// <summary>
    /// パイプがあるが応答しない場合、サーバーを起動することを確認する。
    /// </summary>
    [Fact]
    public async Task EnsureServerRunning_PipeExistsButNoResponse_SpawnsServer()
    {
        var origCheck = DaemonSpawner.IsServerRunningAsync;
        var origSpawn = DaemonSpawner.SpawnServerAsync;
        try
        {
            // 1回目は true（パイプ存在）、2回目は false（応答なし）
            var callCount = 0;
            DaemonSpawner.IsServerRunningAsync = (_, _, _) =>
            {
                callCount++;
                return Task.FromResult(callCount == 1);
            };
            var spawnCalled = false;
            DaemonSpawner.SpawnServerAsync = (_, _, _) =>
            {
                spawnCalled = true;
                return Task.FromResult(true);
            };

            var result = await DaemonSpawner.EnsureServerRunningAsync(CancellationToken.None);

            Assert.True(result);
            Assert.True(spawnCalled);
            Assert.Equal(2, callCount);
        }
        finally
        {
            DaemonSpawner.IsServerRunningAsync = origCheck;
            DaemonSpawner.SpawnServerAsync = origSpawn;
        }
    }
}
