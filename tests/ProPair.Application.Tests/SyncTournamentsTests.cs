using ProPair.Application.Ports;
using ProPair.Application.UseCases;
using ProPair.Domain.Common;
using ProPair.Domain.Tournaments;

namespace ProPair.Application.Tests;

public class SyncTournamentsTests
{
    private static readonly DateOnly Today = new(2026, 11, 27);
    private readonly ScriptedSource _source = new();
    private readonly InMemoryStore _store = new();

    private SyncTournaments Sync() =>
        new(_source, new ImportTournament(_source, _store, new FixedClock()), new FixedClock());

    private const string EoS = "Empire of Sonnstahl\n400 - General\n";
    private const string OK = "Ogre Khans\n500 - Great Khan\n";

    [Fact]
    public async Task Upcoming_skips_closed_finished_leagues_and_singles()
    {
        _source.Add("next", Today.AddDays(10), Today.AddDays(11), SourceStatus.Open);
        _source.Add("closed", Today.AddDays(10), Today.AddDays(11), SourceStatus.Closed);
        _source.Add("past", Today.AddDays(-3), Today.AddDays(-2), SourceStatus.Ongoing);
        _source.Add("league", Today.AddDays(-30), Today.AddDays(60), SourceStatus.Ongoing);
        _source.Add("single", Today.AddDays(5), Today.AddDays(5), SourceStatus.Open, team: false);

        var report = await Sync().UpcomingAsync();

        Assert.Equal(1, report.Synced);
        Assert.Equal(4, report.Skipped);
        Assert.Equal(["next"], (await _store.ListAsync()).Select(t => t.ExternalId));
    }

    [Fact]
    public async Task Backfill_takes_finished_tournaments_too()
    {
        _source.Add("past", Today.AddDays(-40), Today.AddDays(-39), SourceStatus.Closed);

        var report = await Sync().RangeAsync(Today.AddYears(-1), Today, includePast: true);

        Assert.Equal(1, report.Synced);
        var t = Assert.Single(await _store.ListAsync());
        Assert.Equal("empire-of-sonnstahl", t.Participants[0].Roster[0].ArmyId);
    }

    [Fact]
    public async Task Changed_list_resets_only_that_player()
    {
        _source.Add("t", Today.AddDays(10), Today.AddDays(11), SourceStatus.Open);
        await Sync().OneAsync("t");
        var before = (await _store.FindByExternalIdAsync("t"))!.Participants[0].Roster.Select(p => p.Id).ToList();

        _source.SetList("t", player: 0, OK);
        await Sync().OneAsync("t");

        var roster = (await _store.FindByExternalIdAsync("t"))!.Participants[0].Roster;
        Assert.NotEqual(before[0], roster[0].Id);
        Assert.Equal(new FixedClock().Now, roster[0].ListChangedAt);
        Assert.Equal("ogre-khans", roster[0].ArmyId);
        Assert.Equal(before[1], roster[1].Id);
        Assert.Null(roster[1].ListChangedAt);
    }

    [Fact]
    public async Task Hidden_lists_keep_the_last_rosters()
    {
        _source.Add("t", Today.AddDays(10), Today.AddDays(11), SourceStatus.Open);
        await Sync().OneAsync("t");
        var ids = (await _store.FindByExternalIdAsync("t"))!.Participants[0].Roster.Select(p => p.Id).ToList();

        _source.HideLists("t");
        await Sync().OneAsync("t");

        var t = (await _store.FindByExternalIdAsync("t"))!;
        Assert.False(t.ListsVisible);
        Assert.Equal(ids, t.Participants[0].Roster.Select(p => p.Id));
    }

    [Fact]
    public async Task Update_request_goes_to_the_queue_but_not_for_demos()
    {
        var queue = new RecordingQueue();
        _source.Add("t", Today.AddDays(10), Today.AddDays(11), SourceStatus.Open);
        await Sync().OneAsync("t");
        var real = (await _store.FindByExternalIdAsync("t"))!;
        var demo = new Tournament(TournamentId.New(), "demo-x", "Demo", ProPair.Domain.GameSystems.GameSystem.NinthAge.Id, null, Today, []);
        await _store.SaveAsync(demo);

        var request = new RequestTournamentSync(_store, queue);
        await request.ExecuteAsync(real.Id);
        await Assert.ThrowsAsync<DomainException>(() => request.ExecuteAsync(demo.Id));

        Assert.Equal(["t"], queue.Requested);
    }

    private sealed class RecordingQueue : ISyncQueue
    {
        public List<string> Requested { get; } = [];
        public void RequestTournamentSync(string externalId) => Requested.Add(externalId);
    }

    /// <summary>Two teams of three; lists can be changed or hidden between syncs.</summary>
    private sealed class ScriptedSource : ITournamentSource
    {
        private readonly Dictionary<string, (SourceTournamentSummary Summary, SourceTournament Data)> _all = [];

        public void Add(string id, DateOnly start, DateOnly end, SourceStatus status, bool team = true)
        {
            var teams = new[] { "A", "B" }.Select(n => new SourceTeam(n,
                Enumerable.Range(0, 3).Select(i => new SourcePlayer($"{n}{i}", [i == 0 ? EoS : OK])).ToList())).ToList();
            _all[id] = (new SourceTournamentSummary(id, id, start, end, status),
                new SourceTournament(id, id, null, null, team, ListsVisible: true, teams));
        }

        public void SetList(string id, int player, string list)
        {
            var (summary, data) = _all[id];
            var first = data.Teams[0];
            var players = first.Players.Select((p, i) => i == player ? p with { Lists = [list] } : p).ToList();
            _all[id] = (summary, data with { Teams = [first with { Players = players }, .. data.Teams.Skip(1)] });
        }

        public void HideLists(string id)
        {
            var (summary, data) = _all[id];
            _all[id] = (summary, data with { ListsVisible = false, Teams = data.Teams.Select(t => t with { Players = [] }).ToList() });
        }

        public Task<IReadOnlyList<SourceTournamentSummary>> SearchAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SourceTournamentSummary>>(_all.Values.Select(v => v.Summary).ToList());

        public Task<SourceTournament?> GetAsync(string externalId, CancellationToken ct = default) =>
            Task.FromResult(_all.TryGetValue(externalId, out var v) ? v.Data : null);
    }
}
