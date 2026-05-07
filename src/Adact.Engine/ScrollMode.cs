namespace Adact.Engine;

/// <summary>ScrollPattern 操作のモードを表す discriminated union 風レコード。</summary>
public abstract record ScrollMode;

/// <summary>SetScrollPercent でスクロール位置を指定する。null は NoScroll (-1)。</summary>
/// <param name="PercentH">水平スクロール位置 (0〜100)。null は NoScroll。</param>
/// <param name="PercentV">垂直スクロール位置 (0〜100)。null は NoScroll。</param>
public sealed record PercentMode(int? PercentH, int? PercentV) : ScrollMode;

/// <summary>SmallIncrement/Decrement を |delta| 回繰り返す。正=Increment、負=Decrement。null は NoAmount。</summary>
/// <param name="DeltaH">水平方向の small scroll 回数。正=右、負=左。null はスクロールしない。</param>
/// <param name="DeltaV">垂直方向の small scroll 回数。正=下、負=上。null はスクロールしない。</param>
public sealed record SmallMode(int? DeltaH, int? DeltaV) : ScrollMode;

/// <summary>LargeIncrement/Decrement を |delta| 回繰り返す。正=Increment、負=Decrement。null は NoAmount。</summary>
/// <param name="DeltaH">水平方向の large scroll 回数。正=右、負=左。null はスクロールしない。</param>
/// <param name="DeltaV">垂直方向の large scroll 回数。正=下、負=上。null はスクロールしない。</param>
public sealed record LargeMode(int? DeltaH, int? DeltaV) : ScrollMode;
