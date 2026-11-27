using ProPair.Application.Ports;
using ProPair.Domain.Common;
using ProPair.Domain.GameSystems;
using ProPair.Domain.Tournaments;

namespace ProPair.Application.UseCases;

public sealed class SearchSourceTournaments(ITournamentSource source)
{
    public Task<IReadOnlyList<SourceTournamentSummary>> ExecuteAsync(CancellationToken ct = default) =>
        source.SearchAsync(ct);
}

/// <summary>Imports a tournament from the external source, or refreshes it if it was imported before.</summary>
public sealed class ImportTournament(ITournamentSource source, ITournamentRepository tournaments)
{
    public async Task<Tournament> ExecuteAsync(string externalId, CancellationToken ct = default)
    {
        var data = await source.GetAsync(externalId, ct) ?? throw new NotFoundException($"Tournament {externalId}");
        var system = GameSystem.NinthAge;

        var participants = data.Teams
            .Select(t => (t.Name, Roster: (IReadOnlyList<Player>)t.Players.Select(p => ToPlayer(p, system)).ToList()))
            .ToList();

        var tournament = await tournaments.FindByExternalIdAsync(externalId, ct);
        if (tournament is null)
        {
            tournament = new Tournament(TournamentId.New(), data.ExternalId, data.Name, system.Id,
                new TeamSize(data.TeamSize), data.StartDate,
                participants.Select(p => new Participant(ParticipantId.New(), p.Name, p.Roster)));
        }
        else
        {
            tournament.Update(data.Name, data.StartDate, participants);
        }

        await tournaments.SaveAsync(tournament, ct);
        return tournament;
    }

    private static Player ToPlayer(SourcePlayer p, GameSystem system)
    {
        var army = system.FindArmy(p.ArmyName) ?? throw new DomainException($"Unknown army '{p.ArmyName}'.");
        return new Player(PlayerId.New(), p.Name, army.Id, p.ListText);
    }
}
