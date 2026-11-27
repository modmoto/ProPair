using System.Numerics;
using ProPair.Domain.Common;

namespace ProPair.Domain.Pairing.Advice;

/// <summary>
/// Searches the complete pairing tree.
/// <list type="bullet">
/// <item>The end of every branch is a full set of games, scored from the matrix.</item>
/// <item>Every hidden, simultaneous step (defenders, attackers, choices) is a small zero-sum matrix game
///   whose entries are the values of the states it leads to. The <see cref="IMatrixGameSolver"/> decides how
///   such a step is valued (Nash equilibrium by default, or pessimistic maximin).</item>
/// <item>Values are memoised per (remaining own players, remaining opponent players) at round start –
///   8v8 has at most 2⁸·2⁸ such states.</item>
/// </list>
/// The opponent is assumed to rate every game as the negation of our rating.
/// <para>
/// <paramref name="weight"/> transforms ratings for the <b>decision</b> only (e.g. minus counts 1.5×).
/// Everything reported – games, path totals, outcome distributions – is in real rating points.
/// </para>
/// </summary>
public sealed class GameTreeAdvisor(IMatrixGameSolver solver, Func<double, double>? weight = null) : IPairingAdvisor
{
    public PairingAdvice Advise(PairingState state, Func<PlayerId, PlayerId, double> score, int topPicks = 3) =>
        Start(state.RemainingOwn, state.RemainingOpponent, score).Advise(state, topPicks);

    public IPairingAdviceSession Start(IReadOnlyList<PlayerId> own, IReadOnlyList<PlayerId> opponent,
        Func<PlayerId, PlayerId, double> score, RoundValues? precomputed = null)
    {
        Func<PlayerId, PlayerId, double> decision = weight is null ? score : (o, p) => weight(score(o, p));
        return new Session(new Tree(own, opponent, decision, solver, precomputed), solver, score);
    }

    private sealed class Session(Tree tree, IMatrixGameSolver solver, Func<PlayerId, PlayerId, double> score)
        : IPairingAdviceSession
    {
        public RoundValues ExportRoundValues() => tree.ExportRoundValues();

        public PairingAdvice Advise(PairingState state, int topPicks = 3) => GameTreeAdvisor.Advise(tree, solver, state, score, topPicks);
    }

    private static PairingAdvice Advise(Tree tree, IMatrixGameSolver solver, PairingState state,
        Func<PlayerId, PlayerId, double> score, int topPicks)
    {
        var fixedScore = state.Games.Sum(g => score(g.Own, g.Opponent));
        if (state.Step == PairingStep.Done) return new PairingAdvice(fixedScore, fixedScore, [], [], []);

        var step = tree.StepGame(state);
        var solution = solver.Solve(step.Payoff, step.Own.Count, step.Opponent.Count);

        var own = step.Own.Select((players, r) => new MoveOption(players,
                Expected: solution.RowValues[r],
                Worst: Enumerable.Range(0, step.Opponent.Count).Min(c => step.Payoff[r, c]),
                Probability: solution.Rows[r]))
            .ToList();
        var opponent = step.Opponent.Select((players, c) => new MoveOption(players,
                Expected: solution.ColValues[c],
                Worst: Enumerable.Range(0, step.Own.Count).Max(r => step.Payoff[r, c]), // their worst = our best
                Probability: solution.Cols[c]))
            .ToList();

        // Rank by what an option is worth against a perfect opponent, then by how safe it is.
        // Probability only breaks ties: equilibria are often not unique, so a 0 % option can be just as good.
        var rankedOwn = Rank(own);

        var picks = rankedOwn.Take(topPicks).Select(r =>
        {
            var (steps, final) = tree.MostLikelyPath(state, forcedOwnRow: r);
            var games = final.Games.Skip(state.Games.Count)
                .Select((g, i) => new PredictedGame(RoundOf(state, final, i), g.Own, g.Opponent, score(g.Own, g.Opponent)))
                .ToList();
            // what this pick brings: this round and the final total, both as most likely value + probability (real points)
            var thisRound = tree.Outcome(state, r, score, untilRoundEnds: true, fixedScore: 0);
            var total = tree.Outcome(state, r, score, untilRoundEnds: false, fixedScore: fixedScore);
            return new PredictedPath(own[r], steps, games, final.Games.Sum(g => score(g.Own, g.Opponent)), thisRound, total);
        }).ToList();

        return new PairingAdvice(fixedScore, solution.Value,
            rankedOwn.Select(r => own[r]).ToList(),
            opponent.OrderByDescending(o => o.Probability).ThenBy(o => o.Expected).ToList(),
            picks);
    }

    private static List<int> Rank(IReadOnlyList<MoveOption> options) =>
        Enumerable.Range(0, options.Count)
            // one decimal = what the UI shows; smaller differences in expected value are noise, safety matters more
            .OrderByDescending(r => Math.Round(options[r].Expected, 1))
            .ThenByDescending(r => Math.Round(options[r].Worst, 1))
            .ThenByDescending(r => options[r].Expected)
            .ThenByDescending(r => options[r].Probability)
            .ToList();

    /// <summary>Games are added two per round, the final round adds the rest.</summary>
    private static int RoundOf(PairingState start, PairingState final, int newGameIndex)
    {
        var round = start.Round + newGameIndex / 2;
        return Math.Min(round, final.Round);
    }

    /// <summary>One simultaneous step: our moves × their moves → value of the resulting state.</summary>
    internal sealed record StepGame(double[,] Payoff, IReadOnlyList<IReadOnlyList<PlayerId>> Own,
        IReadOnlyList<IReadOnlyList<PlayerId>> Opponent, Func<int, int, PairingMove> Move);

    private sealed class Tree
    {
        private readonly IMatrixGameSolver _solver;
        private readonly IReadOnlyList<PlayerId> _ownIds;   // full roster of the session, index = bit
        private readonly IReadOnlyList<PlayerId> _oppIds;
        private readonly int _n;
        private readonly double[,] _score;
        private readonly double[] _memo;

        // scratch buffers per remaining player count (recursion only goes to smaller counts, so no clashes)
        private readonly double[][,] _defBuf = new double[9][,];
        private readonly double[][,] _attBuf = new double[9][,];
        private readonly double[][,] _choiceBuf = new double[9][,];

        private static readonly int[][] BitsOf = BuildBits();
        private static readonly (int, int)[][] PairsOf = BuildPairs();

        public Tree(IReadOnlyList<PlayerId> own, IReadOnlyList<PlayerId> opponent, Func<PlayerId, PlayerId, double> score,
            IMatrixGameSolver solver, RoundValues? precomputed)
        {
            if (own.Count != opponent.Count || own.Count > 8) throw new DomainException("Advice supports equal rosters of up to 8.");
            _solver = solver;
            _ownIds = own;
            _oppIds = opponent;
            _n = _ownIds.Count;
            _score = new double[_n, _n];
            for (var i = 0; i < _n; i++)
            for (var j = 0; j < _n; j++)
                _score[i, j] = score(_ownIds[i], _oppIds[j]);
            _scoreOf = score;
            _memo = new double[1 << (2 * _n)];
            Array.Fill(_memo, double.NaN);
            if (precomputed is not null)
                foreach (var (key, value) in precomputed.Values)
                    if (key >= 0 && key < _memo.Length) _memo[key] = value;
            for (var k = 1; k <= _n; k++)
            {
                _defBuf[k] = new double[k, k];
                var pairs = (k - 1) * (k - 2) / 2;
                _attBuf[k] = new double[Math.Max(pairs, 1), Math.Max(pairs, 1)];
                _choiceBuf[k] = new double[2, 2];
            }
        }

        private readonly Func<PlayerId, PlayerId, double> _scoreOf;

        /// <summary>Solves the whole tree from the full rosters and returns every round-start value.</summary>
        public RoundValues ExportRoundValues()
        {
            var full = (1 << _n) - 1;
            Round(full, full);
            var values = new Dictionary<int, double>();
            for (var key = 0; key < _memo.Length; key++)
                if (!double.IsNaN(_memo[key])) values[key] = _memo[key];
            return new RoundValues(values);
        }

        // ---------- public entry points (domain objects, used for the current step and predicted paths) ----------

        public StepGame StepGame(PairingState s)
        {
            switch (s.Step)
            {
                case PairingStep.Defenders:
                {
                    var own = s.RemainingOwn.Select(p => (IReadOnlyList<PlayerId>)[p]).ToList();
                    var opp = s.RemainingOpponent.Select(p => (IReadOnlyList<PlayerId>)[p]).ToList();
                    return Build(s, own, opp, (r, c) => new DefendersMove(own[r][0], opp[c][0]));
                }
                case PairingStep.Attackers:
                {
                    var own = Pairs(s.RemainingOwn.Where(p => p != s.OwnDefender));
                    var opp = Pairs(s.RemainingOpponent.Where(p => p != s.OpponentDefender));
                    return Build(s, own, opp, (r, c) => new AttackersMove(own[r][0], own[r][1], opp[c][0], opp[c][1]));
                }
                default: // choices: we pick one of their attackers, they pick one of ours
                {
                    var own = s.OpponentAttackers.Select(p => (IReadOnlyList<PlayerId>)[p]).ToList();
                    var opp = s.OwnAttackers.Select(p => (IReadOnlyList<PlayerId>)[p]).ToList();
                    return Build(s, own, opp, (r, c) => new ChoicesMove(own[r][0], opp[c][0]));
                }
            }
        }

        private readonly Dictionary<string, (StepGame Game, MatrixGameSolution Solution)> _solved = [];

        private (StepGame Game, MatrixGameSolution Solution) Solved(PairingState s)
        {
            var key = Signature(s);
            if (_solved.TryGetValue(key, out var hit)) return hit;
            var game = StepGame(s);
            return _solved[key] = (game, _solver.Solve(game.Payoff, game.Own.Count, game.Opponent.Count));
        }

        private string Signature(PairingState s) =>
            $"{Mask(s.RemainingOwn, _ownIds)}|{Mask(s.RemainingOpponent, _oppIds)}|{s.Step}|" +
            $"{(s.OwnDefender is { } d ? Own(d) : -1)}|{(s.OpponentDefender is { } e ? Opp(e) : -1)}|" +
            string.Join(",", s.OwnAttackers.Select(p => Own(p))) + "|" + string.Join(",", s.OpponentAttackers.Select(p => Opp(p)));

        /// <summary>
        /// Exact distribution of the result if we play <paramref name="ownRow"/> now and afterwards both sides follow
        /// their strategies (mixed: every option with its probability). Returns the most likely value, its probability
        /// and the mean – in real points (<paramref name="real"/>), whatever the decision weighting was.
        /// </summary>
        public PredictedOutcome Outcome(PairingState start, int ownRow, Func<PlayerId, PlayerId, double> real,
            bool untilRoundEnds, double fixedScore)
        {
            var memo = new Dictionary<string, Dictionary<double, double>>();
            bool Stop(PairingState s) => s.Step == PairingStep.Done || (untilRoundEnds && s.Round > start.Round);

            Dictionary<double, double> Dist(PairingState s, int? forced)
            {
                if (Stop(s)) return new() { [0] = 1 };
                var key = Signature(s);
                if (forced is null && memo.TryGetValue(key, out var cached)) return cached;

                var (game, solution) = Solved(s);
                var result = new Dictionary<double, double>();
                var rows = forced is { } f ? [(f, 1.0)] : Support(solution.Rows);
                foreach (var (r, pr) in rows)
                foreach (var (c, pc) in Support(solution.Cols))
                {
                    var child = s.Apply(game.Move(r, c));
                    var gained = child.Games.Skip(s.Games.Count).Sum(g => real(g.Own, g.Opponent));
                    foreach (var (value, p) in Dist(child, null))
                    {
                        var v = Math.Round(value + gained, 2);
                        result[v] = result.GetValueOrDefault(v) + pr * pc * p;
                    }
                }
                if (forced is null) memo[key] = result;
                return result;
            }

            var dist = Dist(start, ownRow);
            var mean = dist.Sum(kv => kv.Key * kv.Value) + fixedScore;
            var mode = dist.OrderByDescending(kv => Math.Round(kv.Value, 6))
                .ThenBy(kv => Math.Abs(kv.Key + fixedScore - mean)).First();
            return new PredictedOutcome(mode.Key + fixedScore, mode.Value, mean);
        }

        private static IEnumerable<(int, double)> Support(double[] mix) =>
            mix.Select((p, i) => (i, p)).Where(x => x.p > 1e-6);

        /// <summary>Follows the most likely moves of both sides to the end. Optionally our first move is fixed.</summary>
        public (IReadOnlyList<PredictedStep> Steps, PairingState Final) MostLikelyPath(PairingState s, int? forcedOwnRow = null)
        {
            var steps = new List<PredictedStep>();
            while (s.Step != PairingStep.Done)
            {
                var (game, solution) = Solved(s);
                // our move: same ranking as the recommendations (value, then safety, then probability)
                var r = forcedOwnRow ?? Rank(Enumerable.Range(0, game.Own.Count).Select(i => new MoveOption(game.Own[i],
                    solution.RowValues[i],
                    Enumerable.Range(0, game.Opponent.Count).Min(c => game.Payoff[i, c]),
                    solution.Rows[i])).ToList())[0];
                forcedOwnRow = null;
                // their most likely move; ties → the one that hurts us most against our chosen move
                var c = ArgMax(solution.Cols, j => -game.Payoff[r, j]);
                steps.Add(new PredictedStep(s.Round, s.Step, game.Own[r], game.Opponent[c], solution.Rows[r], solution.Cols[c]));
                s = s.Apply(game.Move(r, c));
            }
            return (steps, s);
        }

        private static int ArgMax(double[] probabilities, Func<int, double> tieBreak) =>
            Enumerable.Range(0, probabilities.Length)
                .OrderByDescending(i => Math.Round(probabilities[i], 6))
                .ThenByDescending(tieBreak)
                .First();

        private StepGame Build(PairingState s, List<IReadOnlyList<PlayerId>> own, List<IReadOnlyList<PlayerId>> opp,
            Func<int, int, PairingMove> move)
        {
            var payoff = new double[own.Count, opp.Count];
            for (var r = 0; r < own.Count; r++)
            for (var c = 0; c < opp.Count; c++)
                payoff[r, c] = ValueOf(s.Apply(move(r, c)));
            return new StepGame(payoff, own, opp, move);
        }

        private static List<IReadOnlyList<PlayerId>> Pairs(IEnumerable<PlayerId> players)
        {
            var list = players.ToList();
            var pairs = new List<IReadOnlyList<PlayerId>>();
            for (var i = 0; i < list.Count; i++)
            for (var j = i + 1; j < list.Count; j++)
                pairs.Add([list[i], list[j]]);
            return pairs;
        }

        /// <summary>Final total (all games incl. fixed ones) of a state, both sides playing perfectly.</summary>
        private double ValueOf(PairingState s)
        {
            var games = s.Games.Sum(g => _scoreOf(g.Own, g.Opponent));
            if (s.Step == PairingStep.Done) return games;
            int a = Mask(s.RemainingOwn, _ownIds), b = Mask(s.RemainingOpponent, _oppIds);
            return games + s.Step switch
            {
                PairingStep.Defenders => Round(a, b),
                PairingStep.Attackers => Attack(a, b, Own(s.OwnDefender), Opp(s.OpponentDefender)),
                _ => Choice(a, b, Own(s.OwnDefender), Opp(s.OpponentDefender),
                    Own(s.OwnAttackers[0]), Own(s.OwnAttackers[1]), Opp(s.OpponentAttackers[0]), Opp(s.OpponentAttackers[1])),
            };
        }

        private int Own(PlayerId? p) => IndexOf(_ownIds, p!.Value);
        private int Opp(PlayerId? p) => IndexOf(_oppIds, p!.Value);

        private static int IndexOf(IReadOnlyList<PlayerId> ids, PlayerId p)
        {
            for (var i = 0; i < ids.Count; i++) if (ids[i] == p) return i;
            throw new InvalidOperationException("Player is not part of this pairing.");
        }

        private static int Mask(IReadOnlyList<PlayerId> remaining, IReadOnlyList<PlayerId> ids)
        {
            var mask = 0;
            foreach (var p in remaining) mask |= 1 << IndexOf(ids, p);
            return mask;
        }

        // ---------- hot path: bitmasks, no allocations ----------

        /// <summary>Value of the remaining games at the start of a round.</summary>
        private double Round(int a, int b)
        {
            if (a == 0) return 0;
            var key = a | (b << _n);
            if (!double.IsNaN(_memo[key])) return _memo[key];

            var k = BitOperations.PopCount((uint)a);
            var buf = _defBuf[k];
            var ownBits = BitsOf[a];
            var oppBits = BitsOf[b];
            for (var r = 0; r < k; r++)
            for (var c = 0; c < k; c++)
                buf[r, c] = Attack(a, b, ownBits[r], oppBits[c]);
            return _memo[key] = _solver.Value(buf, k, k);
        }

        /// <summary>Defenders d (ours) and e (theirs) are known; both teams pick two attackers.</summary>
        private double Attack(int a, int b, int d, int e)
        {
            var k = BitOperations.PopCount((uint)a);
            var ownPairs = PairsOf[a & ~(1 << d)];
            var oppPairs = PairsOf[b & ~(1 << e)];
            var buf = _attBuf[k];
            for (var r = 0; r < ownPairs.Length; r++)
            for (var c = 0; c < oppPairs.Length; c++)
                buf[r, c] = Choice(a, b, d, e, ownPairs[r].Item1, ownPairs[r].Item2, oppPairs[c].Item1, oppPairs[c].Item2);
            return _solver.Value(buf, ownPairs.Length, oppPairs.Length);
        }

        /// <summary>Rows: which of their attackers (q1, q2) we take. Columns: which of ours (p1, p2) they take.</summary>
        private double Choice(int a, int b, int d, int e, int p1, int p2, int q1, int q2)
        {
            var k = BitOperations.PopCount((uint)a);
            var buf = _choiceBuf[k];
            var v00 = Outcome(a, b, d, e, p1, p2, q1, q2);
            var v01 = Outcome(a, b, d, e, p2, p1, q1, q2);
            var v10 = Outcome(a, b, d, e, p1, p2, q2, q1);
            var v11 = Outcome(a, b, d, e, p2, p1, q2, q1);
            buf[0, 0] = v00; buf[0, 1] = v01; buf[1, 0] = v10; buf[1, 1] = v11; // filled after recursion
            return _solver.Value(buf, 2, 2);
        }

        /// <param name="p">our attacker they accepted</param><param name="pr">our refused attacker</param>
        /// <param name="q">their attacker we accepted</param><param name="qr">their refused attacker</param>
        private double Outcome(int a, int b, int d, int e, int p, int pr, int q, int qr)
        {
            var value = _score[d, q] + _score[p, e];
            var restA = a & ~(1 << d) & ~(1 << p);
            var restB = b & ~(1 << e) & ~(1 << q);

            if (BitOperations.PopCount((uint)a) > 4) return value + Round(restA, restB); // refused attackers return

            value += _score[pr, qr]; // final round: refused attackers play each other
            restA &= ~(1 << pr);
            restB &= ~(1 << qr);
            if (restA != 0) value += _score[BitOperations.TrailingZeroCount(restA), BitOperations.TrailingZeroCount(restB)];
            return value;
        }

        private static int[][] BuildBits()
        {
            var bits = new int[256][];
            for (var m = 0; m < 256; m++)
                bits[m] = Enumerable.Range(0, 8).Where(i => (m & (1 << i)) != 0).ToArray();
            return bits;
        }

        private static (int, int)[][] BuildPairs()
        {
            var pairs = new (int, int)[256][];
            for (var m = 0; m < 256; m++)
            {
                var b = Enumerable.Range(0, 8).Where(i => (m & (1 << i)) != 0).ToArray();
                var list = new List<(int, int)>();
                for (var i = 0; i < b.Length; i++)
                for (var j = i + 1; j < b.Length; j++)
                    list.Add((b[i], b[j]));
                pairs[m] = list.ToArray();
            }
            return pairs;
        }
    }
}
