using Adact.Engine.Exceptions;
using Adact.Engine.Snapshot;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>
/// <see cref="RefRegistry"/> の ref 採番・安定キー・解決ロジックを検証する Unit テスト。
/// MCP で公開される ref セマンティクス (ref-ids.md) の回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
public class RefRegistryTests
{
    /// <summary>
    /// 新規 Registry に最初に登録した要素は s1e1 が割り当てられることを確認する。
    /// </summary>
    [Fact]
    public void Register_FirstElement_GetsId1()
    {
        var r = new RefRegistry(1);
        r.BeginSnapshot();
        var refId = r.Register(new FakeElement(), positionalIndex: 0);
        Assert.Equal("s1e1", refId);
    }

    /// <summary>
    /// 同じ RuntimeId を持つ要素を別 snapshot で登録したとき、eid が再利用されることを確認する。
    /// snapshot 間で ref が安定している仕様の回帰防止。
    /// </summary>
    [Fact]
    public void StableKey_SameRuntimeId_ReusesEid()
    {
        var r = new RefRegistry(1);
        var rid = new[] { 42, 7 };

        r.BeginSnapshot();
        var first = r.Register(new FakeElement { RuntimeId = rid }, positionalIndex: 0);

        r.BeginSnapshot();
        var second = r.Register(new FakeElement { RuntimeId = rid }, positionalIndex: 0);

        Assert.Equal(first, second);
    }

    /// <summary>
    /// 異なる RuntimeId を持つ要素には別 eid が振られることを確認する。
    /// </summary>
    [Fact]
    public void StableKey_DifferentRuntimeId_AssignsNewEid()
    {
        var r = new RefRegistry(1);

        r.BeginSnapshot();
        var a = r.Register(new FakeElement { RuntimeId = new[] { 1 } }, positionalIndex: 0);
        var b = r.Register(new FakeElement { RuntimeId = new[] { 2 } }, positionalIndex: 1);

        Assert.NotEqual(a, b);
    }

    /// <summary>
    /// RuntimeId 未設定の要素は positionalIndex をフォールバックキーとして ref を安定化することを確認する。
    /// RuntimeId を提供しない element ソース (一部の UIA プロバイダ) でも ref がブレない仕様の回帰防止。
    /// 同一要素は再 attach 後も ref が振り直されない契約を保証する。
    /// </summary>
    [Fact]
    public void RuntimeIdMissing_FallsBackToPositionalIndex()
    {
        var r = new RefRegistry(1);

        r.BeginSnapshot();
        var first = r.Register(new FakeElement(), positionalIndex: 0);

        r.BeginSnapshot();
        // 同じ positional index なら同じ stableKey ("unstable:0") → eid 再利用
        var second = r.Register(new FakeElement(), positionalIndex: 0);

        Assert.Equal(first, second);

        // 別の positional index なら別 eid
        var third = r.Register(new FakeElement(), positionalIndex: 1);
        Assert.NotEqual(first, third);
    }

    /// <summary>
    /// 現在 snapshot に含まれる ref を Resolve したとき、同じ要素インスタンスが返されることを確認する。
    /// </summary>
    [Fact]
    public void Resolve_CurrentSnapshotRef_ReturnsElement()
    {
        var r = new RefRegistry(2);
        r.BeginSnapshot();
        var el = new FakeElement();
        var refId = r.Register(el, positionalIndex: 0);
        Assert.Same(el, r.Resolve(refId));
    }

    /// <summary>
    /// 現在 snapshot に含まれない ref を Resolve すると RefNotFoundException となることを確認する。
    /// snapshot を越えて古い ref を使う誤りを検出する仕様の回帰防止。
    /// </summary>
    [Fact]
    public void Resolve_ElementNotInCurrentSnapshot_Throws()
    {
        var r = new RefRegistry(1);
        r.BeginSnapshot();
        // RuntimeId 無し要素を index 0 で登録
        var refId = r.Register(new FakeElement(), positionalIndex: 0);

        // 新 snapshot では index 0 を登録しない (登録なし)
        r.BeginSnapshot();

        var ex = Assert.Throws<RefNotFoundException>(() => r.Resolve(refId));
        Assert.Contains("not found in current snapshot", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 別 sessionId の Registry で ref を Resolve しようとすると、session mismatch メッセージで throw されることを確認する。
    /// </summary>
    [Fact]
    public void Resolve_DifferentSessionRef_Throws()
    {
        var r1 = new RefRegistry(1);
        var r2 = new RefRegistry(2);
        r1.BeginSnapshot();
        r2.BeginSnapshot();
        var s1Ref = r1.Register(new FakeElement(), positionalIndex: 0);
        var ex = Assert.Throws<RefNotFoundException>(() => r2.Resolve(s1Ref));
        Assert.Contains("session mismatch", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 不正な ref 文字列 (全くフォーマット違い/不完全/空) は RefNotFoundException となることを確認する。
    /// </summary>
    [Fact]
    public void Resolve_MalformedRef_Throws()
    {
        var r = new RefRegistry(1);
        r.BeginSnapshot();
        Assert.Throws<RefNotFoundException>(() => r.Resolve("garbage"));
        Assert.Throws<RefNotFoundException>(() => r.Resolve("s1"));
        Assert.Throws<RefNotFoundException>(() => r.Resolve(""));
    }

    /// <summary>
    /// RefId.Format で生成した文字列が TryParse で丸ごと復元されることを確認する。
    /// </summary>
    [Fact]
    public void Format_GivenValidComponents_RoundTripsViaParse()
    {
        var s = RefId.Format(3, 42);
        Assert.Equal("s3e42", s);
        Assert.True(RefId.TryParse(s, out var sid, out var eid));
        Assert.Equal((3, 42), (sid, eid));
    }

    /// <summary>
    /// TryParse が不正・境界入力に対して正しく false を返すか、パース結果が仕様通りかを確認する。
    /// </summary>
    [Theory]
    [InlineData("s-1e2", false, 0, 0)]
    [InlineData("s1 e2", false, 0, 0)]
    [InlineData("s1e2x", false, 0, 0)]
    [InlineData("se1", false, 0, 0)]
    [InlineData("s1e", false, 0, 0)]
    [InlineData(null, false, 0, 0)]
    [InlineData("", false, 0, 0)]
    [InlineData("S1E2", false, 0, 0)]
    public void TryParse_InvalidInputs_ReturnsFalse(string? value, bool expectedResult, int expectedSid, int expectedEid)
    {
        var result = RefId.TryParse(value!, out var sid, out var eid);
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedSid, sid);
        Assert.Equal(expectedEid, eid);
    }

    /// <summary>
    /// uint 範囲内だが int.MaxValue を超える値は TryParse で拒否されることを確認する。
    /// </summary>
    [Theory]
    [InlineData("s2147483648e1")]  // int.MaxValue + 1 for sessionId
    [InlineData("s1e2147483648")]  // int.MaxValue + 1 for elementId
    [InlineData("s4294967295e1")]  // uint.MaxValue for sessionId
    public void TryParse_OverflowBoundary_ReturnsFalse(string value)
    {
        var result = RefId.TryParse(value, out var sid, out var eid);
        Assert.False(result);
        Assert.Equal(0, sid);
        Assert.Equal(0, eid);
    }

    /// <summary>
    /// 先頭ゼロを含む数値は uint.TryParse が許容するため TryParse 成功する。
    /// </summary>
    [Fact]
    public void TryParse_LeadingZeros_Succeeds()
    {
        var result = RefId.TryParse("s01e02", out var sid, out var eid);
        Assert.True(result);
        Assert.Equal(1, sid);
        Assert.Equal(2, eid);
    }
}
