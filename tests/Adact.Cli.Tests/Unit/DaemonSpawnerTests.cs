using Adact.Cli.Connection;
using Adact.Cli.Daemon;
using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Daemon Spawner behavior.</summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class DaemonSpawnerTests
{
    /// <summary>Performs the Ensure Server Running Pipe Exists And Responding Returns True operation.</summary>
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

    /// <summary>Performs the Ensure Server Running Pipe Not Exists Spawns Server operation.</summary>
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

    /// <summary>Performs the Ensure Server Running Pipe Exists But No Response Spawns Server operation.</summary>
    [Fact]
    public async Task EnsureServerRunning_PipeExistsButNoResponse_SpawnsServer()
    {
        var origCheck = DaemonSpawner.IsServerRunningAsync;
        var origSpawn = DaemonSpawner.SpawnServerAsync;
        try
        {
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
