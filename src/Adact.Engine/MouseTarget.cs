using System.Globalization;
using System.Text.RegularExpressions;

namespace Adact.Engine;

/// <summary>
/// マウス系操作 (mousemove, mousedown, mouseup, mousewheel 等) の対象を表す共通型。
/// 要素 ref (<c>s&lt;sid&gt;e&lt;eid&gt;</c>) または座標 (<c>x,y</c>) のいずれかで指定する。
/// </summary>
public abstract record MouseTarget
{
    /// <summary>要素 ref ("^s\d+e\d+$") を表すパターン。</summary>
    private static readonly Regex RefPattern = new(@"^s\d+e\d+$", RegexOptions.Compiled);

    /// <summary>座標 ("^-?\d+,-?\d+$") を表すパターン。マルチモニタ対応のため負値も許可する。</summary>
    private static readonly Regex PointPattern = new(@"^-?\d+,-?\d+$", RegexOptions.Compiled);

    /// <summary>外部からの継承を抑止するための internal コンストラクタ。</summary>
    private protected MouseTarget()
    {
    }

    /// <summary>
    /// 要素 ref で指定された対象。
    /// </summary>
    /// <param name="Ref">要素 ref (例: <c>s1e2</c>)。</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "Phase 8 設計書 §4 で名前を ByRef に固定している。VB から利用する想定はない。")]
    public sealed record ByRef(string Ref) : MouseTarget;

    /// <summary>
    /// 画面座標で指定された対象。
    /// </summary>
    /// <param name="X">スクリーン X 座標 (px)。負値はマルチモニタ環境を考慮して許可される。</param>
    /// <param name="Y">スクリーン Y 座標 (px)。負値はマルチモニタ環境を考慮して許可される。</param>
    public sealed record ByPoint(int X, int Y) : MouseTarget;

    /// <summary>
    /// 入力文字列を <see cref="MouseTarget"/> に解析する。
    /// </summary>
    /// <param name="input">
    /// <c>s&lt;sid&gt;e&lt;eid&gt;</c> 形式 (例: <c>s1e2</c>) または
    /// <c>x,y</c> 形式 (例: <c>20,30</c>, <c>-100,50</c>) のいずれか。
    /// </param>
    /// <returns>解析結果の <see cref="MouseTarget"/>。</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="input"/> が null / 空文字、またはどちらの形式にも一致しない場合。
    /// </exception>
    public static MouseTarget Parse(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentException("target must be non-empty.", nameof(input));
        }

        if (RefPattern.IsMatch(input))
        {
            return new ByRef(input);
        }

        if (PointPattern.IsMatch(input))
        {
            var parts = input.Split(',');
            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y))
            {
                throw new ArgumentException(
                    $"target '{input}' has out-of-range integer components.",
                    nameof(input));
            }

            return new ByPoint(x, y);
        }

        throw new ArgumentException(
            $"target '{input}' is not a valid ref ('s<sid>e<eid>') or point ('x,y').",
            nameof(input));
    }
}
