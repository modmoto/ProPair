using ProPair.Adapters.NewRecruit.Fake;
using ProPair.Application.UseCases;
using ProPair.Domain.Common;
using ProPair.Domain.Matrices;
using ProPair.Domain.Pairing;
using ProPair.Domain.Pairing.Advice;

namespace ProPair.Application.Tests;

public class PairingFlowTests
{
    private readonly InMemoryStore _store = new();

    private async Task<(string Edit, string Read, ParticipantId Opp, MatrixView View)> SetUpAsync()
    {
        var tournament = await new ImportTournament(new FakeTournamentSource(), _store).ExecuteAsync("demo-trio-2026");
        var own = tournament.Participants[0];
        var opp = tournament.Participants[1];
        var prep = await new StartPreparation(_store, _store, _store).ExecuteAsync(tournament.Id, own.Id);
        var getMatrix = new GetMatrix(_store, _store, _store);
        // own[0] wins every game: defending with him guarantees +2
        var setRating = new SetRating(getMatrix, _store, new FixedClock());
        foreach (var o in opp.Roster)
            await setRating.ExecuteAsync(prep.EditToken.Value, opp.Id, new CellKey(own.Roster[0].Id, o.Id), "plus", "Captain");
        return (prep.EditToken.Value, prep.ReadToken.Value, opp.Id, await getMatrix.ExecuteAsync(prep.EditToken.Value, opp.Id));
    }

    private GetPairing GetPairing => new(new GetMatrix(_store, _store, _store), _store, new DefaultPairingAdvisorProvider());

    [Fact]
    public async Task Pairing_runs_to_the_end_with_recommendations()
    {
        var (edit, _, oppId, view) = await SetUpAsync();
        var play = new PlayPairingMove(GetPairing, _store);
        var own = view.Own.Roster.Select(p => p.Id).ToList();
        var opp = view.Opponent.Roster.Select(p => p.Id).ToList();

        var withoutAdvice = await GetPairing.ExecuteAsync(edit, oppId);
        Assert.Null(withoutAdvice.Advice); // advice only on request

        var start = await GetPairing.ExecuteAsync(edit, oppId, AdviceMode.Optimal);
        Assert.Equal(2, start.Advice!.Expected, 6);
        Assert.Equal(3, start.Advice.Own.Count);
        Assert.Equal(6, start.UnratedCells);

        var avoid = await GetPairing.ExecuteAsync(edit, oppId, AdviceMode.AvoidMinus);
        Assert.Equal(AdviceMode.AvoidMinus, avoid.Mode);
        Assert.Equal(0, avoid.FixedScore); // standings never weighted

        await play.ExecuteAsync(edit, oppId, new DefendersMove(own[0], opp[0]));
        await play.ExecuteAsync(edit, oppId, new AttackersMove(own[1], own[2], opp[1], opp[2]));
        var done = await play.ExecuteAsync(edit, oppId, new ChoicesMove(opp[1], own[1]));

        Assert.Equal(PairingStep.Done, done.Session.State.Step);
        Assert.Equal(3, done.Session.State.Games.Count);
        Assert.Equal(2, done.FixedScore);

        var undone = await new UndoPairingMove(GetPairing, _store).ExecuteAsync(edit, oppId);
        Assert.Equal(PairingStep.Choices, undone.Session.State.Step);

        var restarted = await new RestartPairing(GetPairing, _store).ExecuteAsync(edit, oppId);
        Assert.Empty(restarted.Session.Moves);
    }

    [Fact]
    public async Task Read_link_can_watch_but_not_play()
    {
        var (_, read, oppId, view) = await SetUpAsync();

        var watched = await GetPairing.ExecuteAsync(read, oppId);
        Assert.Equal(PairingStep.Defenders, watched.Session.State.Step);

        await Assert.ThrowsAsync<ForbiddenException>(() => new PlayPairingMove(GetPairing, _store).ExecuteAsync(read, oppId,
            new DefendersMove(view.Own.Roster[0].Id, view.Opponent.Roster[0].Id)));
    }
}
