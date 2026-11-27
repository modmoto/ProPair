using System.Globalization;

namespace ProPair.Web.Services;

public static class Format
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>Signed score, e.g. "+2", "−1,5", "0".</summary>
    public static string Score(double value) =>
        Math.Round(value, 1).ToString("+0.#;−0.#;0", De);

    /// <summary>Plain factor in German notation, e.g. "1,5".</summary>
    public static string Factor(double value) => value.ToString("0.##", De);

    /// <summary>Probability as whole percent, e.g. "42 %".</summary>
    public static string Percent(double p) => $"{Math.Round(p * 100):0} %";

    /// <summary>Short local timestamp, e.g. "27.11. 14:32".</summary>
    public static string When(DateTimeOffset at) => at.ToLocalTime().ToString("dd.MM. HH:mm", De);
}
