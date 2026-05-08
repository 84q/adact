using System.Globalization;
using System.Text.RegularExpressions;

namespace Adact.Engine;

/// <summary>
/// Represents a mouse target resolved from a ref or point.
/// </summary>
public abstract record MouseTarget
{
    private static readonly Regex RefPattern = new(@"^s\d+e\d+$", RegexOptions.Compiled);

    private static readonly Regex PointPattern = new(@"^-?\d+,-?\d+$", RegexOptions.Compiled);

    private protected MouseTarget()
    {
    }

    /// <summary>
    /// A mouse target identified by element ref.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "The name is fixed to ByRef in the Phase 8 design doc §4. VB usage is not expected.")]
    public sealed record ByRef(string Ref) : MouseTarget;

    /// <summary>
    /// A mouse target identified by screen coordinates.
    /// </summary>
    public sealed record ByPoint(int X, int Y) : MouseTarget;

    /// <summary>
    /// Parses a mouse target from a ref ID or an <c>x,y</c> point.
    /// </summary>
    /// <param name="input">The input to parse.</param>
    /// <exception cref="ArgumentException">Thrown when the input is invalid.</exception>
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
