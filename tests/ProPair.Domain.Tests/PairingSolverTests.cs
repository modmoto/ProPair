using System.Diagnostics;
using ProPair.Domain.Common;
using ProPair.Domain.Pairing;

namespace ProPair.Domain.Tests;

public class PairingSolverTests
{
    private static (List<PlayerId> Own, List<PlayerId> Opp, Func<PlayerId, PlayerId, double> Score) Random(int n, int seed)
    {
        var rnd = new Random(seed);
        var own = Enumerable.Range(0, n).Select(_ => PlayerId.New()).ToList();
        var opp = Enumerable.Range(0, n).Select(_ => PlayerId.New()).ToList();
        var table = own.SelectMany(o => opp.Select(p => (o, p))).ToDictionary(k => k, _ => (double)rnd.Next(-2, 3));
        return (own, opp, (o, p) => table[(o, p)]);
    }

    /// <summary>Independent reference: maximin straight over PairingState.Apply, no bitmasks, no pruning.</summary>
    private static double BruteForce(PairingState s, Func<PlayerId, PlayerId, double> score)
    {
        if (s.Step == PairingStep.Done) return s.Games.Sum(g => score(g.Own, g.Opponent));
        return s.Step switch
        {
            PairingStep.Defenders => s.RemainingOwn.Max(d => s.RemainingOpponent.Min(e =>
                BruteForce(s.Apply(new DefendersMove(d, e)), score))),
            PairingStep.Attackers => Pairs(s.RemainingOwn.Where(p => p != s.OwnDefender)).Max(p =>
                Pairs(s.RemainingOpponent.Where(q => q != s.OpponentDefender)).Min(q =>
                    BruteForce(s.Apply(new AttackersMove(p.A, p.B, q.A, q.B)), score))),
            _ => s.OpponentAttackers.Max(q => s.OwnAttackers.Min(p => BruteForce(s.Apply(new ChoicesMove(q, p)), score))),
        };
    }

    private static IEnumerable<(PlayerId A, PlayerId B)> Pairs(IEnumerable<PlayerId> players)
    {
        var list = players.ToList();
        for (var i = 0; i < list.Count; i++)
        for (var j = i + 1; j < list.Count; j++)
            yield return (list[i], list[j]);
    }

    [Theory]
    [InlineData(3, 1)]
    [InlineData(3, 2)]
    [InlineData(5, 3)]
    [InlineData(5, 4)]
    [InlineData(5, 5)]
    public void Solver_matches_brute_force(int n, int seed)
    {
        var (own, opp, score) = Random(n, seed);
        var start = PairingState.Start(own, opp);

        var analysis = PairingSolver.Analyze(start, score);

        Assert.Equal(BruteForce(start, score), analysis.Expected, 6);
    }

    [Fact]
    public void Solver_matches_brute_force_mid_pairing()
    {
        var (own, opp, score) = Random(5, 7);
        var s = PairingState.Start(own, opp).Apply(new DefendersMove(own[2], opp[3]));

        var analysis = PairingSolver.Analyze(s, score);

        Assert.Equal(BruteForce(s, score), analysis.Expected, 6);
        Assert.Equal(6, analysis.Options.Count); // C(4,2) attacker pairs
        Assert.All(analysis.Options, o => Assert.True(o.Average >= o.Worst));
    }

    [Fact]
    public void Solver_recommends_obvious_defender()
    {
        var own = Enumerable.Range(0, 3).Select(_ => PlayerId.New()).ToList();
        var opp = Enumerable.Range(0, 3).Select(_ => PlayerId.New()).ToList();
        // own[0] beats everyone, the rest is even: defending with own[0] guarantees +2
        var analysis = PairingSolver.Analyze(PairingState.Start(own, opp), (o, _) => o == own[0] ? 2 : 0);

        Assert.Equal(2, analysis.Expected);
        Assert.All(analysis.Options, o => Assert.Equal(2, o.Worst));
    }

    [Fact]
    public void Solver_handles_8v8_quickly()
    {
        var (own, opp, score) = Random(8, 42);
        var sw = Stopwatch.StartNew();

        var analysis = PairingSolver.Analyze(PairingState.Start(own, opp), score);

        Assert.Equal(8, analysis.Options.Count);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"took {sw.Elapsed}");
    }

    [Fact]
    public void Done_state_returns_fixed_score()
    {
        var (own, opp, score) = Random(3, 9);
        var s = PairingState.Start(own, opp)
            .Apply(new DefendersMove(own[0], opp[0]))
            .Apply(new AttackersMove(own[1], own[2], opp[1], opp[2]))
            .Apply(new ChoicesMove(opp[1], own[1]));

        var analysis = PairingSolver.Analyze(s, score);

        Assert.Equal(s.Games.Sum(g => score(g.Own, g.Opponent)), analysis.Expected);
        Assert.Empty(analysis.Options);
    }
}
