using System.Globalization;

namespace ProPair.Web.Services;

public static class Format
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>Signed score, e.g. "+2", "−1,5", "0".</summary>
    public static string Score(double value) =>
        Math.Round(value, 1).ToString("+0.#;−0.#;0", De);

    /// <summary>Short local timestamp, e.g. "27.11. 14:32".</summary>
    public static string When(DateTimeOffset at) => at.ToLocalTime().ToString("dd.MM. HH:mm", De);
}
