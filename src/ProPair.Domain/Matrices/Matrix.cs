using ProPair.Domain.Common;
using ProPair.Domain.Ratings;

namespace ProPair.Domain.Matrices;

public readonly record struct CellKey(PlayerId OwnPlayerId, PlayerId OpponentPlayerId);

/// <summary>Append-only history entry. Also raised as domain event.</summary>
public sealed record RatingChanged(
    MatrixId MatrixId, CellKey Cell, string? OldLevelKey, string? NewLevelKey, string Author, DateTimeOffset OccurredAt)
    : IDomainEvent;

/// <summary>Ratings of our roster against one opponent participant.</summary>
public sealed class Matrix : AggregateRoot<MatrixId>
{
    private readonly Dictionary<CellKey, string> _cells;
    private readonly List<RatingChanged> _history;
    private readonly Dictionary<CellKey, Note> _notes;

    public PreparationId PreparationId { get; }
    public ParticipantId OpponentParticipantId { get; }
    public IReadOnlyList<PlayerId> OwnPlayerIds { get; private set; }
    public IReadOnlyList<PlayerId> OpponentPlayerIds { get; private set; }
    public IReadOnlyDictionary<CellKey, string> Cells => _cells;
    public IReadOnlyList<RatingChanged> History => _history;
    public IReadOnlyDictionary<CellKey, Note> Notes => _notes;

    public Matrix(MatrixId id, PreparationId preparationId, ParticipantId opponentParticipantId,
        IReadOnlyList<PlayerId> ownPlayerIds, IReadOnlyList<PlayerId> opponentPlayerIds,
        IDictionary<CellKey, string> cells, IEnumerable<RatingChanged> history,
        IDictionary<CellKey, Note>? notes = null) : base(id)
    {
        PreparationId = preparationId;
        OpponentParticipantId = opponentParticipantId;
        OwnPlayerIds = ownPlayerIds;
        OpponentPlayerIds = opponentPlayerIds;
        _cells = new Dictionary<CellKey, string>(cells);
        _history = history.ToList();
        _notes = new Dictionary<CellKey, Note>(notes ?? new Dictionary<CellKey, Note>());
    }

    public static Matrix Create(PreparationId preparationId, ParticipantId opponentParticipantId,
        IReadOnlyList<PlayerId> ownPlayerIds, IReadOnlyList<PlayerId> opponentPlayerIds) =>
        new(MatrixId.New(), preparationId, opponentParticipantId, ownPlayerIds, opponentPlayerIds,
            new Dictionary<CellKey, string>(), []);

    /// <summary>After a roster re-import: adopt the new players, drop cells of players that left.</summary>
    public bool SyncPlayers(IReadOnlyList<PlayerId> ownPlayerIds, IReadOnlyList<PlayerId> opponentPlayerIds)
    {
        if (OwnPlayerIds.SequenceEqual(ownPlayerIds) && OpponentPlayerIds.SequenceEqual(opponentPlayerIds)) return false;
        OwnPlayerIds = ownPlayerIds;
        OpponentPlayerIds = opponentPlayerIds;
        bool Gone(CellKey c) => !ownPlayerIds.Contains(c.OwnPlayerId) || !opponentPlayerIds.Contains(c.OpponentPlayerId);
        foreach (var cell in _cells.Keys.Where(Gone).ToList()) _cells.Remove(cell);
        foreach (var cell in _notes.Keys.Where(Gone).ToList()) _notes.Remove(cell);
        return true;
    }

    public string? RatingAt(CellKey cell) => _cells.GetValueOrDefault(cell);

    public Note? NoteAt(CellKey cell) => _notes.GetValueOrDefault(cell);

    public IEnumerable<RatingChanged> HistoryOf(CellKey cell) => _history.Where(h => h.Cell == cell);

    /// <summary>Sets the note of a matchup; empty text removes it.</summary>
    public void SetNote(CellKey cell, string? text, string author, DateTimeOffset now)
    {
        EnsureCell(cell);
        var note = Note.Create(text, author, now);
        if (note is null) _notes.Remove(cell);
        else if (_notes.GetValueOrDefault(cell)?.Text != note.Text) _notes[cell] = note;
    }

    private void EnsureCell(CellKey cell)
    {
        if (!OwnPlayerIds.Contains(cell.OwnPlayerId) || !OpponentPlayerIds.Contains(cell.OpponentPlayerId))
            throw new DomainException("Cell is not part of this matrix.");
    }

    /// <summary>Sets (or clears with null) the rating of a cell. Last write wins; every change is recorded.</summary>
    public void SetRating(CellKey cell, string? levelKey, string author, RatingScale scale, DateTimeOffset now)
    {
        EnsureCell(cell);
        if (levelKey is not null && !scale.Contains(levelKey))
            throw new DomainException($"Rating '{levelKey}' is not part of the rating scale.");
        if (string.IsNullOrWhiteSpace(author))
            throw new DomainException("Author is required.");

        var old = RatingAt(cell);
        if (old == levelKey) return;

        if (levelKey is null) _cells.Remove(cell);
        else _cells[cell] = levelKey;

        var change = new RatingChanged(Id, cell, old, levelKey, author.Trim(), now);
        _history.Add(change);
        Raise(change);
    }
}
