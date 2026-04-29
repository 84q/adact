using System.Diagnostics;

using Xunit;

namespace Adact.Engine.Tests.Unit;

[Trait("Layer", "Unit")]
public class UiaEngineSerializationTests
{
    /// <summary>
    /// 2 つの Task を同時起動し、1 つ目の action が完了するまで 2 つ目の action が
    /// 開始されないことを検証する。SemaphoreSlim(1, 1) ベースの直列化が機能していれば
    /// 2 つ目の開始時刻 ≧ 1 つ目の完了時刻 となる。
    /// </summary>
    [Fact]
    public async Task RunSerializedAsync_TwoConcurrentCalls_AreSerialized()
    {
        // Engine は実 UIA に触らない (RunSerializedAsync は gate のみで UIA は呼ばない)
        // ため automation を実体化しても問題ないが、構築コストを避けるため using で破棄。
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

        // t1 が gate を取得したことを確認してから t2 を起動する
        await firstStartedTcs.Task;

        var t2 = engine.RunSerializedAsync(_ =>
        {
            secondStartTicks = Stopwatch.GetTimestamp();
            return Task.FromResult(0);
        }, CancellationToken.None);

        // この時点で t2 は gate 待ちでブロックされているはず
        Assert.False(t2.IsCompleted);

        releaseFirstTcs.SetResult();
        await Task.WhenAll(t1, t2);

        Assert.NotNull(firstEndTicks);
        Assert.NotNull(secondStartTicks);
        // 直列化されていれば second の開始は first の完了以降
        Assert.True(
            secondStartTicks!.Value >= firstEndTicks!.Value,
            $"second start ({secondStartTicks}) must be >= first end ({firstEndTicks})");
    }

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

        // t1 を解放して engine が以後も健全であることを確認
        releaseFirstTcs.SetResult();
        await t1;
    }

    [Fact]
    public async Task RunSerializedAsync_Exception_ReleasesGate()
    {
        using var engine = new UiaEngine();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            engine.RunSerializedAsync<int>(_ => throw new InvalidOperationException("boom"), CancellationToken.None));

        // 例外で gate が release されないと次の呼び出しは無限に待機する。
        // タイムアウト付きで動くことを確認する。
        var next = engine.RunSerializedAsync(_ => Task.FromResult(42), CancellationToken.None);
        var completed = await Task.WhenAny(next, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.Same(next, completed);
        Assert.Equal(42, await next);
    }
}
