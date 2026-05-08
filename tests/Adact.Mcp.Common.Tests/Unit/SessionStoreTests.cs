using Adact.Engine;

using Xunit;

namespace Adact.Mcp.Common.Tests.Unit;

/// <summary>
/// Verifies SessionStore registration, active session, lookup, removal, and locking contracts.
/// </summary>
[Trait("Layer", "Unit")]
public class SessionStoreTests
{
    private static WindowInfo Info(int id)
        => new(
            ProcessId: 1000 + id,
            ProcessName: $"fake{id}",
            Title: $"Fake {id}",
            ControlType: "Window",
            ClassName: null,
            NativeWindowHandle: 0x1000 + id);

    private static WindowSession Session(int id)
        => WindowSession.CreateForTest(id, Info(id));

    /// <summary>
    /// Register stores a session and makes it active.
    /// </summary>
    [Fact]
    public void Register_SetsActiveSessionAndAllowsLookup()
    {
        using var store = new SessionStore(new UiaEngine());
        var session = Session(1);

        store.Register(session);

        Assert.Equal("s1", store.ActiveSessionId);
        Assert.True(store.TryGet("s1", out var resolved));
        Assert.Same(session, resolved);
        Assert.Same(session, store.GetActiveOrNull());
    }

    /// <summary>
    /// Registering another session switches the active session without removing the first.
    /// </summary>
    [Fact]
    public void Register_SecondSession_ReplacesActiveSession()
    {
        using var store = new SessionStore(new UiaEngine());
        var first = Session(1);
        var second = Session(2);

        store.Register(first);
        store.Register(second);

        Assert.Equal("s2", store.ActiveSessionId);
        Assert.Same(second, store.GetActiveOrNull());
        Assert.True(store.TryGet("s1", out var resolvedFirst));
        Assert.Same(first, resolvedFirst);
    }

    /// <summary>
    /// ResolveByRef maps valid ref ids to their owning session and rejects invalid refs.
    /// </summary>
    [Fact]
    public void ResolveByRef_ReturnsOwningSession()
    {
        using var store = new SessionStore(new UiaEngine());
        var session = Session(3);
        store.Register(session);

        Assert.Same(session, store.ResolveByRef("s3e42"));
        Assert.Null(store.ResolveByRef("not-a-ref"));
        Assert.Null(store.ResolveByRef("s99e1"));
    }

    /// <summary>
    /// Removing the active session clears ActiveSessionId and lookup state.
    /// </summary>
    [Fact]
    public void TryRemove_ActiveSession_RemovesAndClearsActive()
    {
        using var store = new SessionStore(new UiaEngine());
        var session = Session(1);
        store.Register(session);

        var removed = store.TryRemove("s1", out var removedSession);

        Assert.True(removed);
        Assert.Same(session, removedSession);
        Assert.Null(store.ActiveSessionId);
        Assert.Null(store.GetActiveOrNull());
        Assert.False(store.TryGet("s1", out _));
    }

    /// <summary>
    /// Removing a non-active session leaves the current active session unchanged.
    /// </summary>
    [Fact]
    public void TryRemove_NonActiveSession_KeepsCurrentActive()
    {
        using var store = new SessionStore(new UiaEngine());
        var first = Session(1);
        var second = Session(2);
        store.Register(first);
        store.Register(second);

        var removed = store.TryRemove("s1", out var removedSession);

        Assert.True(removed);
        Assert.Same(first, removedSession);
        Assert.Equal("s2", store.ActiveSessionId);
        Assert.Same(second, store.GetActiveOrNull());
    }

    /// <summary>
    /// Removing an unknown session returns false and no removed session.
    /// </summary>
    [Fact]
    public void TryRemove_UnknownSession_ReturnsFalse()
    {
        using var store = new SessionStore(new UiaEngine());

        var removed = store.TryRemove("s99", out var removedSession);

        Assert.False(removed);
        Assert.Null(removedSession);
    }

    /// <summary>
    /// ListAll returns the currently registered sessions.
    /// </summary>
    [Fact]
    public void ListAll_ReturnsSnapshotOfCurrentSessions()
    {
        using var store = new SessionStore(new UiaEngine());
        var first = Session(1);
        var second = Session(2);
        store.Register(first);
        store.Register(second);

        var sessions = store.ListAll();

        Assert.Equal(2, sessions.Count);
        Assert.Contains(sessions, e => e.Key == "s1" && ReferenceEquals(e.Value, first));
        Assert.Contains(sessions, e => e.Key == "s2" && ReferenceEquals(e.Value, second));
    }

    /// <summary>
    /// AcquireAsync serializes access until the returned guard is disposed.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_SerializesAccessUntilGuardIsDisposed()
    {
        using var store = new SessionStore(new UiaEngine());
        using var firstGuard = await store.AcquireAsync(CancellationToken.None);
        var secondAcquire = store.AcquireAsync(CancellationToken.None);

        Assert.False(secondAcquire.IsCompleted);

        firstGuard.Dispose();
        using var secondGuard = await secondAcquire.WaitAsync(TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Dispose clears sessions and is idempotent.
    /// </summary>
    [Fact]
    public void Dispose_CanBeCalledMoreThanOnce()
    {
        var store = new SessionStore(new UiaEngine());
        store.Register(Session(1));

        store.Dispose();
        store.Dispose();

        Assert.Empty(store.ListAll());
    }

    /// <summary>
    /// SessionStore.Dispose は利用中の UiaEngine を破棄せず、所有権を呼び出し側へ残すことを確認する。
    /// </summary>
    [Fact]
    public async Task Dispose_DoesNotDisposeEngine()
    {
        var engine = new UiaEngine();
        var store = new SessionStore(engine);

        store.Dispose();

        // Engine が Dispose されていなければ ThrowIfDisposed() を通過する。
        // ListWindowsAsync は ThrowIfDisposed() を最初に呼ぶため、
        // ObjectDisposedException が出なければ Engine は生存している。
        var ex = await Record.ExceptionAsync(() => engine.ListWindowsAsync());
        Assert.False(ex is ObjectDisposedException,
            "SessionStore.Dispose should not dispose the injected UiaEngine.");

        engine.Dispose();

        // 明示 Dispose 後は ObjectDisposedException になること。
        await Assert.ThrowsAsync<ObjectDisposedException>(() => engine.ListWindowsAsync());
    }

    /// <summary>
    /// 複数タスクから同時に Register してもすべてのセッションが登録されることを確認する。
    /// </summary>
    [Fact]
    public async Task Register_ConcurrentAccess_AllSessionsRegistered()
    {
        using var store = new SessionStore(new UiaEngine());
        const int count = 100;

        var tasks = Enumerable.Range(1, count).Select(i => Task.Run(() =>
        {
            store.Register(Session(i));
        })).ToArray();
        await Task.WhenAll(tasks);

        var all = store.ListAll();
        Assert.Equal(count, all.Count);
    }

    /// <summary>
    /// 複数タスクから同時に TryRemove してもデータ不整合が起きないことを確認する。
    /// </summary>
    [Fact]
    public async Task TryRemove_ConcurrentAccess_NoDataCorruption()
    {
        using var store = new SessionStore(new UiaEngine());
        const int count = 100;

        for (int i = 1; i <= count; i++)
            store.Register(Session(i));

        var tasks = Enumerable.Range(1, count).Select(i => Task.Run(() =>
        {
            store.TryRemove($"s{i}", out _);
        })).ToArray();
        await Task.WhenAll(tasks);

        Assert.Empty(store.ListAll());
    }
}
