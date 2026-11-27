using ProPair.Application.Ports;
using ProPair.Domain.Common;
using ProPair.Domain.Matrices;
using ProPair.Domain.Preparations;

namespace ProPair.Application.UseCases;

public sealed class SetCellNote(GetMatrix getMatrix, IMatrixRepository matrices, IClock clock)
{
    public Task<Matrix> ExecuteAsync(string token, ParticipantId opponentId, CellKey cell, string? text, string author,
        CancellationToken ct = default) =>
        MatrixUpdate.RunAsync(getMatrix, matrices, token, opponentId,
            view => view.Matrix.SetNote(cell, text, author, clock.Now), ct);
}

/// <summary>Rating history of one cell, newest first. Kept out of the matrix payload; loaded on demand.</summary>
public sealed class GetCellHistory(GetMatrix getMatrix)
{
    public async Task<IReadOnlyList<RatingChanged>> ExecuteAsync(string token, ParticipantId opponentId, CellKey cell,
        CancellationToken ct = default)
    {
        var view = await getMatrix.ExecuteAsync(token, opponentId, ct);
        return view.Matrix.HistoryOf(cell).OrderByDescending(h => h.OccurredAt).ToList();
    }
}

public sealed class SetPlayerNote(IPreparationRepository preparations, ITournamentRepository tournaments, IClock clock)
{
    private const int MaxAttempts = 5;

    public async Task<Preparation> ExecuteAsync(string token, PlayerId playerId, string? text, string author,
        CancellationToken ct = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            var preparation = await preparations.FindByTokenAsync(token, ct) ?? throw new NotFoundException("Preparation");
            if (preparation.AccessFor(token) != AccessLevel.Edit) throw new ForbiddenException("Read-only link.");

            var tournament = await tournaments.GetAsync(preparation.TournamentId, ct) ?? throw new NotFoundException("Tournament");
            if (!tournament.Participants.Any(p => p.Roster.Any(r => r.Id == playerId)))
                throw new DomainException("Player is not part of this tournament.");

            preparation.SetPlayerNote(playerId, text, author, clock.Now);
            try
            {
                await preparations.SaveAsync(preparation, ct);
                return preparation;
            }
            catch (ConcurrencyException) when (attempt < MaxAttempts)
            {
                await Task.Delay(Random.Shared.Next(10, 50) * attempt, ct);
            }
        }
    }
}

/// <summary>Colours (or un-marks with color null) one line of a player's list, with an optional short note.</summary>
public sealed class SetListMark(IPreparationRepository preparations, ITournamentRepository tournaments, IClock clock)
{
    private const int MaxAttempts = 5;

    public async Task<Preparation> ExecuteAsync(string token, PlayerId playerId, int lineIndex, string lineText,
        MarkColor? color, string? note, string author, CancellationToken ct = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            var preparation = await preparations.FindByTokenAsync(token, ct) ?? throw new NotFoundException("Preparation");
            if (preparation.AccessFor(token) != AccessLevel.Edit) throw new ForbiddenException("Read-only link.");

            var tournament = await tournaments.GetAsync(preparation.TournamentId, ct) ?? throw new NotFoundException("Tournament");
            var player = tournament.Participants.SelectMany(p => p.Roster).FirstOrDefault(r => r.Id == playerId)
                         ?? throw new DomainException("Player is not part of this tournament.");

            preparation.MarkListLine(playerId, player.ListText, lineIndex, lineText, color, note, author, clock.Now);
            try
            {
                await preparations.SaveAsync(preparation, ct);
                return preparation;
            }
            catch (ConcurrencyException) when (attempt < MaxAttempts)
            {
                await Task.Delay(Random.Shared.Next(10, 50) * attempt, ct);
            }
        }
    }
}
