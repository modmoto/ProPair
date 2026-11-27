using ProPair.Domain.Common;

namespace ProPair.Domain.Tournaments;

public sealed record Player(PlayerId Id, string Name, string ArmyId, string ListText);

/// <summary>A team taking part in a tournament, with its roster.</summary>
public sealed class Participant(ParticipantId id, string name, IReadOnlyList<Player> roster)
{
    public ParticipantId Id { get; } = id;
    public string Name { get; } = name;
    public IReadOnlyList<Player> Roster { get; private set; } = roster;

    internal void ReplaceRoster(IReadOnlyList<Player> roster) => Roster = roster;
}

/// <summary>A team tournament, usually imported from an external source (New Recruit).</summary>
public sealed class Tournament : AggregateRoot<TournamentId>
{
    private readonly List<Participant> _participants;

    public string ExternalId { get; }
    public string Name { get; private set; }
    public string GameSystemId { get; }
    public TeamSize TeamSize { get; }
    public DateOnly? StartDate { get; private set; }
    public IReadOnlyList<Participant> Participants => _participants;

    public Tournament(TournamentId id, string externalId, string name, string gameSystemId, TeamSize teamSize,
        DateOnly? startDate, IEnumerable<Participant> participants) : base(id)
    {
        ExternalId = externalId;
        Name = name;
        GameSystemId = gameSystemId;
        TeamSize = teamSize;
        StartDate = startDate;
        _participants = participants.ToList();
        foreach (var p in _participants) EnsureRosterSize(p.Roster);
    }

    public Participant GetParticipant(ParticipantId id) =>
        _participants.FirstOrDefault(p => p.Id == id) ?? throw new DomainException("Unknown participant.");

    /// <summary>Re-import: updates name/date and rosters. Participants are matched by name so ids stay stable.</summary>
    public void Update(string name, DateOnly? startDate, IEnumerable<(string Name, IReadOnlyList<Player> Roster)> participants)
    {
        Name = name;
        StartDate = startDate;
        foreach (var (participantName, roster) in participants)
        {
            EnsureRosterSize(roster);
            var existing = _participants.FirstOrDefault(p => p.Name == participantName);
            if (existing is null)
            {
                _participants.Add(new Participant(ParticipantId.New(), participantName, roster));
                continue;
            }

            // keep player ids stable when name + army match, so matrix cells survive a re-import
            var merged = roster.Select(p =>
                existing.Roster.FirstOrDefault(o => o.Name == p.Name && o.ArmyId == p.ArmyId) is { } old
                    ? p with { Id = old.Id }
                    : p).ToList();
            existing.ReplaceRoster(merged);
        }
    }

    private void EnsureRosterSize(IReadOnlyList<Player> roster)
    {
        if (roster.Count != TeamSize.Value)
            throw new DomainException($"Roster must have {TeamSize.Value} players, has {roster.Count}.");
    }
}
