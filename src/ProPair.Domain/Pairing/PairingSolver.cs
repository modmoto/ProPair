using System.Numerics;
using ProPair.Domain.Common;

namespace ProPair.Domain.Pairing;

/// <summary>Evaluation of one of our possible moves in the current step.</summary>
/// <param name="Players">Defender (1), attacker pair (2) or accepted opponent attacker (1).</param>
/// <param name="Worst">Total score if the opponent answers optimally (pessimistic, maximin).</param>
/// <param name="Average">Total score averaged over all opponent answers.</param>
public sealed record PairingOption(IReadOnlyList<PlayerId> Players, double Worst, double Average);

/// <param name="Fixed">Score of games already paired.</param>
/// <param name="Expected">Best guaranteed total from here (maximin), equals Fixed when done.</param>
/// <param name="Options">Our options for the current step, best first.</param>
public sealed record PairingAnalysis(double Fixed, double Expected, IReadOnlyList<PairingOption> Options);

/// <summary>
/// Searches the complete pairing tree. Simultaneous steps are treated pessimistically:
/// we choose first, the opponent answers knowing our choice (maximin). The opponent's
/// view of a game is assumed to be the negated own rating (zero-sum).
/// Memoized over remaining-player bitmasks; 8v8 is a few million evaluations.
/// </summary>
public static class PairingSolver
{
    public static PairingAnalysis Analyze(PairingState state, Func<PlayerId, PlayerId, double> score)
    {
        var fixedScore = state.Games.Sum(g => score(g.Own, g.Opponent));
        if (state.Step == PairingStep.Done) return new PairingAnalysis(fixedScore, fixedScore, []);

        var search = new Search(state, score);
        var options = search.Options()
            .Select(o => o with { Worst = o.Worst + fixedScore, Average = o.Average + fixedScore })
            .OrderByDescending(o => o.Worst).ThenByDescending(o => o.Average)
            .ToList();
        return new PairingAnalysis(fixedScore, options[0].Worst, options);
    }

    private sealed class Search
    {
        private readonly PairingState _state;
        private readonly IReadOnlyList<PlayerId> _own;
        private readonly IReadOnlyList<PlayerId> _opp;
        private readonly double[,] _score;
        private readonly double[] _memo;

        public Search(PairingState state, Func<PlayerId, PlayerId, double> score)
        {
            _state = state;
            _own = state.RemainingOwn;
            _opp = state.RemainingOpponent;
            var n = _own.Count;
            _score = new double[n, n];
            for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
                _score[i, j] = score(_own[i], _opp[j]);
            _memo = new double[1 << (2 * n)];
            Array.Fill(_memo, double.NaN);
        }

        private int Full => (1 << _own.Count) - 1;
        private int OwnIndex(PlayerId? p) => _own.ToList().IndexOf(p!.Value);
        private int OppIndex(PlayerId? p) => _opp.ToList().IndexOf(p!.Value);

        public IEnumerable<PairingOption> Options()
        {
            int a = Full, b = Full;
            switch (_state.Step)
            {
                case PairingStep.Defenders:
                    foreach (var d in Bits(a))
                        yield return Option([_own[d]], Bits(b).Select(e => Attackers(a, b, d, e)));
                    break;

                case PairingStep.Attackers:
                {
                    int d = OwnIndex(_state.OwnDefender), e = OppIndex(_state.OpponentDefender);
                    var theirPairs = Pairs(b & ~(1 << e)).ToList();
                    foreach (var (p1, p2) in Pairs(a & ~(1 << d)))
                        yield return Option([_own[p1], _own[p2]],
                            theirPairs.Select(q => Choices(a, b, d, e, p1, p2, q.Item1, q.Item2)));
                    break;
                }

                case PairingStep.Choices:
                {
                    int d = OwnIndex(_state.OwnDefender), e = OppIndex(_state.OpponentDefender);
                    int p1 = OwnIndex(_state.OwnAttackers[0]), p2 = OwnIndex(_state.OwnAttackers[1]);
                    int q1 = OppIndex(_state.OpponentAttackers[0]), q2 = OppIndex(_state.OpponentAttackers[1]);
                    foreach (var (q, qr) in new[] { (q1, q2), (q2, q1) })
                        yield return Option([_opp[q]],
                            new[] { (p1, p2), (p2, p1) }.Select(p => Outcome(a, b, d, e, p.Item1, p.Item2, q, qr)));
                    break;
                }
            }
        }

        private static PairingOption Option(IReadOnlyList<PlayerId> players, IEnumerable<double> answers)
        {
            var values = answers.ToList();
            return new PairingOption(players, values.Min(), values.Average());
        }

        /// <summary>Value at the start of a round with remaining own players a and opponent players b.</summary>
        private double Round(int a, int b)
        {
            if (a == 0) return 0;
            var key = a | (b << _own.Count);
            if (!double.IsNaN(_memo[key])) return _memo[key];

            var best = double.NegativeInfinity;
            foreach (var d in Bits(a))
            {
                var worst = double.PositiveInfinity;
                foreach (var e in Bits(b))
                {
                    worst = Math.Min(worst, Attackers(a, b, d, e));
                    if (worst <= best) break; // cannot beat the current best defender
                }
                best = Math.Max(best, worst);
            }
            return _memo[key] = best;
        }

        private double Attackers(int a, int b, int d, int e)
        {
            var theirPairs = Pairs(b & ~(1 << e)).ToList();
            var best = double.NegativeInfinity;
            foreach (var (p1, p2) in Pairs(a & ~(1 << d)))
            {
                var worst = double.PositiveInfinity;
                foreach (var (q1, q2) in theirPairs)
                {
                    worst = Math.Min(worst, Choices(a, b, d, e, p1, p2, q1, q2));
                    if (worst <= best) break;
                }
                best = Math.Max(best, worst);
            }
            return best;
        }

        /// <summary>We pick q (their attacker vs our defender d), they pick p (our attacker vs their defender e).</summary>
        private double Choices(int a, int b, int d, int e, int p1, int p2, int q1, int q2) =>
            Math.Max(
                Math.Min(Outcome(a, b, d, e, p1, p2, q1, q2), Outcome(a, b, d, e, p2, p1, q1, q2)),
                Math.Min(Outcome(a, b, d, e, p1, p2, q2, q1), Outcome(a, b, d, e, p2, p1, q2, q1)));

        /// <param name="p">our attacker accepted by them</param><param name="pr">our refused attacker</param>
        /// <param name="q">their attacker accepted by us</param><param name="qr">their refused attacker</param>
        private double Outcome(int a, int b, int d, int e, int p, int pr, int q, int qr)
        {
            var value = _score[d, q] + _score[p, e];
            var restA = a & ~(1 << d) & ~(1 << p);
            var restB = b & ~(1 << e) & ~(1 << q);

            if (BitOperations.PopCount((uint)a) > 4) return value + Round(restA, restB);

            value += _score[pr, qr]; // final round: refused attackers play each other
            restA &= ~(1 << pr);
            restB &= ~(1 << qr);
            if (restA != 0) value += _score[BitOperations.TrailingZeroCount(restA), BitOperations.TrailingZeroCount(restB)];
            return value;
        }

        private static IEnumerable<int> Bits(int mask)
        {
            for (var m = mask; m != 0; m &= m - 1) yield return BitOperations.TrailingZeroCount(m);
        }

        private static IEnumerable<(int, int)> Pairs(int mask)
        {
            var bits = Bits(mask).ToArray();
            for (var i = 0; i < bits.Length; i++)
            for (var j = i + 1; j < bits.Length; j++)
                yield return (bits[i], bits[j]);
        }
    }
}
