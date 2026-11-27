namespace ProPair.Application.Ports;

/// <summary>External tournament source (New Recruit). Returns raw data; mapping to the domain happens in the use case.</summary>
public interface ITournamentSource
{
    Task<IReadOnlyList<SourceTournamentSummary>> SearchAsync(CancellationToken ct = default);
    Task<SourceTournament?> GetAsync(string externalId, CancellationToken ct = default);
}

public sealed record SourceTournamentSummary(string ExternalId, string Name, DateOnly? StartDate, int TeamSize, int TeamCount);

public sealed record SourceTournament(
    string ExternalId, string Name, DateOnly? StartDate, int TeamSize, IReadOnlyList<SourceTeam> Teams);

public sealed record SourceTeam(string Name, IReadOnlyList<SourcePlayer> Players);

public sealed record SourcePlayer(string Name, string ArmyName, string ListText);
