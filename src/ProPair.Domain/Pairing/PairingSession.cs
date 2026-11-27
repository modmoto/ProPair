using ProPair.Domain.Common;

namespace ProPair.Domain.Pairing;

public readonly record struct PairingSessionId(Guid Value)
{
    public static PairingSessionId New() => new(Guid.CreateVersion7());
}

/// <summary>
/// A live pairing against one opponent. Stores the entered moves; the state is derived by replaying them,
/// which makes undo trivial and keeps the rules in one place (<see cref="PairingState"/>).
/// </summary>
public sealed class PairingSession : AggregateRoot<PairingSessionId>
{
    private readonly List<PairingMove> _moves;

    public PreparationId PreparationId { get; }
    public ParticipantId OpponentParticipantId { get; }
    public IReadOnlyList<PlayerId> OwnPlayerIds { get; private set; }
    public IReadOnlyList<PlayerId> OpponentPlayerIds { get; private set; }
    public IReadOnlyList<PairingMove> Moves => _moves;
    public PairingState State { get; private set; }

    public PairingSession(PairingSessionId id, PreparationId preparationId, ParticipantId opponentParticipantId,
        IReadOnlyList<PlayerId> ownPlayerIds, IReadOnlyList<PlayerId> opponentPlayerIds, IEnumerable<PairingMove> moves)
        : base(id)
    {
        PreparationId = preparationId;
        OpponentParticipantId = opponentParticipantId;
        OwnPlayerIds = ownPlayerIds;
        OpponentPlayerIds = opponentPlayerIds;
        _moves = moves.ToList();
        State = Replay(ownPlayerIds, opponentPlayerIds, _moves);
    }

    public static PairingSession Start(PreparationId preparationId, ParticipantId opponentId,
        IReadOnlyList<PlayerId> own, IReadOnlyList<PlayerId> opponent) =>
        new(PairingSessionId.New(), preparationId, opponentId, own, opponent, []);

    public void Apply(PairingMove move)
    {
        State = State.Apply(move);
        _moves.Add(move);
    }

    public void Undo()
    {
        if (_moves.Count == 0) return;
        _moves.RemoveAt(_moves.Count - 1);
        State = Replay(OwnPlayerIds, OpponentPlayerIds, _moves);
    }

    /// <summary>
    /// Takes over the moves entered on a device (possibly offline). The whole list is replayed through the rules,
    /// so an invalid list is rejected as a whole. One captain enters the pairing – last write wins.
    /// </summary>
    public void ReplaceMoves(IReadOnlyList<PairingMove> moves)
    {
        State = Replay(OwnPlayerIds, OpponentPlayerIds, moves);
        _moves.Clear();
        _moves.AddRange(moves);
    }

    /// <summary>Starts over, e.g. after a roster change.</summary>
    public void Restart(IReadOnlyList<PlayerId> own, IReadOnlyList<PlayerId> opponent)
    {
        OwnPlayerIds = own;
        OpponentPlayerIds = opponent;
        _moves.Clear();
        State = PairingState.Start(own, opponent);
    }

    private static PairingState Replay(IReadOnlyList<PlayerId> own, IReadOnlyList<PlayerId> opp, IEnumerable<PairingMove> moves) =>
        moves.Aggregate(PairingState.Start(own, opp), (s, m) => s.Apply(m));
}
