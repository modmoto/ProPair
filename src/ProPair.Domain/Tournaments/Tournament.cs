using ProPair.Domain.Common;
using ProPair.Domain.GameSystems;

namespace ProPair.Domain.Tournaments;

/// <param name="ListChangedAt">Set when a sync brought a different list for this player: ratings and notes were reset.</param>
/// <param name="IsDummy">Placeholder for a missing player or list.</param>
public sealed record Player(PlayerId Id, string Name, string ArmyId, string ListText, DateTimeOffset? ListChangedAt = null,
    bool IsDummy = false)
{
    public static Player Dummy(int number) =>
        new(PlayerId.New(), number == 1 ? "Dummy" : $"Dummy {number}", ArmyDetector.Unknown, "", IsDummy: true);
}

/// <summary>A team taking part in a tournament, with its roster.</summary>
public sealed class Participant(ParticipantId id, string name, IReadOnlyList<Player> roster,
    IReadOnlyList<string>? leftOut = null)
{
    public ParticipantId Id { get; } = id;
    public string Name { get; } = name;
    public IReadOnlyList<Player> Roster { get; private set; } = roster;

    /// <summary>Players with a list who did not fit into the roster.</summary>
    public IReadOnlyList<string> LeftOut { get; private set; } = leftOut ?? [];

    /// <summary>The source did not deliver exactly one list per place – the matrix may be wrong.</summary>
    public bool IsIncomplete => LeftOut.Count > 0 || Roster.Any(p => p.IsDummy);

    internal void ReplaceRoster(IReadOnlyList<Player> roster, IReadOnlyList<string>? leftOut)
    {
        Roster = roster;
        LeftOut = leftOut ?? [];
    }
}

/// <summary>
/// A team tournament, usually synced from New Recruit. Rosters may be empty while the organiser keeps the lists hidden.
/// </summary>
public sealed class Tournament : AggregateRoot<TournamentId>
{
    private readonly List<Participant> _participants;

    public string ExternalId { get; }
    public string Name { get; private set; }
    public string GameSystemId { get; }

    /// <summary>Players per team; null while no roster is known.</summary>
    public TeamSize? TeamSize { get; private set; }

    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }

    /// <summary>False while the organiser hides the lists; the last seen rosters are kept.</summary>
    public bool ListsVisible { get; private set; }

    public DateTimeOffset? LastSyncedAt { get; private set; }
    public IReadOnlyList<Participant> Participants => _participants;

    public Tournament(TournamentId id, string externalId, string name, string gameSystemId, TeamSize? teamSize,
        DateOnly? startDate, IEnumerable<Participant> participants, DateOnly? endDate = null, bool listsVisible = true,
        DateTimeOffset? lastSyncedAt = null) : base(id)
    {
        ExternalId = externalId;
        Name = name;
        GameSystemId = gameSystemId;
        TeamSize = teamSize;
        StartDate = startDate;
        EndDate = endDate;
        ListsVisible = listsVisible;
        LastSyncedAt = lastSyncedAt;
        _participants = participants.ToList();
        foreach (var p in _participants) EnsureRosterSize(p.Roster);
        TeamSize ??= NominalSize();
    }

    public Participant GetParticipant(ParticipantId id) =>
        _participants.FirstOrDefault(p => p.Id == id) ?? throw new DomainException("Unknown participant.");

    /// <summary>
    /// Sync from the source. Teams are matched by name, players by name within their team.
    /// <list type="bullet">
    /// <item>Lists hidden: rosters stay as last seen (lists can be closed and reopened), new teams are added empty.</item>
    /// <item>Same list as before: the player keeps its id – ratings and notes stay.</item>
    /// <item>Different list: the player gets a new id, so ratings and notes of the old list no longer apply,
    ///   and <see cref="Player.ListChangedAt"/> tells the team to rate again.</item>
    /// <item>Lists visible: empty teams that are no longer delivered are removed.</item>
    /// </list>
    /// </summary>
    public void Update(string name, DateOnly? startDate, DateOnly? endDate, bool listsVisible,
        IReadOnlyList<Lineup> lineups, DateTimeOffset now)
    {
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        ListsVisible = listsVisible;
        LastSyncedAt = now;

        if (listsVisible)
            _participants.RemoveAll(p => p.Roster.Count == 0 && lineups.All(l => l.Name != p.Name));

        foreach (var (participantName, roster, leftOut) in lineups)
        {
            EnsureRosterSize(roster);
            var existing = _participants.FirstOrDefault(p => p.Name == participantName);
            if (existing is null)
            {
                _participants.Add(listsVisible
                    ? new Participant(ParticipantId.New(), participantName, roster, leftOut)
                    : new Participant(ParticipantId.New(), participantName, []));
                continue;
            }
            if (!listsVisible) continue; // keep the last seen lists

            var merged = roster.Select(p => existing.Roster.FirstOrDefault(o => o.Name == p.Name && o.IsDummy == p.IsDummy) switch
            {
                null => p,
                var old when ArmyList.Same(old.ListText, p.ListText) => p with { Id = old.Id, ListChangedAt = old.ListChangedAt },
                _ => p with { Id = PlayerId.New(), ListChangedAt = now },
            }).ToList();
            existing.ReplaceRoster(merged, leftOut);
        }
        TeamSize = NominalSize() ?? TeamSize;
    }

    /// <summary>Old signature for re-imports without visibility info (demo source).</summary>
    public void Update(string name, DateOnly? startDate, IReadOnlyList<Lineup> lineups) =>
        Update(name, startDate, EndDate, true, lineups, DateTimeOffset.UtcNow);

    private TeamSize? NominalSize()
    {
        var largest = _participants.Select(p => p.Roster.Count).DefaultIfEmpty(0).Max();
        return largest >= Tournaments.TeamSize.Min ? new TeamSize(largest) : null;
    }

    private static void EnsureRosterSize(IReadOnlyList<Player> roster)
    {
        if (roster.Count > Tournaments.TeamSize.Max)
            throw new DomainException($"A roster can have at most {Tournaments.TeamSize.Max} players, has {roster.Count}.");
    }
}

/// <summary>Army list text helpers.</summary>
public static class ArmyList
{
    /// <summary>Same list, ignoring whitespace and the "Created with newrecruit.eu vX" footer.</summary>
    public static bool Same(string a, string b) => Normalize(a) == Normalize(b);

    public static string Normalize(string text) => string.Join('\n', text.Replace("\r", "").Split('\n')
        .Select(l => l.Trim())
        .Where(l => l.Length > 0 && !l.StartsWith("Created with", StringComparison.OrdinalIgnoreCase)));
}
