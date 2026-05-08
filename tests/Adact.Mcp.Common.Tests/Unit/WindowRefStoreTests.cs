using Adact.Engine;

using Xunit;

namespace Adact.Mcp.Common.Tests.Unit;

/// <summary>Contains tests for the Window Ref Store behavior.</summary>
[Trait("Layer", "Unit")]
public class WindowRefStoreTests
{
    private static WindowInfo Info(int pid, nint hwnd, string title = "Title")
        => new(pid, "TestProc", title, "Window", null, hwnd);

    private static WindowKey Key(int pid, nint hwnd)
        => new(hwnd, pid, DateTime.MinValue);

    /// <summary>Performs the Sync Or Assign Same Key Returns Same Window Ref operation.</summary>
    [Fact]
    public void SyncOrAssign_SameKey_ReturnsSameWindowRef()
    {
        var store = new WindowRefStore();
        var k = Key(100, 0x1000);
        var info = Info(100, 0x1000);

        var first = store.SyncOrAssign(k, info);
        var second = store.SyncOrAssign(k, info);

        Assert.Equal("w1", first.WindowRef);
        Assert.Equal(first.WindowRef, second.WindowRef);
    }

    /// <summary>Performs the Sync Or Assign Different Keys Assigns Sequential Refs operation.</summary>
    [Fact]
    public void SyncOrAssign_DifferentKeys_AssignsSequentialRefs()
    {
        var store = new WindowRefStore();

        var a = store.SyncOrAssign(Key(100, 0x1000), Info(100, 0x1000));
        var b = store.SyncOrAssign(Key(200, 0x2000), Info(200, 0x2000));
        var c = store.SyncOrAssign(Key(300, 0x3000), Info(300, 0x3000));

        Assert.Equal("w1", a.WindowRef);
        Assert.Equal("w2", b.WindowRef);
        Assert.Equal("w3", c.WindowRef);
    }

    /// <summary>Performs the Retire Missing Removes Non Present Entries From Try Resolve operation.</summary>
    [Fact]
    public void RetireMissing_RemovesNonPresentEntriesFromTryResolve()
    {
        var store = new WindowRefStore();
        var k1 = Key(100, 0x1000);
        var k2 = Key(200, 0x2000);
        var e1 = store.SyncOrAssign(k1, Info(100, 0x1000));
        var e2 = store.SyncOrAssign(k2, Info(200, 0x2000));

        store.RetireMissing(new[] { k2 });

        Assert.False(store.TryResolve(e1.WindowRef, out _));
        Assert.True(store.TryResolve(e2.WindowRef, out var resolved));
        Assert.Equal(e2.WindowRef, resolved.WindowRef);
    }

    /// <summary>Performs the Retire Missing Does Not Reuse Retired Counter operation.</summary>
    [Fact]
    public void RetireMissing_DoesNotReuseRetiredCounter()
    {
        var store = new WindowRefStore();
        var k1 = Key(100, 0x1000);
        var k2 = Key(200, 0x2000);

        var e1 = store.SyncOrAssign(k1, Info(100, 0x1000));
        Assert.Equal("w1", e1.WindowRef);


        var e2 = store.SyncOrAssign(k2, Info(200, 0x2000));
        Assert.Equal("w2", e2.WindowRef);
    }

    /// <summary>Performs the Associate And Clear Session Update Entry operation.</summary>
    [Fact]
    public void AssociateAndClearSession_UpdateEntry()
    {
        var store = new WindowRefStore();
        var k = Key(100, 0x1000);
        var entry = store.SyncOrAssign(k, Info(100, 0x1000));

        store.AssociateSession(entry.WindowRef, "s1");
        Assert.True(store.TryResolve(entry.WindowRef, out var withSession));
        Assert.Equal("s1", withSession.SessionId);

        store.ClearSession(entry.WindowRef);
        Assert.True(store.TryResolve(entry.WindowRef, out var cleared));
        Assert.Null(cleared.SessionId);
    }

    /// <summary>Performs the List Active Excludes Retired operation.</summary>
    [Fact]
    public void ListActive_ExcludesRetired()
    {
        var store = new WindowRefStore();
        var k1 = Key(100, 0x1000);
        var k2 = Key(200, 0x2000);
        store.SyncOrAssign(k1, Info(100, 0x1000));
        store.SyncOrAssign(k2, Info(200, 0x2000));
        store.RetireMissing(new[] { k1 });

        var active = store.ListActive();
        Assert.Single(active);
        Assert.Equal("w1", active[0].WindowRef);
    }

    /// <summary>Attempts to perform the Try Resolve Retired Entry Returns False operation.</summary>
    [Fact]
    public void TryResolve_RetiredEntry_ReturnsFalse()
    {
        var store = new WindowRefStore();
        var k = Key(100, 0x1000);
        var entry = store.SyncOrAssign(k, Info(100, 0x1000));

        store.RetireMissing(Array.Empty<WindowKey>());

        Assert.False(store.TryResolve(entry.WindowRef, out _));
    }

    /// <summary>Performs the Sync Or Assign Revives Retired Entry With Same Window Ref operation.</summary>
    [Fact]
    public void SyncOrAssign_RevivesRetiredEntryWithSameWindowRef()
    {
        var store = new WindowRefStore();
        var k = Key(100, 0x1000);
        var first = store.SyncOrAssign(k, Info(100, 0x1000));

        store.RetireMissing(Array.Empty<WindowKey>());
        Assert.False(store.TryResolve(first.WindowRef, out _));

        var revived = store.SyncOrAssign(k, Info(100, 0x1000));
        Assert.Equal(first.WindowRef, revived.WindowRef);
        Assert.False(revived.Retired);
        Assert.True(store.TryResolve(first.WindowRef, out _));
    }

    /// <summary>Attempts to perform the Try Resolve Unknown Window Ref Returns False operation.</summary>
    [Fact]
    public void TryResolve_UnknownWindowRef_ReturnsFalse()
    {
        var store = new WindowRefStore();
        Assert.False(store.TryResolve("w42", out _));
    }

    /// <summary>Attempts to perform the Try Find By Key Returns Existing Entry For Same Key operation.</summary>
    [Fact]
    public void TryFindByKey_ReturnsExistingEntryForSameKey()
    {
        var store = new WindowRefStore();
        var k = Key(100, 0x1000);
        var info = Info(100, 0x1000);
        var assigned = store.SyncOrAssign(k, info);

        Assert.True(store.TryFindByKey(k, out var found));
        Assert.Equal(assigned.WindowRef, found.WindowRef);
        Assert.Equal(k, found.Key);
        Assert.False(found.Retired);
    }

    /// <summary>Attempts to perform the Try Find By Key Unknown Key Returns False operation.</summary>
    [Fact]
    public void TryFindByKey_UnknownKey_ReturnsFalse()
    {
        var store = new WindowRefStore();
        Assert.False(store.TryFindByKey(Key(999, 0x9999), out _));
    }

    /// <summary>Attempts to perform the Try Find By Key Includes Retired Entry operation.</summary>
    [Fact]
    public void TryFindByKey_IncludesRetiredEntry()
    {
        var store = new WindowRefStore();
        var k = Key(100, 0x1000);
        var assigned = store.SyncOrAssign(k, Info(100, 0x1000));
        store.RetireMissing(Array.Empty<WindowKey>());

        Assert.True(store.TryFindByKey(k, out var found));
        Assert.Equal(assigned.WindowRef, found.WindowRef);
        Assert.True(found.Retired);
    }

    /// <summary>Attempts to perform the Try Find By Session Id Returns Matching Entry operation.</summary>
    [Fact]
    public void TryFindBySessionId_ReturnsMatchingEntry()
    {
        var store = new WindowRefStore();
        var k = Key(100, 0x1000);
        var entry = store.SyncOrAssign(k, Info(100, 0x1000));
        store.AssociateSession(entry.WindowRef, "s1");

        Assert.True(store.TryFindBySessionId("s1", out var found));
        Assert.Equal(entry.WindowRef, found.WindowRef);
        Assert.Equal("s1", found.SessionId);
    }

    /// <summary>Attempts to perform the Try Find By Session Id After Clear Session Returns False operation.</summary>
    [Fact]
    public void TryFindBySessionId_AfterClearSession_ReturnsFalse()
    {
        var store = new WindowRefStore();
        var k = Key(100, 0x1000);
        var entry = store.SyncOrAssign(k, Info(100, 0x1000));
        store.AssociateSession(entry.WindowRef, "s1");
        store.ClearSession(entry.WindowRef);

        Assert.False(store.TryFindBySessionId("s1", out _));
    }

    /// <summary>Attempts to perform the Try Find By Session Id Retired Entry Excluded operation.</summary>
    [Fact]
    public void TryFindBySessionId_RetiredEntryExcluded()
    {
        var store = new WindowRefStore();
        var k = Key(100, 0x1000);
        var entry = store.SyncOrAssign(k, Info(100, 0x1000));
        store.AssociateSession(entry.WindowRef, "s1");

        store.RetireMissing(Array.Empty<WindowKey>());

        Assert.False(store.TryFindBySessionId("s1", out _));
    }

    /// <summary>Attempts to perform the Try Find By Session Id Unknown Session Returns False operation.</summary>
    [Fact]
    public void TryFindBySessionId_UnknownSession_ReturnsFalse()
    {
        var store = new WindowRefStore();
        Assert.False(store.TryFindBySessionId("s99", out _));
    }

    /// <summary>Performs the Remove By Session Id Removes Associated Entry operation.</summary>
    [Fact]
    public void RemoveBySessionId_RemovesAssociatedEntry()
    {
        var store = new WindowRefStore();
        var k = Key(100, 0x1000);
        var entry = store.SyncOrAssign(k, Info(100, 0x1000));
        store.AssociateSession(entry.WindowRef, "s1");

        store.RemoveBySessionId("s1");

        Assert.False(store.TryResolve(entry.WindowRef, out _));
        Assert.False(store.TryFindByKey(k, out _));
    }

    /// <summary>Performs the Purge Expired Retired Entries Removes Only Expired Retired Entries operation.</summary>
    [Fact]
    public void PurgeExpiredRetiredEntries_RemovesOnlyExpiredRetiredEntries()
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var store = new WindowRefStore(
            retiredEntryTtl: TimeSpan.FromMinutes(5),
            utcNow: () => now);

        var expiredKey = Key(100, 0x1000);
        var liveKey = Key(200, 0x2000);
        var expired = store.SyncOrAssign(expiredKey, Info(100, 0x1000));
        store.SyncOrAssign(liveKey, Info(200, 0x2000));

        store.RetireMissing(new[] { liveKey });
        now = now.AddMinutes(6);

        var purged = store.PurgeExpiredRetiredEntries();

        Assert.Equal(1, purged);
        Assert.False(store.TryFindByKey(expiredKey, out _));
        Assert.True(store.TryFindByKey(liveKey, out _));
        Assert.False(store.TryResolve(expired.WindowRef, out _));
    }

    /// <summary>Performs the Sync Or Assign Concurrent Access All Refs Unique operation.</summary>
    [Fact]
    public async Task SyncOrAssign_ConcurrentAccess_AllRefsUnique()
    {
        var store = new WindowRefStore();
        const int count = 100;

        var results = new WindowRefEntry[count];
        var tasks = Enumerable.Range(0, count).Select(i => Task.Run(() =>
        {
            var k = Key(1000 + i, (nint)(0x1000 + i));
            var info = Info(1000 + i, (nint)(0x1000 + i));
            results[i] = store.SyncOrAssign(k, info);
        })).ToArray();
        await Task.WhenAll(tasks);

        var refs = results.Select(r => r.WindowRef).ToHashSet();
        Assert.Equal(count, refs.Count);
    }

    /// <summary>Performs the Sync Or Assign Concurrent Duplicate Key Returns Same Ref operation.</summary>
    [Fact]
    public async Task SyncOrAssign_ConcurrentDuplicateKey_ReturnsSameRef()
    {
        var store = new WindowRefStore();
        var k = Key(100, 0x1000);
        var info = Info(100, 0x1000);
        const int count = 100;

        var results = new WindowRefEntry[count];
        var tasks = Enumerable.Range(0, count).Select(i => Task.Run(() =>
        {
            results[i] = store.SyncOrAssign(k, info);
        })).ToArray();
        await Task.WhenAll(tasks);

        var distinctRefs = results.Select(r => r.WindowRef).Distinct().ToList();
        Assert.Single(distinctRefs);
    }
}
