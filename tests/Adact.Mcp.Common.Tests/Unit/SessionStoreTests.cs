using Adact.Engine;

using System.Reflection;

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
    public void Dispose_DoesNotDisposeEngine()
    {
        var engine = new UiaEngine();
        var store = new SessionStore(engine);

        store.Dispose();

        Assert.Equal(0, ReadDisposedFlag(engine));

        engine.Dispose();
        Assert.Equal(1, ReadDisposedFlag(engine));
    }

    private static int ReadDisposedFlag(UiaEngine engine)
    {
        var field = typeof(UiaEngine).GetField("_disposed", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (int)field!.GetValue(engine)!;
    }
}
