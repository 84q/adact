using System.Diagnostics;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>Contains tests for the Uia Engine Serialization behavior.</summary>
[Trait("Layer", "Unit")]
public class UiaEngineSerializationTests
{
    /// <summary>Performs the Run Serialized Async Two Concurrent Calls Are Serialized operation.</summary>
    [Fact]
    public async Task RunSerializedAsync_TwoConcurrentCalls_AreSerialized()
    {
        using var engine = new UiaEngine();

        var firstStartedTcs = new TaskCompletionSource();
        var releaseFirstTcs = new TaskCompletionSource();

        long? firstEndTicks = null;
        long? secondStartTicks = null;

        var t1 = engine.RunSerializedAsync(async _ =>
        {
            firstStartedTcs.SetResult();
            await releaseFirstTcs.Task.ConfigureAwait(false);
            firstEndTicks = Stopwatch.GetTimestamp();
            return 0;
        }, CancellationToken.None);

        await firstStartedTcs.Task;

        var t2 = engine.RunSerializedAsync(_ =>
        {
            secondStartTicks = Stopwatch.GetTimestamp();
            return Task.FromResult(0);
        }, CancellationToken.None);

        Assert.False(t2.IsCompleted);

        releaseFirstTcs.SetResult();
        await Task.WhenAll(t1, t2);

        Assert.NotNull(firstEndTicks);
        Assert.NotNull(secondStartTicks);
        Assert.True(
            secondStartTicks!.Value >= firstEndTicks!.Value,
            $"second start ({secondStartTicks}) must be >= first end ({firstEndTicks})");
    }

    /// <summary>Performs the Run Serialized Async Cancellation Honours Cancellation Token operation.</summary>
    [Fact]
    public async Task RunSerializedAsync_Cancellation_HonoursCancellationToken()
    {
        using var engine = new UiaEngine();

        var firstStartedTcs = new TaskCompletionSource();
        var releaseFirstTcs = new TaskCompletionSource();

        var t1 = engine.RunSerializedAsync(async _ =>
        {
            firstStartedTcs.SetResult();
            await releaseFirstTcs.Task.ConfigureAwait(false);
            return 0;
        }, CancellationToken.None);

        await firstStartedTcs.Task;

        using var cts = new CancellationTokenSource();
        var t2 = engine.RunSerializedAsync(_ => Task.FromResult(0), cts.Token);

        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t2);

        releaseFirstTcs.SetResult();
        await t1;
    }

    /// <summary>Performs the Run Serialized Async Exception Releases Gate operation.</summary>
    [Fact]
    public async Task RunSerializedAsync_Exception_ReleasesGate()
    {
        using var engine = new UiaEngine();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            engine.RunSerializedAsync<int>(_ => throw new InvalidOperationException("boom"), CancellationToken.None));

        var next = engine.RunSerializedAsync(_ => Task.FromResult(42), CancellationToken.None);
        var completed = await Task.WhenAny(next, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.Same(next, completed);
        Assert.Equal(42, await next);
    }
}
