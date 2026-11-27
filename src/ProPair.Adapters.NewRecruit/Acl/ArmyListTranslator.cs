using System.Text.RegularExpressions;
using ProPair.Application.Ports;
using ProPair.Domain.GameSystems;

namespace ProPair.Adapters.NewRecruit.Acl;

/// <summary>
/// Anti-corruption layer for army lists: New Recruit delivers free text, we hand on clean lists.
/// <list type="bullet">
/// <item>Removed: header lines (player, team, army, export or points summaries), total lines, the New Recruit
///   footer, and lists handed in twice.</item>
/// <item>Kept as written: everything between and after the units (categories, event extras).</item>
/// <item>Placeholders such as "kadrowicz / 0" are no list.</item>
/// <item>The army is read from the raw text first, it usually only stands in the header.</item>
/// </list>
/// Unknown formats are not rewritten – they only lose totals and footer.
/// </summary>
public static partial class ArmyListTranslator
{
    /// <summary>Fewer lines with a name and points than this: not an army list.</summary>
    private const int MinPointLines = 3;

    public static SourcePlayer Translate(string name, IEnumerable<string?> rawLists)
    {
        var raw = rawLists.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l!).ToList();
        var lists = raw.Where(IsArmyList).SelectMany(Clean).Distinct().ToList();
        return lists.Count == 0
            ? new SourcePlayer(name, ArmyDetector.Unknown, "")
            : new SourcePlayer(name, ArmyDetector.Detect(string.Join("\n\n", raw)), string.Join("\n\n", lists));
    }

    private static bool IsArmyList(string raw) =>
        Lines(raw).Count(l => Letter().IsMatch(l) && Points().IsMatch(l)) >= MinPointLines;

    /// <summary>One text can hold the same list twice, separated by the total – hence a sequence of parts.</summary>
    private static IEnumerable<string> Clean(string raw)
    {
        var lines = Lines(raw).Where(l => !l.TrimStart().StartsWith("Created with", StringComparison.OrdinalIgnoreCase)).ToList();
        // only the usual "480 - Unit, options" format is known well enough to cut the header
        var usual = lines.Count(l => Entry().IsMatch(l)) >= MinPointLines;
        if (usual) lines = lines.SkipWhile(l => !UnitStart().IsMatch(l)).ToList();

        var part = new List<string>();
        foreach (var line in lines.Append(null))
        {
            if (line is not null && !Total().IsMatch(line))
            {
                part.Add(line);
                continue;
            }

            if (usual && part.Any(l => Entry().IsMatch(l))) part = part.SkipWhile(l => !UnitStart().IsMatch(l)).ToList();
            var text = Join(part);
            if (text.Length > 0) yield return text;
            part = [];
        }
    }

    private static IEnumerable<string> Lines(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Select(l => l.TrimEnd());

    /// <summary>No empty lines at the edges, never two in a row.</summary>
    private static string Join(List<string> lines)
    {
        var result = new List<string>();
        foreach (var line in lines)
            if (line.Length > 0 || result is [.., { Length: > 0 }]) result.Add(line);
        return string.Join('\n', result).TrimEnd('\n');
    }

    /// <summary>"480 - Unit" – the line New Recruit writes per unit.</summary>
    [GeneratedRegex(@"^\s*\d{2,4}\s*[-–—]\s*\S")]
    private static partial Regex Entry();

    /// <summary>First line of the units: an entry, or points without a dash ("575 Beastlord").</summary>
    [GeneratedRegex(@"^\s*(\d{2,4}\s*[-–—]\s*\S|\d{3,4}\s+\p{L})")]
    private static partial Regex UnitStart();

    /// <summary>The army total on its own line: "4000", "4 000 pts", "Total: 3995".</summary>
    [GeneratedRegex(@"^\s*(total\s*:?\s*)?\d{1,2}[ .,]?\d{3}\s*(pts?|points|p)?\.?\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex Total();

    [GeneratedRegex(@"\p{L}")]
    private static partial Regex Letter();

    [GeneratedRegex(@"\d{2,}")]
    private static partial Regex Points();
}
