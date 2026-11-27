using System.Globalization;

namespace ProPair.Web.Services;

public static class Format
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>Signed score, e.g. "+2", "−1,5", "0".</summary>
    public static string Score(double value) =>
        Math.Round(value, 1).ToString("+0.#;−0.#;0", De);
}
