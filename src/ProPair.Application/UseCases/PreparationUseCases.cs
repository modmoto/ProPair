using ProPair.Application.Ports;
using ProPair.Domain.Common;
using ProPair.Domain.Matrices;
using ProPair.Domain.Preparations;
using ProPair.Domain.Teams;
using ProPair.Domain.Tournaments;

namespace ProPair.Application.UseCases;

/// <summary>Captain picks the own participant of an imported tournament and starts preparing.</summary>
public sealed class StartPreparation(
    ITournamentRepository tournaments, ITeamRepository teams, IPreparationRepository preparations)
{
    public async Task<Preparation> ExecuteAsync(TournamentId tournamentId, ParticipantId ownParticipantId,
        CancellationToken ct = default)
    {
        var tournament = await tournaments.GetAsync(tournamentId, ct) ?? throw new NotFoundException("Tournament");
        var participant = tournament.GetParticipant(ownParticipantId);

        // MVP: the team is identified by the participant name; later teams get their own management.
        var team = await teams.FindByNameAsync(participant.Name, ct);
        if (team is null)
        {
            team = Team.Create(participant.Name);
            await teams.SaveAsync(team, ct);
        }

        var existing = await preparations.FindAsync(team.Id, tournament.Id, ct);
        if (existing is not null) return existing;

        var preparation = Preparation.Start(team, tournament, ownParticipantId);
        await preparations.SaveAsync(preparation, ct);
        return preparation;
    }
}

public sealed record PreparationView(
    Preparation Preparation, AccessLevel Access, Tournament Tournament, Participant Own,
    IReadOnlyList<OpponentView> Opponents);

public sealed record OpponentView(Participant Participant, int RatedCells);

public sealed class GetPreparation(
    IPreparationRepository preparations, ITournamentRepository tournaments, IMatrixRepository matrices)
{
    public async Task<PreparationView> ExecuteAsync(string token, CancellationToken ct = default)
    {
        var (preparation, access) = await Access.ResolveAsync(preparations, token, ct);
        var tournament = await tournaments.GetAsync(preparation.TournamentId, ct)
                         ?? throw new NotFoundException("Tournament");
        var rated = (await matrices.ListAsync(preparation.Id, ct))
            .ToDictionary(m => m.OpponentParticipantId, m => m.Cells.Count);

        var opponents = tournament.Participants
            .Where(p => p.Id != preparation.OwnParticipantId)
            .Select(p => new OpponentView(p, rated.GetValueOrDefault(p.Id)))
            .ToList();

        return new PreparationView(preparation, access, tournament,
            tournament.GetParticipant(preparation.OwnParticipantId), opponents);
    }
}

public sealed record MatrixView(
    Preparation Preparation, AccessLevel Access, Tournament Tournament, Participant Own, Participant Opponent,
    Matrix Matrix);

/// <summary>Loads the matrix against one opponent; creates it on first access.</summary>
public sealed class GetMatrix(
    IPreparationRepository preparations, ITournamentRepository tournaments, IMatrixRepository matrices)
{
    public async Task<MatrixView> ExecuteAsync(string token, ParticipantId opponentId, CancellationToken ct = default)
    {
        var (preparation, access) = await Access.ResolveAsync(preparations, token, ct);
        var tournament = await tournaments.GetAsync(preparation.TournamentId, ct)
                         ?? throw new NotFoundException("Tournament");
        var own = tournament.GetParticipant(preparation.OwnParticipantId);
        var opponent = tournament.GetParticipant(opponentId);
        if (opponent.Id == own.Id) throw new DomainException("Opponent must not be the own team.");

        var ownIds = own.Roster.Select(p => p.Id).ToList();
        var oppIds = opponent.Roster.Select(p => p.Id).ToList();

        var matrix = await matrices.FindAsync(preparation.Id, opponentId, ct);
        if (matrix is null)
        {
            matrix = Matrix.Create(preparation.Id, opponentId, ownIds, oppIds);
            if (access == AccessLevel.Edit) await matrices.SaveAsync(matrix, ct);
        }
        else if (matrix.SyncPlayers(ownIds, oppIds) && access == AccessLevel.Edit)
        {
            await matrices.SaveAsync(matrix, ct);
        }

        return new MatrixView(preparation, access, tournament, own, opponent, matrix);
    }
}

public sealed class SetRating(GetMatrix getMatrix, IMatrixRepository matrices, IClock clock)
{
    public Task<Matrix> ExecuteAsync(string token, ParticipantId opponentId, CellKey cell, string? levelKey,
        string author, CancellationToken ct = default) =>
        MatrixUpdate.RunAsync(getMatrix, matrices, token, opponentId,
            view => view.Matrix.SetRating(cell, levelKey, author, view.Preparation.RatingScale, clock.Now), ct);
}

/// <summary>Load → change → save with optimistic concurrency; on conflict reload and apply again (last write wins).</summary>
internal static class MatrixUpdate
{
    private const int MaxAttempts = 5;

    public static async Task<Matrix> RunAsync(GetMatrix getMatrix, IMatrixRepository matrices, string token,
        ParticipantId opponentId, Action<MatrixView> change, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            var view = await getMatrix.ExecuteAsync(token, opponentId, ct);
            if (view.Access != AccessLevel.Edit) throw new ForbiddenException("Read-only link.");
            change(view);
            try
            {
                await matrices.SaveAsync(view.Matrix, ct);
                return view.Matrix;
            }
            catch (ConcurrencyException) when (attempt < MaxAttempts)
            {
                await Task.Delay(Random.Shared.Next(10, 50) * attempt, ct);
            }
        }
    }
}

internal static class Access
{
    public static async Task<(Preparation, AccessLevel)> ResolveAsync(
        IPreparationRepository preparations, string token, CancellationToken ct)
    {
        var preparation = await preparations.FindByTokenAsync(token, ct) ?? throw new NotFoundException("Preparation");
        return (preparation, preparation.AccessFor(token));
    }
}
