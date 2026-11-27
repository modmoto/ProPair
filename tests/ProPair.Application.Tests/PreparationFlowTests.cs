using ProPair.Adapters.NewRecruit.Fake;
using ProPair.Application.UseCases;
using ProPair.Domain.Common;
using ProPair.Domain.Matrices;

namespace ProPair.Application.Tests;

public class PreparationFlowTests
{
    private readonly InMemoryStore _store = new();
    private readonly FakeTournamentSource _source = new();

    private GetMatrix GetMatrix => new(_store, _store, _store);

    [Fact]
    public async Task Fake_source_offers_all_demo_tournaments_and_they_import()
    {
        var sources = await new SearchSourceTournaments(_source).ExecuteAsync();
        Assert.Equal(4, sources.Count);

        foreach (var s in sources)
        {
            var t = await new ImportTournament(_source, _store).ExecuteAsync(s.ExternalId);
            Assert.Equal(s.TeamCount, t.Participants.Count);
        }
    }

    [Fact]
    public async Task Import_twice_keeps_ids()
    {
        var import = new ImportTournament(_source, _store);
        var first = await import.ExecuteAsync("demo-trio-2026");
        var playerId = first.Participants[0].Roster[0].Id;

        var second = await import.ExecuteAsync("demo-trio-2026");

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(playerId, second.Participants[0].Roster[0].Id);
    }

    [Fact]
    public async Task Captain_rates_a_cell_and_read_link_cannot()
    {
        var tournament = await new ImportTournament(_source, _store).ExecuteAsync("demo-etc-2026");
        var own = tournament.Participants[0];
        var opp = tournament.Participants[1];
        var prep = await new StartPreparation(_store, _store, _store).ExecuteAsync(tournament.Id, own.Id);
        var cell = new CellKey(own.Roster[0].Id, opp.Roster[0].Id);
        var setRating = new SetRating(_store, GetMatrix, _store, new FixedClock());

        await setRating.ExecuteAsync(prep.EditToken.Value, opp.Id, cell, "plus", "Simon");

        var view = await GetMatrix.ExecuteAsync(prep.ReadToken.Value, opp.Id);
        Assert.Equal("plus", view.Matrix.RatingAt(cell));
        Assert.Single(view.Matrix.History);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            setRating.ExecuteAsync(prep.ReadToken.Value, opp.Id, cell, "minus", "Eve"));

        var overview = await new GetPreparation(_store, _store, _store).ExecuteAsync(prep.EditToken.Value);
        Assert.Equal(5, overview.Opponents.Count);
        Assert.Equal(1, overview.Opponents.Single(o => o.Participant.Id == opp.Id).RatedCells);
    }

    [Fact]
    public async Task Starting_preparation_twice_returns_same_links()
    {
        var tournament = await new ImportTournament(_source, _store).ExecuteAsync("demo-dtm-2026");
        var start = new StartPreparation(_store, _store, _store);

        var a = await start.ExecuteAsync(tournament.Id, tournament.Participants[0].Id);
        var b = await start.ExecuteAsync(tournament.Id, tournament.Participants[0].Id);

        Assert.Equal(a.EditToken, b.EditToken);
    }

    [Fact]
    public async Task Unknown_token_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => new GetPreparation(_store, _store, _store).ExecuteAsync("nope"));
}
