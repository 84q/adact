using System.Globalization;

using Adact.Engine;

namespace Adact.Mcp.Common;

/// <summary>
/// daemon 側 in-memory singleton。top-level window に対し短い Window Ref ID
/// (<c>w1</c>, <c>w2</c>, ...) を払い出し、列挙との同期および引退管理を行う。
/// 詳細は discussion/009_Phase5設計.md §7 参照。
/// </summary>
public sealed class WindowRefStore
{
    private static readonly TimeSpan DefaultRetiredEntryTtl = TimeSpan.FromMinutes(5);

    /// <summary>すべての読み書きを覆うロック。<c>_entries</c> と <c>_nextRef</c> はこのロック下でのみ触る。</summary>
    private readonly object _lock = new();
    /// <summary>WindowKey → エントリ。引退済みエントリも保持し、同一 HWND の復活時に番号を保つ。</summary>
    private readonly Dictionary<WindowKey, WindowRefEntry> _entries = new();
    /// <summary>最も最近採番した <c>w&lt;n&gt;</c> の <c>n</c>。<see cref="Interlocked.Increment(ref int)"/> で採番される。</summary>
    private int _nextRef;
    private readonly TimeSpan _retiredEntryTtl;
    private readonly Func<DateTimeOffset> _utcNow;

    /// <summary>
    /// 新しい <see cref="WindowRefStore"/> を構築する。
    /// </summary>
    /// <param name="retiredEntryTtl">retired entry を保持する最大期間。null の場合は既定値。</param>
    /// <param name="utcNow">現在 UTC 時刻の取得関数。テスト用。null の場合は <see cref="DateTimeOffset.UtcNow"/>。</param>
    public WindowRefStore(TimeSpan? retiredEntryTtl = null, Func<DateTimeOffset>? utcNow = null)
    {
        _retiredEntryTtl = retiredEntryTtl ?? DefaultRetiredEntryTtl;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 同じ <see cref="WindowKey"/> に対しては既存 windowRef を返し、
    /// 未知の WindowKey には新たな番号を採番してエントリを登録する。
    /// 既存エントリが引退済みの場合も同じ key とみなして再採番せず、
    /// 同じ番号で復活させる (HWND 一致なら同一 window と扱う)。
    /// </summary>
    /// <param name="key">window 同一性キー。</param>
    /// <param name="info">最新の <see cref="WindowInfo"/> (title 等の更新に使用)。</param>
    /// <returns>該当 window に対応する <see cref="WindowRefEntry"/>。</returns>
    /// <remarks>スレッドセーフ。内部で <see cref="_lock"/> を取得している。</remarks>
    public WindowRefEntry SyncOrAssign(WindowKey key, WindowInfo info)
    {
        lock (_lock)
        {
            PurgeExpiredRetiredEntriesCore(_utcNow());

            if (_entries.TryGetValue(key, out var existing))
            {
                // 引退済から復活する場合は最新の WindowInfo に更新し Retired を解除。
                if (existing.Retired)
                {
                    var revived = existing with { Info = info, SessionId = null, Retired = false, RetiredAtUtc = null };
                    _entries[key] = revived;
                    return revived;
                }

                // タイトル等が変わっている可能性があるので常に Info を最新化。
                var updated = existing with { Info = info };
                _entries[key] = updated;
                return updated;
            }

            var n = Interlocked.Increment(ref _nextRef);
            var windowRef = "w" + n.ToString(CultureInfo.InvariantCulture);
            var entry = new WindowRefEntry(windowRef, key, info, SessionId: null, Retired: false);
            _entries[key] = entry;
            return entry;
        }
    }

    /// <summary>
    /// ストアに保持している非引退エントリのうち、<paramref name="presentKeys"/> に含まれないものを
    /// 引退マークし、<see cref="WindowRefEntry.SessionId"/> をクリアする。
    /// </summary>
    /// <param name="presentKeys">今回の list で見えている WindowKey 集合。</param>
    /// <exception cref="ArgumentNullException"><paramref name="presentKeys"/> が <c>null</c> のとき。</exception>
    public void RetireMissing(IEnumerable<WindowKey> presentKeys)
    {
        ArgumentNullException.ThrowIfNull(presentKeys);

        var present = new HashSet<WindowKey>(presentKeys);
        lock (_lock)
        {
            var now = _utcNow();
            PurgeExpiredRetiredEntriesCore(now);

            // Dictionary の列挙中に書き換えると例外になるため、対象キーを集めてから書き戻す。
            List<WindowKey>? toRetire = null;
            foreach (var kv in _entries)
            {
                if (kv.Value.Retired) continue;
                if (present.Contains(kv.Key)) continue;
                (toRetire ??= new List<WindowKey>()).Add(kv.Key);
            }
            if (toRetire is not null)
            {
                foreach (var key in toRetire)
                {
                    _entries[key] = _entries[key] with { SessionId = null, Retired = true, RetiredAtUtc = now };
                }
            }
        }
    }

    /// <summary>指定 <c>w&lt;n&gt;</c> に対応する生存エントリを探す。引退済みエントリは false を返す。</summary>
    /// <param name="windowRef">探す windowRef。</param>
    /// <param name="entry">見つかったエントリ。見つからない場合は <c>default!</c>。</param>
    /// <returns>生存エントリが見つかったかどうか。</returns>
    public bool TryResolve(string windowRef, out WindowRefEntry entry)
    {
        lock (_lock)
        {
            PurgeExpiredRetiredEntriesCore(_utcNow());
            foreach (var e in _entries.Values)
            {
                if (e.Retired) continue;
                if (string.Equals(e.WindowRef, windowRef, StringComparison.Ordinal))
                {
                    entry = e;
                    return true;
                }
            }
        }
        entry = default!;
        return false;
    }

    /// <summary>attach 成功時に sessionId を関連付ける。</summary>
    /// <param name="windowRef">関連付け対象の <c>w&lt;n&gt;</c>。</param>
    /// <param name="sessionId"><see cref="SessionStore"/> から見た sessionId (例: <c>s1</c>)。</param>
    public void AssociateSession(string windowRef, string sessionId)
    {
        lock (_lock)
        {
            PurgeExpiredRetiredEntriesCore(_utcNow());
            foreach (var kv in _entries)
            {
                if (kv.Value.Retired) continue;
                if (!string.Equals(kv.Value.WindowRef, windowRef, StringComparison.Ordinal)) continue;
                _entries[kv.Key] = kv.Value with { SessionId = sessionId };
                return;
            }
        }
    }

    /// <summary>detach 時に sessionId をクリアする (Phase 5 #7 で利用)。</summary>
    /// <param name="windowRef">関連 sessionId を取り外す <c>w&lt;n&gt;</c>。</param>
    public void ClearSession(string windowRef)
    {
        lock (_lock)
        {
            PurgeExpiredRetiredEntriesCore(_utcNow());
            foreach (var kv in _entries)
            {
                if (!string.Equals(kv.Value.WindowRef, windowRef, StringComparison.Ordinal)) continue;
                _entries[kv.Key] = kv.Value with { SessionId = null };
                return;
            }
        }
    }

    /// <summary>
    /// 指定 <see cref="WindowKey"/> に一致するエントリを返す。引退済みも含めて検索する
    /// (呼び出し側が <see cref="WindowRefEntry.Retired"/> を見て判断する)。
    /// </summary>
    /// <param name="key">探すキー。</param>
    /// <param name="entry">見つかったエントリ。存在しなければ <c>default!</c>。</param>
    /// <returns>エントリが見つかったかどうか。</returns>
    public bool TryFindByKey(WindowKey key, out WindowRefEntry entry)
    {
        lock (_lock)
        {
            PurgeExpiredRetiredEntriesCore(_utcNow());
            if (_entries.TryGetValue(key, out var found))
            {
                entry = found;
                return true;
            }
        }
        entry = default!;
        return false;
    }

    /// <summary>引退済を除く全エントリのスナップショット。</summary>
    /// <returns>生存エントリのリスト (コピー)。</returns>
    public IReadOnlyList<WindowRefEntry> ListActive()
    {
        lock (_lock)
        {
            PurgeExpiredRetiredEntriesCore(_utcNow());
            return _entries.Values.Where(e => !e.Retired).ToArray();
        }
    }

    /// <summary>
    /// 指定 sessionId に紐付く生存エントリを返す。引退済みは対象外。線形スキャン (エントリ数は通常少数)。
    /// </summary>
    /// <param name="sessionId">探す sessionId。</param>
    /// <param name="entry">見つかったエントリ。見つからなければ <c>default!</c>。</param>
    /// <returns>エントリが見つかったかどうか。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sessionId"/> が <c>null</c> のとき。</exception>
    public bool TryFindBySessionId(string sessionId, out WindowRefEntry entry)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        lock (_lock)
        {
            PurgeExpiredRetiredEntriesCore(_utcNow());
            foreach (var e in _entries.Values)
            {
                if (e.Retired) continue;
                if (string.Equals(e.SessionId, sessionId, StringComparison.Ordinal))
                {
                    entry = e;
                    return true;
                }
            }
        }
        entry = default!;
        return false;
    }

    /// <summary>
    /// 指定 sessionId に紐付いている entry をストアから完全に削除する。
    /// detach / close / kill 後の不要な関連付け掃除に使う。
    /// </summary>
    /// <param name="sessionId">削除対象の sessionId。</param>
    public void RemoveBySessionId(string sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        lock (_lock)
        {
            PurgeExpiredRetiredEntriesCore(_utcNow());

            List<WindowKey>? toRemove = null;
            foreach (var kv in _entries)
            {
                if (!string.Equals(kv.Value.SessionId, sessionId, StringComparison.Ordinal)) continue;
                (toRemove ??= []).Add(kv.Key);
            }

            if (toRemove is null) return;
            foreach (var key in toRemove)
            {
                _entries.Remove(key);
            }
        }
    }

    internal int PurgeExpiredRetiredEntries()
    {
        lock (_lock)
        {
            return PurgeExpiredRetiredEntriesCore(_utcNow());
        }
    }

    private int PurgeExpiredRetiredEntriesCore(DateTimeOffset now)
    {
        List<WindowKey>? expired = null;
        foreach (var kv in _entries)
        {
            if (!kv.Value.Retired || kv.Value.RetiredAtUtc is null) continue;
            if (now - kv.Value.RetiredAtUtc.Value < _retiredEntryTtl) continue;
            (expired ??= []).Add(kv.Key);
        }

        if (expired is null) return 0;
        foreach (var key in expired)
        {
            _entries.Remove(key);
        }

        return expired.Count;
    }
}

/// <summary>
/// WindowRefStore に保持される単一エントリ。
/// </summary>
/// <param name="WindowRef">この window に割り当てられた安定 ref (例: <c>w1</c>)。</param>
/// <param name="Key">同一性判定に使用する <see cref="WindowKey"/>。</param>
/// <param name="Info">最新の <see cref="WindowInfo"/> (title 等は list のたびに更新される)。</param>
/// <param name="SessionId">attach 済みの sessionId。未 attach なら <c>null</c>。</param>
/// <param name="Retired">このエントリが list から消えたため引退済みかどうか。</param>
/// <param name="RetiredAtUtc">引退時刻。未引退または旧形式 entry では <c>null</c>。</param>
public sealed record WindowRefEntry(
    string WindowRef,
    WindowKey Key,
    WindowInfo Info,
    string? SessionId,
    bool Retired,
    DateTimeOffset? RetiredAtUtc = null);
