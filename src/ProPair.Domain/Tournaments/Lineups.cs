namespace ProPair.Domain.Tournaments;

/// <summary>
/// A team as registered at the source. Players are the members with an army list; all others
/// (coaches, judges, NPCs) only count in <paramref name="Members"/>.
/// </summary>
public sealed record RegisteredTeam(string Name, int Members, IReadOnlyList<Player> Players);

/// <summary>The roster a team plays with.</summary>
/// <param name="LeftOut">Names of players with a list that exceed the team size.</param>
public sealed record Lineup(string Name, IReadOnlyList<Player> Roster, IReadOnlyList<string>? LeftOut = null);

public static class Lineups
{
    /// <summary>
    /// Builds rosters of equal size – pairing needs that.
    /// <list type="bullet">
    /// <item>Team size: the most common number of players among teams with at least 3 (ties: the larger), at most 8.</item>
    /// <item>Missing players or lists are filled with dummies, surplus players are left out.</item>
    /// <item>A team without any player and with fewer members than the team size (judges, stand-ins) is not a team.</item>
    /// <item>No list at all yet: all rosters stay empty.</item>
    /// </list>
    /// Returns null for formats we do not support (2v2 and smaller).
    /// </summary>
    public static IReadOnlyList<Lineup>? Build(IReadOnlyList<RegisteredTeam> teams)
    {
        var counts = teams.Select(t => t.Players.Count).Where(n => n > 0).ToList();
        if (counts.Count == 0) return teams.Select(t => new Lineup(t.Name, [])).ToList();

        var full = counts.Where(n => n >= TeamSize.Min).ToList();
        if (full.Count == 0 || counts.Count(n => n == 2) > full.Count) return null;

        var size = full.Select(n => Math.Min(n, TeamSize.Max)).GroupBy(n => n)
            .OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).First().Key;

        return teams.Where(t => t.Players.Count > 0 || t.Members >= size).Select(t => Fill(t, size)).ToList();
    }

    private static Lineup Fill(RegisteredTeam team, int size)
    {
        var roster = team.Players.Take(size).ToList();
        for (var number = 1; roster.Count < size; number++) roster.Add(Player.Dummy(number));
        return new Lineup(team.Name, roster, team.Players.Skip(size).Select(p => p.Name).ToList());
    }
}
