using System.Globalization;
using System.Text;

namespace SEE.Tools.BalanceSim.Report;

/// <summary>Small formatting helpers so every printed table is fixed-width and invariant.</summary>
internal static class Fmt
{
    /// <summary>Fixed-point number with an invariant decimal point (half away from zero, like §4.3).</summary>
    public static string N(double value, int decimals = 2) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero)
            .ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    /// <summary>Signed percent value, for example <c>+2.93%</c>.</summary>
    public static string Pct(double value, int decimals = 2) =>
        (value >= 0.0 ? "+" : string.Empty) + N(value, decimals) + "%";

    /// <summary>PASS / FAIL verdict text.</summary>
    public static string Verdict(bool pass) => pass ? "PASS" : "FAIL";

    /// <summary>Left-aligned cell.</summary>
    public static string L(string text, int width) => text.PadRight(width);

    /// <summary>Right-aligned cell.</summary>
    public static string R(string text, int width) => text.PadLeft(width);

    /// <summary>Right-aligned integer cell.</summary>
    public static string R(int value, int width) => R(value.ToString(CultureInfo.InvariantCulture), width);

    /// <summary>Right-aligned fixed-point cell.</summary>
    public static string R(double value, int width, int decimals = 2) => R(N(value, decimals), width);

    /// <summary>Builds one fixed-width line; cells are (text, width) pairs, right aligned after the first.</summary>
    public static string Line(params (string Text, int Width)[] cells)
    {
        var builder = new StringBuilder();
        for (int index = 0; index < cells.Length; index++)
        {
            if (index == 0)
            {
                builder.Append(L(cells[index].Text, cells[index].Width));
            }
            else
            {
                builder.Append(R(cells[index].Text, cells[index].Width));
            }

            builder.Append(' ');
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>Prints a section heading with a full-width rule.</summary>
    public static void Section(string title)
    {
        Console.WriteLine();
        Console.WriteLine("== " + title + " " + new string('=', Math.Max(4, 76 - title.Length)));
    }

    /// <summary>Prints an informational line.</summary>
    public static void Info(string label, string value) => Console.WriteLine("  " + L(label, 38) + value);
}
