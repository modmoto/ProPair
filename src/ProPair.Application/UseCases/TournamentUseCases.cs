using ProPair.Application.Ports;
using ProPair.Domain.Common;
using ProPair.Domain.GameSystems;
using ProPair.Domain.Tournaments;

namespace ProPair.Application.UseCases;

/// <summary>Demo tournaments offered by the (fake) source – for trying the app without New Recruit.</summary>
public sealed class SearchSourceTournaments(ITournamentSource source, IClock clock)
{
    public Task<IReadOnlyList<SourceTournamentSummary>> ExecuteAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(clock.Now.UtcDateTime);
        return source.SearchAsync(today.AddYears(-1), today.AddYears(1), ct);
    }
}

/// <summary>
/// Copies one team tournament from the source into our database (new or update). The domain decides what an
/// update means: hidden lists keep the last seen rosters, a changed list resets that player.
/// Returns null for tournaments that are not team tournaments.
/// </summary>
public sealed class ImportTournament(ITournamentSource source, ITournamentRepository tournaments, IClock clock)
{
    public async Task<Tournament?> ExecuteAsync(string externalId, SourceTournamentSummary? summary = null,
        CancellationToken ct = default)
    {
        var data = await source.GetAsync(externalId, ct) ?? throw new NotFoundException($"Tournament {externalId}");
        if (!data.IsTeamTournament) return null;
        var start = summary?.StartDate ?? data.StartDate;
        var end = summary?.EndDate ?? data.EndDate;
        var registered = data.Teams
            .Select(t => new RegisteredTeam(t.Name, t.Players.Count, t.Players.Where(HasList).Select(ToPlayer).ToList()))
            .ToList();
        // hidden lists: we only learn the team names, the domain keeps the last seen rosters
        var lineups = data.ListsVisible
            ? Lineups.Build(registered)
            : registered.Select(t => new Lineup(t.Name, [])).ToList();
        if (lineups is null) return null; // 2v2 and the like

        var tournament = await tournaments.FindByExternalIdAsync(externalId, ct);
        if (tournament is null)
        {
            tournament = new Tournament(TournamentId.New(), data.ExternalId, data.Name, GameSystem.NinthAge.Id, null, start,
                lineups.Select(l => new Participant(ParticipantId.New(), l.Name, l.Roster, l.LeftOut)),
                end, data.ListsVisible, clock.Now);
        }
        else
        {
            // a single-tournament sync has no summary: keep the dates we know
            tournament.Update(data.Name, start ?? tournament.StartDate, end ?? tournament.EndDate, data.ListsVisible,
                lineups, clock.Now);
        }

        await tournaments.SaveAsync(tournament, ct);
        return tournament;
    }

    /// <summary>Separates players from coaches, judges and NPCs: only players have a list.</summary>
    private static bool HasList(SourcePlayer p) => p.ListText.Length > 0;

    private static Player ToPlayer(SourcePlayer p) => new(PlayerId.New(), p.Name, p.ArmyId, p.ListText);
}

public sealed record SyncReport(int Found, int Synced, int Skipped, IReadOnlyList<string> Failed);

/// <summary>
/// Pulls team tournaments from the source. Only what is still to come: closed or finished tournaments and leagues
/// (running longer than <see cref="MaxEventDays"/> days) are skipped. Requests go one by one.
/// </summary>
public sealed class SyncTournaments(ITournamentSource source, ImportTournament import, IClock clock)
{
    public const int MaxEventDays = 4;
    public const int MonthsAhead = 12;

    public Task<SyncReport> UpcomingAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(clock.Now.UtcDateTime);
        return RangeAsync(today, today.AddMonths(MonthsAhead), includePast: false, ct);
    }

    /// <summary>includePast: also finished/closed tournaments – only for testing with old events that have open lists.</summary>
    public async Task<SyncReport> RangeAsync(DateOnly from, DateOnly to, bool includePast, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(clock.Now.UtcDateTime);
        var found = await source.SearchAsync(from, to, ct);
        var wanted = found
            .Where(t => !IsLeague(t))
            .Where(t => includePast || (t.Status != SourceStatus.Closed && (t.EndDate ?? t.StartDate ?? today) >= today))
            .ToList();

        int synced = 0, skipped = found.Count - wanted.Count;
        var failed = new List<string>();
        foreach (var summary in wanted)
        {
            try
            {
                if (await import.ExecuteAsync(summary.ExternalId, summary, ct) is null) skipped++; // not a team tournament
                else synced++;
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                failed.Add($"{summary.ExternalId} {summary.Name}: {e.Message}"); // one broken tournament must not stop the rest
            }
        }
        return new SyncReport(found.Count, synced, skipped, failed);
    }

    public async Task<bool> OneAsync(string externalId, CancellationToken ct = default) =>
        await import.ExecuteAsync(externalId, null, ct) is not null;

    private static bool IsLeague(SourceTournamentSummary t) =>
        t is { StartDate: { } start, EndDate: { } end } && end.DayNumber - start.DayNumber > MaxEventDays;
}

/// <summary>Entry points for the background jobs (Hangfire calls these in the sync process).</summary>
public sealed class TournamentSyncJob(SyncTournaments sync)
{
    public Task<SyncReport> SyncUpcomingAsync() => sync.UpcomingAsync();

    public Task<bool> SyncOneAsync(string externalId) => sync.OneAsync(externalId);

    /// <param name="from">yyyy-MM-dd</param><param name="to">yyyy-MM-dd</param>
    public Task<SyncReport> BackfillAsync(string from, string to) =>
        sync.RangeAsync(DateOnly.Parse(from), DateOnly.Parse(to), includePast: true);
}

/// <summary>"Update now" from the UI: the sync process does the work.</summary>
public sealed class RequestTournamentSync(ITournamentRepository tournaments, ISyncQueue queue)
{
    public async Task ExecuteAsync(TournamentId id, CancellationToken ct = default)
    {
        var tournament = await tournaments.GetAsync(id, ct) ?? throw new NotFoundException("Tournament");
        if (tournament.ExternalId.StartsWith(FillDemoRatings.DemoPrefix))
            throw new DomainException("Demo tournaments are not synced with New Recruit.");
        queue.RequestTournamentSync(tournament.ExternalId);
    }
}

public sealed class ListTournaments(ITournamentRepository tournaments)
{
    public Task<IReadOnlyList<Tournament>> ExecuteAsync(CancellationToken ct = default) => tournaments.ListAsync(ct);
}

public sealed class GetTournament(ITournamentRepository tournaments)
{
    public async Task<Tournament> ExecuteAsync(TournamentId id, CancellationToken ct = default) =>
        await tournaments.GetAsync(id, ct) ?? throw new NotFoundException("Tournament");
}
