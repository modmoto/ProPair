using ProPair.Domain.Common;

namespace ProPair.Domain.Pairing.Advice;

/// <summary>
/// Recommends moves for the current pairing step. The app only knows this interface, so the whole
/// algorithm can be replaced (e.g. by a heuristic or an external engine) without touching anything else.
/// </summary>
public interface IPairingAdvisor
{
    /// <param name="score">Our rating of a game (own player vs opponent player); the opponent is assumed to see the negation.</param>
    /// <param name="topPicks">How many of our best options get a predicted path.</param>
    PairingAdvice Advise(PairingState state, Func<PlayerId, PlayerId, double> score, int topPicks = 3);
}

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

/// <summary>The most likely continuation if we play <see cref="Option"/> now.</summary>
public sealed record PredictedPath(MoveOption Option, IReadOnlyList<PredictedStep> Steps, IReadOnlyList<PredictedGame> Games, double Total);

/// <param name="Fixed">Score of games already paired.</param>
/// <param name="Expected">Value of the pairing from here (final total, our view) if both sides play perfectly.</param>
/// <param name="Own">Our options for the current step, best first.</param>
/// <param name="Opponent">The opponent's options for the current step, most likely first.</param>
/// <param name="TopPicks">Predicted paths for our best options.</param>
public sealed record PairingAdvice(double Fixed, double Expected, IReadOnlyList<MoveOption> Own, IReadOnlyList<MoveOption> Opponent,
    IReadOnlyList<PredictedPath> TopPicks);
