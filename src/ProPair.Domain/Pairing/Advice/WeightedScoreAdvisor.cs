using ProPair.Domain.Common;

namespace ProPair.Domain.Pairing.Advice;

/// <summary>
/// Decorator: lets the inner advisor decide on transformed ratings (e.g. minus counts 1.5×),
/// but reports games, path totals and the fixed score in real rating points.
/// Expected/Worst of options stay in the transformed scale – they are what the decision was based on.
/// </summary>
public sealed class WeightedScoreAdvisor(IPairingAdvisor inner, Func<double, double> transform) : IPairingAdvisor
{
    public PairingAdvice Advise(PairingState state, Func<PlayerId, PlayerId, double> score, int topPicks = 3)
    {
        var advice = inner.Advise(state, (own, opp) => transform(score(own, opp)), topPicks);
        return advice with
        {
            Fixed = state.Games.Sum(g => score(g.Own, g.Opponent)),
            TopPicks = advice.TopPicks.Select(p =>
            {
                var games = p.Games.Select(g => g with { Score = score(g.Own, g.Opponent) }).ToList();
                return p with { Games = games, Total = state.Games.Sum(g => score(g.Own, g.Opponent)) + games.Sum(g => g.Score) };
            }).ToList(),
        };
    }
}
