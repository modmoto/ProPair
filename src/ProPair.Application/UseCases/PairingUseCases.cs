using ProPair.Application.Ports;
using ProPair.Domain.Common;
using ProPair.Domain.Matrices;
using ProPair.Domain.Pairing;
using ProPair.Domain.Pairing.Advice;
using ProPair.Domain.Preparations;

namespace ProPair.Application.UseCases;

/// <param name="Advice">Recommendations; only computed when asked for (they are optional in the UI and cost up to a second).</param>
public sealed record PairingView(MatrixView Matrix, PairingSession Session, double FixedScore, PairingAdvice? Advice,
    AdviceMode? Mode, int UnratedCells);

/// <summary>Port: which advisor to use for a mode. Swap the implementation to change the algorithm.</summary>
public interface IPairingAdvisorProvider
{
    IPairingAdvisor For(AdviceMode mode);
}

public sealed class DefaultPairingAdvisorProvider : IPairingAdvisorProvider
{
    private readonly Dictionary<AdviceMode, IPairingAdvisor> _advisors =
        Enum.GetValues<AdviceMode>().ToDictionary(m => m, AdviceModes.Create);

    public IPairingAdvisor For(AdviceMode mode) => _advisors[mode];
}

/// <summary>Loads (or starts) the live pairing against one opponent; optionally asks the advisor for recommendations.</summary>
public sealed class GetPairing(GetMatrix getMatrix, IPairingSessionRepository sessions, IPairingAdvisorProvider advisors)
{
    /// <param name="advice">null = no recommendations (they are optional in the UI and cost up to a second).</param>
    public async Task<PairingView> ExecuteAsync(string token, ParticipantId opponentId, AdviceMode? advice = null,
        CancellationToken ct = default)
    {
        var matrix = await getMatrix.ExecuteAsync(token, opponentId, ct);
        var session = await LoadAsync(matrix, ct);
        return Analyze(matrix, session, advice);
    }

    internal async Task<(MatrixView, PairingSession)> LoadForEditAsync(string token, ParticipantId opponentId, CancellationToken ct)
    {
        var matrix = await getMatrix.ExecuteAsync(token, opponentId, ct);
        if (matrix.Access != AccessLevel.Edit) throw new ForbiddenException("Read-only link.");
        return (matrix, await LoadAsync(matrix, ct));
    }

    internal PairingView Analyze(MatrixView matrix, PairingSession session, AdviceMode? mode)
    {
        var scale = matrix.Preparation.RatingScale;
        double Score(PlayerId own, PlayerId opp) =>
            matrix.Matrix.RatingAt(new CellKey(own, opp)) is { } key ? (double)(scale.Find(key)?.Score ?? 0) : 0;

        var unrated = matrix.Matrix.OwnPlayerIds.Count * matrix.Matrix.OpponentPlayerIds.Count - matrix.Matrix.Cells.Count;
        var fixedScore = session.State.Games.Sum(g => Score(g.Own, g.Opponent));
        var advice = mode is { } m ? advisors.For(m).Advise(session.State, Score) : null;
        return new PairingView(matrix, session, fixedScore, advice, mode, unrated);
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
        AdviceMode? advice = null, CancellationToken ct = default)
    {
        var (matrix, session) = await getPairing.LoadForEditAsync(token, opponentId, ct);
        session.Apply(move);
        await sessions.SaveAsync(session, ct);
        return getPairing.Analyze(matrix, session, advice);
    }
}

public sealed class UndoPairingMove(GetPairing getPairing, IPairingSessionRepository sessions)
{
    public async Task<PairingView> ExecuteAsync(string token, ParticipantId opponentId, AdviceMode? advice = null,
        CancellationToken ct = default)
    {
        var (matrix, session) = await getPairing.LoadForEditAsync(token, opponentId, ct);
        session.Undo();
        await sessions.SaveAsync(session, ct);
        return getPairing.Analyze(matrix, session, advice);
    }
}

public sealed class RestartPairing(GetPairing getPairing, IPairingSessionRepository sessions)
{
    public async Task<PairingView> ExecuteAsync(string token, ParticipantId opponentId, CancellationToken ct = default)
    {
        var (matrix, session) = await getPairing.LoadForEditAsync(token, opponentId, ct);
        session.Restart(session.OwnPlayerIds, session.OpponentPlayerIds);
        await sessions.SaveAsync(session, ct);
        return getPairing.Analyze(matrix, session, null);
    }
}
