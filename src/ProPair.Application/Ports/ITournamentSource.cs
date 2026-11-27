namespace ProPair.Application.Ports;

/// <summary>
/// External tournament source (New Recruit). The adapter translates its formats (anti-corruption layer);
/// what to keep is decided in the use cases.
/// </summary>
public interface ITournamentSource
{
    /// <summary>Tournaments of our game system that overlap the period.</summary>
    Task<IReadOnlyList<SourceTournamentSummary>> SearchAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<SourceTournament?> GetAsync(string externalId, CancellationToken ct = default);
}

public enum SourceStatus { Pending = 0, Open = 1, Ongoing = 2, Closed = 3 }

public sealed record SourceTournamentSummary(string ExternalId, string Name, DateOnly? StartDate, DateOnly? EndDate, SourceStatus Status);

/// <param name="ListsVisible">False while the organiser hides the lists – teams may then come without players.</param>
public sealed record SourceTournament(string ExternalId, string Name, DateOnly? StartDate, DateOnly? EndDate,
    bool IsTeamTournament, bool ListsVisible, IReadOnlyList<SourceTeam> Teams);

public sealed record SourceTeam(string Name, IReadOnlyList<SourcePlayer> Players);

/// <param name="ListText">Clean army list, one unit per line; empty if the member has none.</param>
public sealed record SourcePlayer(string Name, string ArmyId, string ListText);
