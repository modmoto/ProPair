using ProPair.Application.Ports;
using ProPair.Domain.Matrices;
using ProPair.Domain.Preparations;

namespace ProPair.Application.UseCases;

/// <summary>
/// Dev helper: fills empty cells of a demo tournament matrix with plausible ratings (bell-shaped around "0").
/// Deterministic per cell, so a refill gives the same picture. Only for tournaments from the fake source.
/// </summary>
public sealed class FillDemoRatings(GetMatrix getMatrix, IMatrixRepository matrices, IClock clock)
{
    public const string DemoPrefix = "demo-";

    public async Task ExecuteAsync(string token, Domain.Common.ParticipantId opponentId, CancellationToken ct = default)
    {
        var view = await getMatrix.ExecuteAsync(token, opponentId, ct);
        if (view.Access != AccessLevel.Edit) throw new ForbiddenException("Read-only link.");
        if (!view.Tournament.ExternalId.StartsWith(DemoPrefix)) throw new ForbiddenException("Only for demo tournaments.");

        var levels = view.Preparation.RatingScale.Levels;
        var mid = (levels.Count - 1) / 2.0;
        var weights = levels.Select((_, i) => levels.Count / 2 + 1 - Math.Abs(i - mid)).ToArray();

        foreach (var own in view.Own.Roster)
        foreach (var opp in view.Opponent.Roster)
        {
            var cell = new CellKey(own.Id, opp.Id);
            if (view.Matrix.RatingAt(cell) is not null) continue;
            var rnd = new Random(own.Id.Value.GetHashCode() * 31 + opp.Id.Value.GetHashCode());
            var roll = rnd.NextDouble() * weights.Sum();
            var index = 0;
            while (roll > weights[index]) roll -= weights[index++];
            view.Matrix.SetRating(cell, levels[index].Key, "Demo", view.Preparation.RatingScale, clock.Now);
        }

        await matrices.SaveAsync(view.Matrix, ct);
    }
}
