using Adact.Engine;

using Xunit;

namespace Adact.Mcp.Common.Tests.Unit;

/// <summary>Contains tests for the Session Store behavior.</summary>
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

    /// <summary>Performs the Register Sets Active Session And Allows Lookup operation.</summary>
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

    /// <summary>Performs the Register Second Session Replaces Active Session operation.</summary>
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

    /// <summary>Resolves the Resolve By Ref Returns Owning Session value.</summary>
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

    /// <summary>Attempts to perform the Try Remove Active Session Removes And Clears Active operation.</summary>
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

    /// <summary>Attempts to perform the Try Remove Non Active Session Keeps Current Active operation.</summary>
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

    /// <summary>Attempts to perform the Try Remove Unknown Session Returns False operation.</summary>
    [Fact]
    public void TryRemove_UnknownSession_ReturnsFalse()
    {
        using var store = new SessionStore(new UiaEngine());

        var removed = store.TryRemove("s99", out var removedSession);

        Assert.False(removed);
        Assert.Null(removedSession);
    }

    /// <summary>Performs the List All Returns Snapshot Of Current Sessions operation.</summary>
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

    /// <summary>Performs the Acquire Async Serializes Access Until Guard Is Disposed operation.</summary>
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

    /// <summary>Performs the Dispose Can Be Called More Than Once operation.</summary>
    [Fact]
    public void Dispose_CanBeCalledMoreThanOnce()
    {
        var store = new SessionStore(new UiaEngine());
        store.Register(Session(1));

        store.Dispose();
        store.Dispose();

        Assert.Empty(store.ListAll());
    }

    /// <summary>Performs the Dispose Does Not Dispose Engine operation.</summary>
    [Fact]
    public async Task Dispose_DoesNotDisposeEngine()
    {
        var engine = new UiaEngine();
        var store = new SessionStore(engine);

        store.Dispose();

        var ex = await Record.ExceptionAsync(() => engine.ListWindowsAsync());
        Assert.False(ex is ObjectDisposedException,
            "SessionStore.Dispose should not dispose the injected UiaEngine.");

        engine.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => engine.ListWindowsAsync());
    }

    /// <summary>Performs the Register Concurrent Access All Sessions Registered operation.</summary>
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

    /// <summary>Attempts to perform the Try Remove Concurrent Access No Data Corruption operation.</summary>
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
