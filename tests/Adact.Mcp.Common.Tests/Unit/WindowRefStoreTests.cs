using Adact.Engine;
using Adact.Mcp.Common;

using Xunit;

namespace Adact.Mcp.Common.Tests.Unit;

[Trait("Layer", "Unit")]
public class WindowRefStoreTests
{
    private static WindowInfo Info(int pid, nint hwnd, string title = "Title")
        => new(pid, "TestProc", title, "Window", null, hwnd);

    private static WindowKey Key(int pid, nint hwnd)
        => new(hwnd, pid, DateTime.MinValue);

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

    [Fact]
    public void RetireMissing_RemovesNonPresentEntriesFromTryResolve()
    {
        var store = new WindowRefStore();
        var k1 = Key(100, 0x1000);
        var k2 = Key(200, 0x2000);
        var e1 = store.SyncOrAssign(k1, Info(100, 0x1000));
        var e2 = store.SyncOrAssign(k2, Info(200, 0x2000));

        // k2 のみ生存
        store.RetireMissing(new[] { k2 });

        Assert.False(store.TryResolve(e1.WindowRef, out _));
        Assert.True(store.TryResolve(e2.WindowRef, out var resolved));
        Assert.Equal(e2.WindowRef, resolved.WindowRef);
    }

    [Fact]
    public void RetireMissing_DoesNotReuseRetiredCounter()
    {
        var store = new WindowRefStore();
        var k1 = Key(100, 0x1000);
        var k2 = Key(200, 0x2000);

        var e1 = store.SyncOrAssign(k1, Info(100, 0x1000));
        Assert.Equal("w1", e1.WindowRef);

        store.RetireMissing(Array.Empty<WindowKey>()); // 全引退

        var e2 = store.SyncOrAssign(k2, Info(200, 0x2000));
        // w1 は再利用しない
        Assert.Equal("w2", e2.WindowRef);
    }

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

    [Fact]
    public void ListActive_ExcludesRetired()
    {
        var store = new WindowRefStore();
        var k1 = Key(100, 0x1000);
        var k2 = Key(200, 0x2000);
        store.SyncOrAssign(k1, Info(100, 0x1000));
        store.SyncOrAssign(k2, Info(200, 0x2000));

        store.RetireMissing(new[] { k1 }); // k2 を引退

        var active = store.ListActive();
        Assert.Single(active);
        Assert.Equal("w1", active[0].WindowRef);
    }

    [Fact]
    public void TryResolve_RetiredEntry_ReturnsFalse()
    {
        var store = new WindowRefStore();
        var k = Key(100, 0x1000);
        var entry = store.SyncOrAssign(k, Info(100, 0x1000));

        store.RetireMissing(Array.Empty<WindowKey>());

        Assert.False(store.TryResolve(entry.WindowRef, out _));
    }

    [Fact]
    public void SyncOrAssign_RevivesRetiredEntryWithSameWindowRef()
    {
        // 一度引退したあとに再度同じ key で list-apps されたら、同じ windowRef で復活する
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

    [Fact]
    public void TryResolve_UnknownWindowRef_ReturnsFalse()
    {
        var store = new WindowRefStore();
        Assert.False(store.TryResolve("w42", out _));
    }

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

    [Fact]
    public void TryFindByKey_UnknownKey_ReturnsFalse()
    {
        var store = new WindowRefStore();
        Assert.False(store.TryFindByKey(Key(999, 0x9999), out _));
    }

    [Fact]
    public void TryFindByKey_IncludesRetiredEntry()
    {
        // 引退済みも含めて検索可能であることを保証する (M-1 の idempotent 判定で使用)
        var store = new WindowRefStore();
        var k = Key(100, 0x1000);
        var assigned = store.SyncOrAssign(k, Info(100, 0x1000));
        store.RetireMissing(Array.Empty<WindowKey>());

        Assert.True(store.TryFindByKey(k, out var found));
        Assert.Equal(assigned.WindowRef, found.WindowRef);
        Assert.True(found.Retired);
    }
}
