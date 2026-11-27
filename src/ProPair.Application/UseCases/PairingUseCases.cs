using ProPair.Application.Ports;
using ProPair.Domain.Common;
using ProPair.Domain.Matrices;
using ProPair.Domain.Pairing;
using ProPair.Domain.Pairing.Advice;
using ProPair.Domain.Preparations;

namespace ProPair.Application.UseCases;

/// <param name="Advice">Recommendations; only computed when asked for (they are optional in the UI and cost up to a second).</param>
public sealed record PairingView(MatrixView Matrix, PairingSession Session, double FixedScore, PairingAdvice? Advice, int UnratedCells);

/// <summary>Loads (or starts) the live pairing against one opponent; optionally asks the advisor for recommendations.</summary>
public sealed class GetPairing(GetMatrix getMatrix, IPairingSessionRepository sessions, IPairingAdvisor advisor)
{
    public async Task<PairingView> ExecuteAsync(string token, ParticipantId opponentId, bool withAdvice = false,
        CancellationToken ct = default)
    {
        var matrix = await getMatrix.ExecuteAsync(token, opponentId, ct);
        var session = await LoadAsync(matrix, ct);
        return Analyze(matrix, session, withAdvice);
    }

    internal async Task<(MatrixView, PairingSession)> LoadForEditAsync(string token, ParticipantId opponentId, CancellationToken ct)
    {
        var matrix = await getMatrix.ExecuteAsync(token, opponentId, ct);
        if (matrix.Access != AccessLevel.Edit) throw new ForbiddenException("Read-only link.");
        return (matrix, await LoadAsync(matrix, ct));
    }

    internal PairingView Analyze(MatrixView matrix, PairingSession session, bool withAdvice)
    {
        var scale = matrix.Preparation.RatingScale;
        double Score(PlayerId own, PlayerId opp) =>
            matrix.Matrix.RatingAt(new CellKey(own, opp)) is { } key ? (double)(scale.Find(key)?.Score ?? 0) : 0;

        var unrated = matrix.Matrix.OwnPlayerIds.Count * matrix.Matrix.OpponentPlayerIds.Count - matrix.Matrix.Cells.Count;
        var fixedScore = session.State.Games.Sum(g => Score(g.Own, g.Opponent));
        var advice = withAdvice ? advisor.Advise(session.State, Score) : null;
        return new PairingView(matrix, session, fixedScore, advice, unrated);
    }

    private async Task<PairingSession> LoadAsync(MatrixView matrix, CancellationToken ct)
    {
        var own = matrix.Own.Roster.Select(p => p.Id).ToList();
        var opp = matrix.Opponent.Roster.Select(p => p.Id).ToList();
        var canEdit = matrix.Access == AccessLevel.Edit;

        var session = await sessions.FindAsync(matrix.Preparation.Id, matrix.Opponent.Id, ct);
        if (session is null)
        {
            session = PairingSession.Start(matrix.Preparation.Id, matrix.Opponent.Id, own, opp);
            if (canEdit) await sessions.SaveAsync(session, ct);
        }
        else if (!session.OwnPlayerIds.SequenceEqual(own) || !session.OpponentPlayerIds.SequenceEqual(opp))
        {
            session.Restart(own, opp); // roster changed by a re-import
            if (canEdit) await sessions.SaveAsync(session, ct);
        }
        return session;
    }
}

public sealed class PlayPairingMove(GetPairing getPairing, IPairingSessionRepository sessions)
{
    public async Task<PairingView> ExecuteAsync(string token, ParticipantId opponentId, PairingMove move,
        bool withAdvice = false, CancellationToken ct = default)
    {
        var (matrix, session) = await getPairing.LoadForEditAsync(token, opponentId, ct);
        session.Apply(move);
        await sessions.SaveAsync(session, ct);
        return getPairing.Analyze(matrix, session, withAdvice);
    }
}

public sealed class UndoPairingMove(GetPairing getPairing, IPairingSessionRepository sessions)
{
    public async Task<PairingView> ExecuteAsync(string token, ParticipantId opponentId, bool withAdvice = false,
        CancellationToken ct = default)
    {
        var (matrix, session) = await getPairing.LoadForEditAsync(token, opponentId, ct);
        session.Undo();
        await sessions.SaveAsync(session, ct);
        return getPairing.Analyze(matrix, session, withAdvice);
    }
}

public sealed class RestartPairing(GetPairing getPairing, IPairingSessionRepository sessions)
{
    public async Task<PairingView> ExecuteAsync(string token, ParticipantId opponentId, CancellationToken ct = default)
    {
        var (matrix, session) = await getPairing.LoadForEditAsync(token, opponentId, ct);
        session.Restart(session.OwnPlayerIds, session.OpponentPlayerIds);
        await sessions.SaveAsync(session, ct);
        return getPairing.Analyze(matrix, session, withAdvice: false);
    }
}
