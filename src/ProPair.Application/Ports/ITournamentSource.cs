namespace ProPair.Application.Ports;

/// <summary>External tournament source (New Recruit). Returns raw data; mapping to the domain happens in the use cases.</summary>
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

/// <param name="Lists">Raw list texts as the source delivers them (a player can have more than one).</param>
public sealed record SourcePlayer(string Name, IReadOnlyList<string> Lists);
