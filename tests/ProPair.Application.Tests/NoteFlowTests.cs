using ProPair.Adapters.NewRecruit.Fake;
using ProPair.Application.UseCases;
using ProPair.Domain.Common;
using ProPair.Domain.Matrices;

namespace ProPair.Application.Tests;

public class NoteFlowTests
{
    private readonly InMemoryStore _store = new();
    private GetMatrix GetMatrix => new(_store, _store, _store);

    [Fact]
    public async Task Notes_and_history_round_trip_and_read_link_is_blocked()
    {
        var t = (await new ImportTournament(new FakeTournamentSource(), _store, new FixedClock()).ExecuteAsync("demo-trio-2026"))!;
        var own = t.Participants[0];
        var opp = t.Participants[1];
        var prep = await new StartPreparation(_store, _store, _store).ExecuteAsync(t.Id, own.Id);
        var edit = prep.EditToken.Value;
        var cell = new CellKey(own.Roster[0].Id, opp.Roster[0].Id);

        await new SetRating(GetMatrix, _store, new FixedClock()).ExecuteAsync(edit, opp.Id, cell, "plus", "A");
        await new SetRating(GetMatrix, _store, new FixedClock()).ExecuteAsync(edit, opp.Id, cell, "zero", "B");
        await new SetCellNote(GetMatrix, _store, new FixedClock()).ExecuteAsync(edit, opp.Id, cell, "Tisch mit Wald!", "A");
        await new SetPlayerNote(_store, _store, new FixedClock()).ExecuteAsync(edit, opp.Roster[1].Id, "Achtung Flieger", "A");

        var view = await GetMatrix.ExecuteAsync(prep.ReadToken.Value, opp.Id);
        Assert.Equal("Tisch mit Wald!", view.Matrix.NoteAt(cell)!.Text);
        Assert.Equal("Achtung Flieger", view.Preparation.PlayerNotes[opp.Roster[1].Id].Text);

        var history = await new GetCellHistory(GetMatrix).ExecuteAsync(prep.ReadToken.Value, opp.Id, cell);
        Assert.Equal(2, history.Count);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            new SetCellNote(GetMatrix, _store, new FixedClock()).ExecuteAsync(prep.ReadToken.Value, opp.Id, cell, "x", "E"));
        await Assert.ThrowsAsync<DomainException>(() =>
            new SetPlayerNote(_store, _store, new FixedClock()).ExecuteAsync(edit, PlayerId.New(), "x", "A"));
    }
}
