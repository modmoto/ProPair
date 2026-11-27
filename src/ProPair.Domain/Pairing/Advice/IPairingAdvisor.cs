using ProPair.Domain.Common;

namespace ProPair.Domain.Pairing.Advice;

/// <summary>
/// Recommends moves for the current pairing step. The app only knows this interface, so the whole
/// algorithm can be replaced (e.g. by a heuristic or an external engine) without touching anything else.
/// </summary>
public interface IPairingAdvisor
{
    /// <summary>One-off advice for a state (builds a fresh session over the remaining players).</summary>
    /// <param name="score">Our rating of a game (own player vs opponent player); the opponent is assumed to see the negation.</param>
    /// <param name="topPicks">How many of our best options get a predicted path.</param>
    PairingAdvice Advise(PairingState state, Func<PlayerId, PlayerId, double> score, int topPicks = 3);

    /// <summary>
    /// Advice session over the full rosters of one pairing: caches work between steps and can import/export the
    /// expensive part (<see cref="RoundValues"/>) – so a server can precompute it and a client can advise offline.
    /// </summary>
    IPairingAdviceSession Start(IReadOnlyList<PlayerId> own, IReadOnlyList<PlayerId> opponent,
        Func<PlayerId, PlayerId, double> score, RoundValues? precomputed = null);
}

public interface IPairingAdviceSession
{
    PairingAdvice Advise(PairingState state, int topPicks = 3);

    /// <summary>Value of every reachable round start, keyed by (own mask | opponent mask &lt;&lt; roster size).</summary>
    RoundValues ExportRoundValues();
}

/// <summary>Precomputed values of round starts. Only valid for the same rosters (order), ratings and mode.</summary>
public sealed record RoundValues(IReadOnlyDictionary<int, double> Values);

/// <summary>One option in the current step, for us or for the opponent.</summary>
/// <param name="Players">Defender (1), attacker pair (2) or accepted attacker (1).</param>
/// <param name="Expected">Final total (our view) if this option is played and the other side plays its equilibrium mix.</param>
/// <param name="Worst">Final total if the other side answers this option as well as it can (only meaningful for our options).</param>
/// <param name="Probability">How often a perfect player picks this option (0–1).</param>
public sealed record MoveOption(IReadOnlyList<PlayerId> Players, double Expected, double Worst, double Probability);

/// <summary>One predicted step of the pairing: what both sides most likely do.</summary>
public sealed record PredictedStep(int Round, PairingStep Step, IReadOnlyList<PlayerId> Own, IReadOnlyList<PlayerId> Opponent,
    double OwnProbability, double OpponentProbability);

/// <summary>A game that is not fixed yet but expected on the predicted path.</summary>
public sealed record PredictedGame(int Round, PlayerId Own, PlayerId Opponent, double Score);

/// <summary>A result in real rating points: most likely value, how likely exactly that is, and the average.</summary>
public sealed record PredictedOutcome(double Value, double Probability, double Mean);

/// <summary>The most likely continuation if we play <see cref="Option"/> now.</summary>
/// <param name="Total">Final total of the most likely path.</param>
/// <param name="ThisRound">What the current round brings (sum of its games).</param>
/// <param name="Final">Final total of the whole pairing.</param>
public sealed record PredictedPath(MoveOption Option, IReadOnlyList<PredictedStep> Steps, IReadOnlyList<PredictedGame> Games,
    double Total, PredictedOutcome ThisRound, PredictedOutcome Final);

/// <param name="Fixed">Score of games already paired.</param>
/// <param name="Expected">Value of the pairing from here (final total, our view) if both sides play perfectly.</param>
/// <param name="Own">Our options for the current step, best first.</param>
/// <param name="Opponent">The opponent's options for the current step, most likely first.</param>
/// <param name="TopPicks">Predicted paths for our best options.</param>
public sealed record PairingAdvice(double Fixed, double Expected, IReadOnlyList<MoveOption> Own, IReadOnlyList<MoveOption> Opponent,
    IReadOnlyList<PredictedPath> TopPicks);
