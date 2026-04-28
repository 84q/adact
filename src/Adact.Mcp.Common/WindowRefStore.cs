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
  private readonly object _lock = new();
  private readonly Dictionary<WindowKey, WindowRefEntry> _entries = new();
  private int _nextRef;

  /// <summary>
  /// 同じ <see cref="WindowKey"/> に対しては既存 windowRef を返し、
  /// 未知の WindowKey には新たな番号を採番してエントリを登録する。
  /// 既存エントリが引退済みの場合も「同じ key」とみなさず再採番せず、
  /// 同じ番号で復活させる (HWND 一致なら同一 window と扱う)。
  /// </summary>
  public WindowRefEntry SyncOrAssign(WindowKey key, WindowInfo info)
  {
    lock (_lock)
    {
      if (_entries.TryGetValue(key, out var existing))
      {
        // 引退済から復活する場合は最新の WindowInfo に更新し Retired を解除。
        if (existing.Retired)
        {
          var revived = existing with { Info = info, Retired = false };
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
  /// 列挙された <see cref="WindowKey"/> のうち、ストアに存在するエントリのみ生存とみなし、
  /// それ以外を引退マークする。引退時 <see cref="WindowRefEntry.SessionId"/> はクリアする。
  /// </summary>
  public void RetireMissing(IEnumerable<WindowKey> presentKeys)
  {
    ArgumentNullException.ThrowIfNull(presentKeys);

    var present = new HashSet<WindowKey>(presentKeys);
    lock (_lock)
    {
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
          _entries[key] = _entries[key] with { SessionId = null, Retired = true };
        }
      }
    }
  }

  /// <summary>引退済みエントリは false を返す。</summary>
  public bool TryResolve(string windowRef, out WindowRefEntry entry)
  {
    lock (_lock)
    {
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
  public void AssociateSession(string windowRef, string sessionId)
  {
    lock (_lock)
    {
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
  public void ClearSession(string windowRef)
  {
    lock (_lock)
    {
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
  public bool TryFindByKey(WindowKey key, out WindowRefEntry entry)
  {
    lock (_lock)
    {
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
  public IReadOnlyList<WindowRefEntry> ListActive()
  {
    lock (_lock)
    {
      return _entries.Values.Where(e => !e.Retired).ToArray();
    }
  }

  /// <summary>
  /// 指定 sessionId に紐付く生存エントリを返す。引退済みは対象外。線形スキャン (エントリ数は通常少数)。
  /// </summary>
  public bool TryFindBySessionId(string sessionId, out WindowRefEntry entry)
  {
    ArgumentNullException.ThrowIfNull(sessionId);
    lock (_lock)
    {
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
}

/// <summary>
/// WindowRefStore に保持される単一エントリ。
/// </summary>
public sealed record WindowRefEntry(
    string WindowRef,
    WindowKey Key,
    WindowInfo Info,
    string? SessionId,
    bool Retired);
