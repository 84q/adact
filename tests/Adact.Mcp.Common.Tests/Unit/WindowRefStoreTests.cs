using Adact.Engine;

using Xunit;

namespace Adact.Mcp.Common.Tests.Unit;

/// <summary>
/// <see cref="WindowRefStore"/> の windowRef 採番・引退・session 紐付け・検索ロジックを検証する Unit テスト。
/// MCP 経由で公開される windowRef の安定性 (ref-ids 仕様) と list-apps の引退検知が回帰しないことを担保するため。
/// </summary>
[Trait("Layer", "Unit")]
public class WindowRefStoreTests
{
    private static WindowInfo Info(int pid, nint hwnd, string title = "Title")
        => new(pid, "TestProc", title, "Window", null, hwnd);

    private static WindowKey Key(int pid, nint hwnd)
        => new(hwnd, pid, DateTime.MinValue);

    /// <summary>
    /// 同一 <see cref="WindowKey"/> を続けて SyncOrAssign したとき、同じ windowRef ("w1") が返ることを確認する。
    /// list-apps 連続呼び出しで ref が振り直されない仕様 (ref-ids §安定性) の回帰防止。
    /// </summary>
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

    /// <summary>
    /// 異なる WindowKey を順に SyncOrAssign した際、windowRef が w1, w2, w3 と連番採番されることを確認する。
    /// 採番順序がユーザー視点で予測可能であることを保証するため。
    /// </summary>
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

    /// <summary>
    /// 引退対象 (RetireMissing で渡されない key) のエントリは TryResolve できなくなることを確認する。
    /// list-apps 後に閉じられたウィンドウへの ref が解決されない仕様の回帰防止。
    /// </summary>
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

    /// <summary>
    /// 引退エントリの windowRef 番号は再利用せず、新規エントリには次の番号 (w2) が振られることを確認する。
    /// 番号衝突によるユーザーの混乱を防ぐため。
    /// </summary>
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

    /// <summary>
    /// AssociateSession / ClearSession でエントリの SessionId が正しく更新・解除されることを確認する。
    /// attach/detach 時の windowRef↔sessionId 双方向リンク管理が破綻しないようにするため。
    /// </summary>
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

    /// <summary>
    /// ListActive が引退エントリを除外し、生存中のエントリのみ返すことを確認する。
    /// list-apps の応答に閉じられたウィンドウが混入しないようにするため。
    /// </summary>
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

    /// <summary>
    /// 引退済みエントリは TryResolve で false を返すことを確認する。
    /// 閉じられたウィンドウの windowRef を使った操作が失敗するべき仕様の回帰防止。
    /// </summary>
    [Fact]
    public void TryResolve_RetiredEntry_ReturnsFalse()
    {
        var store = new WindowRefStore();
        var k = Key(100, 0x1000);
        var entry = store.SyncOrAssign(k, Info(100, 0x1000));

        store.RetireMissing(Array.Empty<WindowKey>());

        Assert.False(store.TryResolve(entry.WindowRef, out _));
    }

    /// <summary>
    /// 一度引退したエントリと同じ key で再度 SyncOrAssign すると、同じ windowRef で復活することを確認する。
    /// ウィンドウが一時的に列挙から外れた後復帰した際に ref が変わらない仕様 (ref-ids §復活) の回帰防止。
    /// </summary>
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

    /// <summary>
    /// 未登録の windowRef を TryResolve すると false が返ることを確認する。
    /// 不正な windowRef 入力時に NRE などにならない安全側の挙動を担保するため。
    /// </summary>
    [Fact]
    public void TryResolve_UnknownWindowRef_ReturnsFalse()
    {
        var store = new WindowRefStore();
        Assert.False(store.TryResolve("w42", out _));
    }

    /// <summary>
    /// 既存 key を TryFindByKey で検索すると、その key のエントリが返ることを確認する。
    /// attach の idempotent 判定 (M-1) で同じ key の既存 ref を再利用するロジックの回帰防止。
    /// </summary>
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

    /// <summary>
    /// 未登録の key を TryFindByKey すると false が返ることを確認する。
    /// 新規ウィンドウの初回 attach パスを誤検出しないようにするため。
    /// </summary>
    [Fact]
    public void TryFindByKey_UnknownKey_ReturnsFalse()
    {
        var store = new WindowRefStore();
        Assert.False(store.TryFindByKey(Key(999, 0x9999), out _));
    }

    /// <summary>
    /// TryFindByKey は引退済みエントリも検索結果に含めることを確認する。
    /// 引退→復活の idempotent 判定で過去の windowRef を再利用するために必須の挙動。
    /// </summary>
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

    /// <summary>
    /// SessionId で検索したとき、AssociateSession で紐付いた生存エントリが返ることを確認する。
    /// MCP ツール側で sessionId からの逆引きが必要なケース (detach など) の回帰防止。
    /// </summary>
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

    /// <summary>
    /// ClearSession 後は TryFindBySessionId が false を返すことを確認する。
    /// detach 後にゴーストの sessionId が残らないようにするため。
    /// </summary>
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

    /// <summary>
    /// 引退済みエントリは sessionId 紐付けが残っていても TryFindBySessionId に出ないことを確認する。
    /// 閉じられたウィンドウのセッションが操作対象として誤って解決されないようにするため。
    /// </summary>
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

    /// <summary>
    /// 未登録の sessionId を渡しても false を返すことを確認する。
    /// 不正な sessionId に対する境界挙動の保証。
    /// </summary>
    [Fact]
    public void TryFindBySessionId_UnknownSession_ReturnsFalse()
    {
        var store = new WindowRefStore();
        Assert.False(store.TryFindBySessionId("s99", out _));
    }

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
}
