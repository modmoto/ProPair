using System.Diagnostics;
using ProPair.Domain.Common;
using ProPair.Domain.Pairing;
using ProPair.Domain.Pairing.Advice;

namespace ProPair.Domain.Tests;

public class GameTreeAdvisorTests
{
    private static readonly GameTreeAdvisor Nash = new(new EquilibriumSolver());
    private static readonly GameTreeAdvisor Pessimistic = new(new PessimisticSolver());

    private static (List<PlayerId> Own, List<PlayerId> Opp, Func<PlayerId, PlayerId, double> Score) Random(int n, int seed)
    {
        var rnd = new Random(seed);
        var own = Enumerable.Range(0, n).Select(_ => PlayerId.New()).ToList();
        var opp = Enumerable.Range(0, n).Select(_ => PlayerId.New()).ToList();
        var table = own.SelectMany(o => opp.Select(p => (o, p))).ToDictionary(k => k, _ => (double)rnd.Next(-2, 3));
        return (own, opp, (o, p) => table[(o, p)]);
    }

    /// <summary>Reference straight over the domain rules: we commit first (lower) or they commit first (upper).</summary>
    private static double BruteForce(PairingState s, Func<PlayerId, PlayerId, double> score, bool weFirst)
    {
        if (s.Step == PairingStep.Done) return s.Games.Sum(g => score(g.Own, g.Opponent));
        var (own, opp, move) = Moves(s);
        double Cell(int r, int c) => BruteForce(s.Apply(move(r, c)), score, weFirst);
        return weFirst
            ? Enumerable.Range(0, own).Max(r => Enumerable.Range(0, opp).Min(c => Cell(r, c)))
            : Enumerable.Range(0, opp).Min(c => Enumerable.Range(0, own).Max(r => Cell(r, c)));
    }

    private static (int, int, Func<int, int, PairingMove>) Moves(PairingState s)
    {
        static List<(PlayerId A, PlayerId B)> Pairs(IEnumerable<PlayerId> p)
        {
            var l = p.ToList();
            return [.. from i in Enumerable.Range(0, l.Count) from j in Enumerable.Range(i + 1, l.Count - i - 1) select (l[i], l[j])];
        }
        switch (s.Step)
        {
            case PairingStep.Defenders:
                return (s.RemainingOwn.Count, s.RemainingOpponent.Count,
                    (r, c) => new DefendersMove(s.RemainingOwn[r], s.RemainingOpponent[c]));
            case PairingStep.Attackers:
                var own = Pairs(s.RemainingOwn.Where(p => p != s.OwnDefender));
                var opp = Pairs(s.RemainingOpponent.Where(p => p != s.OpponentDefender));
                return (own.Count, opp.Count, (r, c) => new AttackersMove(own[r].A, own[r].B, opp[c].A, opp[c].B));
            default:
                return (2, 2, (r, c) => new ChoicesMove(s.OpponentAttackers[r], s.OwnAttackers[c]));
        }
    }

    [Theory]
    [InlineData(3, 1)]
    [InlineData(3, 2)]
    [InlineData(5, 3)]
    [InlineData(5, 4)]
    public void Pessimistic_tree_matches_brute_force(int n, int seed)
    {
        var (own, opp, score) = Random(n, seed);
        var start = PairingState.Start(own, opp);

        Assert.Equal(BruteForce(start, score, weFirst: true), Pessimistic.Advise(start, score).Expected, 6);
    }

    [Theory]
    [InlineData(3, 1)]
    [InlineData(3, 5)]
    [InlineData(5, 3)]
    [InlineData(5, 6)]
    public void Equilibrium_lies_between_pessimistic_and_optimistic(int n, int seed)
    {
        var (own, opp, score) = Random(n, seed);
        var start = PairingState.Start(own, opp);

        var nash = Nash.Advise(start, score).Expected;

        Assert.InRange(nash, BruteForce(start, score, weFirst: true) - 1e-6, BruteForce(start, score, weFirst: false) + 1e-6);
    }

    [Fact]
    public void Obvious_defender_is_recommended()
    {
        var own = Enumerable.Range(0, 3).Select(_ => PlayerId.New()).ToList();
        var opp = Enumerable.Range(0, 3).Select(_ => PlayerId.New()).ToList();
        // own[0] wins only as long as he is not refused: defending with him guarantees his +2
        var advice = Nash.Advise(PairingState.Start(own, opp), (o, _) => o == own[0] ? 2 : 0);

        Assert.Equal(2, advice.Expected, 6);
        Assert.Equal(3, advice.Own.Count);
        Assert.Equal(3, advice.Opponent.Count);
    }

    [Fact]
    public void Advice_mid_round_lists_attacker_pairs_and_paths_reach_the_end()
    {
        var (own, opp, score) = Random(5, 11);
        var s = PairingState.Start(own, opp).Apply(new DefendersMove(own[0], opp[0]));

        var advice = Nash.Advise(s, score);

        Assert.Equal(6, advice.Own.Count); // C(4,2)
        Assert.All(advice.Own, o => Assert.Equal(2, o.Players.Count));
        Assert.Equal(1, advice.Own.Sum(o => o.Probability), 6);
        Assert.Equal(3, advice.TopPicks.Count);
        Assert.All(advice.TopPicks, p =>
        {
            Assert.Equal(5, p.Games.Count);
            Assert.Equal(p.Games.Sum(g => g.Score), p.Total, 6);
            Assert.Equal(p.Option.Players, p.Steps[0].Own);
            Assert.Equal(PairingStep.Choices, p.Steps[^1].Step);
        });
    }

    [Fact]
    public void Probabilities_and_expected_values_are_consistent()
    {
        var (own, opp, score) = Random(5, 21);
        var advice = Nash.Advise(PairingState.Start(own, opp), score);

        Assert.Equal(1, advice.Own.Sum(o => o.Probability), 6);
        Assert.Equal(1, advice.Opponent.Sum(o => o.Probability), 6);
        // every option we would actually play is worth the game value against their mix
        Assert.All(advice.Own.Where(o => o.Probability > 1e-6), o => Assert.Equal(advice.Expected, o.Expected, 5));
        Assert.All(advice.Own, o => Assert.True(o.Expected <= advice.Expected + 1e-6));
        Assert.All(advice.Own, o => Assert.True(o.Worst <= o.Expected + 1e-6));
    }

    [Fact]
    public void Done_state_returns_fixed_score()
    {
        var (own, opp, score) = Random(3, 9);
        var s = PairingState.Start(own, opp)
            .Apply(new DefendersMove(own[0], opp[0]))
            .Apply(new AttackersMove(own[1], own[2], opp[1], opp[2]))
            .Apply(new ChoicesMove(opp[1], own[1]));

        var advice = Nash.Advise(s, score);

        Assert.Equal(s.Games.Sum(g => score(g.Own, g.Opponent)), advice.Expected);
        Assert.Empty(advice.Own);
    }

    [Theory]
    [InlineData(42)]
    [InlineData(7)]
    public void Equilibrium_8v8_is_fast_enough(int seed)
    {
        var (own, opp, score) = Random(8, seed);
        var sw = Stopwatch.StartNew();

        var advice = Nash.Advise(PairingState.Start(own, opp), score);

        Assert.Equal(8, advice.Own.Count);
        Assert.Equal(3, advice.TopPicks.Count);
        Assert.All(advice.TopPicks, p => Assert.Equal(8, p.Games.Count));
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(3), $"took {sw.Elapsed}");
    }
}

public class AdvisorSwapTests
{
    /// <summary>The 3v3 example from the README.</summary>
    private static (List<PlayerId>, List<PlayerId>, Func<PlayerId, PlayerId, double>) Example()
    {
        double[,] m = { { 2, -1, 0 }, { -2, 1, 1 }, { 0, 0, -1 } };
        var own = Enumerable.Range(0, 3).Select(_ => PlayerId.New()).ToList();
        var opp = Enumerable.Range(0, 3).Select(_ => PlayerId.New()).ToList();
        return (own, opp, (o, p) => m[own.IndexOf(o), opp.IndexOf(p)]);
    }

    [Fact]
    public void Readme_example_nash_mixes_ben_and_anna()
    {
        var (own, opp, score) = Example();
        var advice = new GameTreeAdvisor(new EquilibriumSolver()).Advise(PairingState.Start(own, opp), score);

        Assert.Equal(0.4, advice.Expected, 6);
        Assert.Equal(own[1], advice.Own[0].Players[0]);           // Ben first: same value, safer
        Assert.Equal(0.8, advice.Own[0].Probability, 6);
        Assert.Equal(0.2, advice.Own.Single(o => o.Players[0] == own[0]).Probability, 6);
    }

    [Fact]
    public void Pessimistic_advisor_ranks_by_guaranteed_value()
    {
        var (own, opp, score) = Example();
        var advice = new GameTreeAdvisor(new PessimisticSolver()).Advise(PairingState.Start(own, opp), score);

        Assert.Equal(0, advice.Expected, 6);
        Assert.Equal(own[1], advice.Own[0].Players[0]);           // Ben guarantees 0, Anna risks −2
        Assert.All(advice.Own, o => Assert.Equal(o.Worst, o.Expected, 6));
    }
}

public class WholeTeamThinkingTests
{
    /// <summary>
    /// Choice step: their attackers on our defender are Q1 (we'd score 0/+ = +1) and Q2 (0).
    /// Q2 crushes everyone else in our team (−2), Q1 loses to everyone else (+1).
    /// Taking the "better" 0/+ sends Q2 back into the pool – so the advisor must prefer the 0.
    /// </summary>
    [Fact]
    public void Takes_the_zero_to_remove_a_dangerous_attacker_from_the_pool()
    {
        var own = Enumerable.Range(0, 5).Select(_ => PlayerId.New()).ToList(); // D, A1, A2, O3, O4
        var opp = Enumerable.Range(0, 5).Select(_ => PlayerId.New()).ToList(); // E, Q1, Q2, R3, R4
        PlayerId d = own[0], q1 = opp[1], q2 = opp[2];
        double Score(PlayerId o, PlayerId p) =>
            o == d ? (p == q1 ? 1 : 0)
            : p == q2 ? -2
            : p == q1 ? 1
            : 0;

        var state = PairingState.Start(own, opp)
            .Apply(new DefendersMove(d, opp[0]))
            .Apply(new AttackersMove(own[1], own[2], q1, q2));

        var advice = new GameTreeAdvisor(new EquilibriumSolver()).Advise(state, Score);

        Assert.Equal(q2, advice.Own[0].Players[0]);
        var takeQ1 = advice.Own.Single(o => o.Players[0] == q1);
        Assert.True(advice.Own[0].Expected > takeQ1.Expected,
            $"take 0 → {advice.Own[0].Expected}, take 0/+ → {takeQ1.Expected}");
    }
}
