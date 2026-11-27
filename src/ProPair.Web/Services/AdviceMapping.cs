using ProPair.Contracts;
using ProPair.Domain.Pairing.Advice;

namespace ProPair.Web.Services;

/// <summary>Maps the domain advice (computed on the device) onto the DTOs the page renders.</summary>
public static class AdviceMapping
{
    public static double Score(PairingDto p, Guid own, Guid opp)
    {
        var cell = p.Cells.FirstOrDefault(c => c.OwnPlayerId == own && c.OpponentPlayerId == opp);
        return cell is null ? 0 : (double)(p.Scale.FirstOrDefault(l => l.Key == cell.LevelKey)?.Score ?? 0);
    }

    public static PairingAdviceDto ToDto(PairingAdvice a, string mode, IReadOnlyList<RatingLevelDto> scale)
    {
        static List<Guid> Ids(IEnumerable<ProPair.Domain.Common.PlayerId> ids) => ids.Select(i => i.Value).ToList();
        static MoveOptionDto Option(MoveOption o) => new(Ids(o.Players), o.Expected, o.Worst, o.Probability);

        return new PairingAdviceDto(
            mode,
            scale.Where(l => l.Score < 0).OrderBy(l => l.Score)
                .Select(l => new MinusFactorDto(l.Key, AdviceModes.MinusFactor((double)l.Score))).ToList(),
            a.Expected,
            a.Own.Select(Option).ToList(),
            a.Opponent.Select(Option).ToList(),
            a.TopPicks.Select(p => new PredictedPathDto(Option(p.Option),
                p.Steps.Select(s => new PredictedStepDto(s.Round, s.Step.ToString().ToLowerInvariant(), Ids(s.Own), Ids(s.Opponent),
                    s.OwnProbability, s.OpponentProbability)).ToList(),
                p.Games.Select(g => new PredictedGameDto(g.Round, g.Own.Value, g.Opponent.Value, g.Score)).ToList(),
                p.Total,
                new OutcomeDto(p.ThisRound.Value, p.ThisRound.Probability, p.ThisRound.Mean),
                new OutcomeDto(p.Final.Value, p.Final.Probability, p.Final.Mean))).ToList());
    }
}
