using ProPair.Domain.Common;
using ProPair.Domain.Pairing;

namespace ProPair.Domain.Tests;

public class PairingStateTests
{
    private static List<PlayerId> Players(int n) => Enumerable.Range(0, n).Select(_ => PlayerId.New()).ToList();

    /// <summary>Plays a full pairing always picking the first available options.</summary>
    private static PairingState PlayThrough(int size)
    {
        var s = PairingState.Start(Players(size), Players(size));
        while (s.Step != PairingStep.Done)
        {
            s = s.Apply(new DefendersMove(s.RemainingOwn[0], s.RemainingOpponent[0]));
            s = s.Apply(new AttackersMove(s.RemainingOwn[1], s.RemainingOwn[2], s.RemainingOpponent[1], s.RemainingOpponent[2]));
            s = s.Apply(new ChoicesMove(s.OpponentAttackers[0], s.OwnAttackers[0]));
        }
        return s;
    }

    [Theory]
    [InlineData(3, 1)]
    [InlineData(5, 2)]
    [InlineData(8, 3)]
    public void Full_pairing_pairs_everyone_once(int size, int rounds)
    {
        var s = PlayThrough(size);

        Assert.Equal(size, s.Games.Count);
        Assert.Equal(size, s.Games.Select(g => g.Own).Distinct().Count());
        Assert.Equal(size, s.Games.Select(g => g.Opponent).Distinct().Count());
        Assert.Equal(rounds, s.Round);
    }

    [Fact]
    public void Refused_attacker_returns_to_pool_in_normal_round()
    {
        var own = Players(8);
        var opp = Players(8);
        var s = PairingState.Start(own, opp)
            .Apply(new DefendersMove(own[0], opp[0]))
            .Apply(new AttackersMove(own[1], own[2], opp[1], opp[2]))
            .Apply(new ChoicesMove(opp[1], own[2]));

        Assert.Equal([new Game(own[0], opp[1]), new Game(own[2], opp[0])], s.Games);
        Assert.Contains(own[1], s.RemainingOwn);
        Assert.Contains(opp[2], s.RemainingOpponent);
        Assert.Equal(6, s.RemainingOwn.Count);
        Assert.Equal(PairingStep.Defenders, s.Step);
    }

    [Fact]
    public void Final_round_with_four_pairs_refused_and_last()
    {
        var own = Players(4);
        var opp = Players(4);
        var s = PairingState.Start(own, opp)
            .Apply(new DefendersMove(own[0], opp[0]))
            .Apply(new AttackersMove(own[1], own[2], opp[1], opp[2]))
            .Apply(new ChoicesMove(opp[1], own[1]));

        Assert.Equal(PairingStep.Done, s.Step);
        Assert.Contains(new Game(own[2], opp[2]), s.Games); // refused vs refused
        Assert.Contains(new Game(own[3], opp[3]), s.Games); // last vs last
    }

    [Fact]
    public void Wrong_step_is_rejected() =>
        Assert.Throws<DomainException>(() =>
            PairingState.Start(Players(3), Players(3)).Apply(new ChoicesMove(PlayerId.New(), PlayerId.New())));

    [Fact]
    public void Defender_cannot_attack()
    {
        var own = Players(5);
        var opp = Players(5);
        var s = PairingState.Start(own, opp).Apply(new DefendersMove(own[0], opp[0]));

        Assert.Throws<DomainException>(() => s.Apply(new AttackersMove(own[0], own[1], opp[1], opp[2])));
    }

    [Fact]
    public void Session_undo_replays_moves()
    {
        var own = Players(5);
        var opp = Players(5);
        var session = PairingSession.Start(PreparationId.New(), ParticipantId.New(), own, opp);
        session.Apply(new DefendersMove(own[0], opp[0]));
        session.Apply(new AttackersMove(own[1], own[2], opp[1], opp[2]));

        session.Undo();

        Assert.Equal(PairingStep.Attackers, session.State.Step);
        Assert.Single(session.Moves);
    }
}
