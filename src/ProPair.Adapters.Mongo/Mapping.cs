using ProPair.Adapters.Mongo.Documents;
using ProPair.Domain.Common;
using ProPair.Domain.Matrices;
using ProPair.Domain.Pairing;
using ProPair.Domain.Preparations;
using ProPair.Domain.Ratings;
using ProPair.Domain.Teams;
using ProPair.Domain.Tournaments;

namespace ProPair.Adapters.Mongo;

internal static class Mapping
{
    public static List<RatingLevelDoc> ToDoc(RatingScale s) =>
        s.Levels.Select(l => new RatingLevelDoc { Key = l.Key, Label = l.Label, Color = l.Color, Score = l.Score }).ToList();

    public static RatingScale ToDomain(List<RatingLevelDoc> d) =>
        new(d.Select(l => new RatingLevel(l.Key, l.Label, l.Color, l.Score)));

    private static NoteDoc ToDoc(Note n) => new() { Text = n.Text, Author = n.Author, UpdatedAt = n.UpdatedAt };
    private static Note ToDomain(NoteDoc n) => new(n.Text, n.Author, n.UpdatedAt);

    public static TeamDoc ToDoc(Team t) => new()
    {
        Id = t.Id.Value, Version = t.Version, Name = t.Name, DefaultRatingScale = ToDoc(t.DefaultRatingScale),
    };

    public static Team ToDomain(TeamDoc d) =>
        WithVersion(new Team(new TeamId(d.Id), d.Name, ToDomain(d.DefaultRatingScale)), d.Version);

    public static TournamentDoc ToDoc(Tournament t) => new()
    {
        Id = t.Id.Value, Version = t.Version, ExternalId = t.ExternalId, Name = t.Name,
        GameSystemId = t.GameSystemId, TeamSize = t.TeamSize?.Value, StartDate = t.StartDate,
        EndDate = t.EndDate, ListsVisible = t.ListsVisible, LastSyncedAt = t.LastSyncedAt,
        Participants = t.Participants.Select(p => new ParticipantDoc
        {
            Id = p.Id.Value, Name = p.Name,
            Roster = p.Roster.Select(r => new PlayerDoc
                { Id = r.Id.Value, Name = r.Name, ArmyId = r.ArmyId, ListText = r.ListText, ListChangedAt = r.ListChangedAt }).ToList(),
        }).ToList(),
    };

    public static Tournament ToDomain(TournamentDoc d) => WithVersion(new Tournament(
        new TournamentId(d.Id), d.ExternalId, d.Name, d.GameSystemId, d.TeamSize is { } size ? new TeamSize(size) : null,
        d.StartDate,
        d.Participants.Select(p => new Participant(new ParticipantId(p.Id), p.Name,
            p.Roster.Select(r => new Player(new PlayerId(r.Id), r.Name, r.ArmyId, r.ListText, r.ListChangedAt)).ToList())),
        d.EndDate, d.ListsVisible ?? true, d.LastSyncedAt), d.Version);

    public static PreparationDoc ToDoc(Preparation p) => new()
    {
        Id = p.Id.Value, Version = p.Version, TeamId = p.TeamId.Value, TournamentId = p.TournamentId.Value,
        OwnParticipantId = p.OwnParticipantId.Value, RatingScale = ToDoc(p.RatingScale),
        EditToken = p.EditToken.Value, ReadToken = p.ReadToken.Value,
        PlayerNotes = p.PlayerNotes.Select(n => new PlayerNoteDoc { PlayerId = n.Key.Value, Note = ToDoc(n.Value) }).ToList(),
        ListMarks = p.ListMarks.Select(m => new ListMarkDoc
        {
            PlayerId = m.Key.PlayerId.Value, LineIndex = m.Value.LineIndex, LineText = m.Value.LineText,
            Color = m.Value.Color.ToString(), Note = m.Value.Note, Author = m.Value.Author, UpdatedAt = m.Value.UpdatedAt,
        }).ToList(),
    };

    public static Preparation ToDomain(PreparationDoc d) => WithVersion(new Preparation(
        new PreparationId(d.Id), new TeamId(d.TeamId), new TournamentId(d.TournamentId),
        new ParticipantId(d.OwnParticipantId), ToDomain(d.RatingScale),
        new ShareToken(d.EditToken), new ShareToken(d.ReadToken),
        (d.PlayerNotes ?? []).ToDictionary(n => new PlayerId(n.PlayerId), n => ToDomain(n.Note)),
        (d.ListMarks ?? []).ToDictionary(m => new ListLineKey(new PlayerId(m.PlayerId), m.LineIndex),
            m => new ListMark(m.LineIndex, m.LineText, Enum.Parse<MarkColor>(m.Color), m.Note, m.Author, m.UpdatedAt))),
        d.Version);

    public static MatrixDoc ToDoc(Matrix m) => new()
    {
        Id = m.Id.Value, Version = m.Version, PreparationId = m.PreparationId.Value,
        OpponentParticipantId = m.OpponentParticipantId.Value,
        OwnPlayerIds = m.OwnPlayerIds.Select(i => i.Value).ToList(),
        OpponentPlayerIds = m.OpponentPlayerIds.Select(i => i.Value).ToList(),
        Cells = m.Cells.Select(c => new CellDoc
            { Own = c.Key.OwnPlayerId.Value, Opponent = c.Key.OpponentPlayerId.Value, Level = c.Value }).ToList(),
        History = m.History.Select(h => new RatingChangeDoc
        {
            Own = h.Cell.OwnPlayerId.Value, Opponent = h.Cell.OpponentPlayerId.Value,
            Old = h.OldLevelKey, New = h.NewLevelKey, Author = h.Author, At = h.OccurredAt,
        }).ToList(),
        Notes = m.Notes.Select(n => new CellNoteDoc
            { Own = n.Key.OwnPlayerId.Value, Opponent = n.Key.OpponentPlayerId.Value, Note = ToDoc(n.Value) }).ToList(),
    };

    public static Matrix ToDomain(MatrixDoc d)
    {
        var id = new MatrixId(d.Id);
        return WithVersion(new Matrix(id, new PreparationId(d.PreparationId), new ParticipantId(d.OpponentParticipantId),
            d.OwnPlayerIds.Select(i => new PlayerId(i)).ToList(),
            d.OpponentPlayerIds.Select(i => new PlayerId(i)).ToList(),
            d.Cells.ToDictionary(c => new CellKey(new PlayerId(c.Own), new PlayerId(c.Opponent)), c => c.Level),
            d.History.Select(h => new RatingChanged(id, new CellKey(new PlayerId(h.Own), new PlayerId(h.Opponent)),
                h.Old, h.New, h.Author, h.At)),
            (d.Notes ?? []).ToDictionary(n => new CellKey(new PlayerId(n.Own), new PlayerId(n.Opponent)), n => ToDomain(n.Note))),
            d.Version);
    }

    public static PairingSessionDoc ToDoc(PairingSession s) => new()
    {
        Id = s.Id.Value, Version = s.Version, PreparationId = s.PreparationId.Value,
        OpponentParticipantId = s.OpponentParticipantId.Value,
        OwnPlayerIds = s.OwnPlayerIds.Select(i => i.Value).ToList(),
        OpponentPlayerIds = s.OpponentPlayerIds.Select(i => i.Value).ToList(),
        Moves = s.Moves.Select(ToDoc).ToList(),
    };

    private static PairingMoveDoc ToDoc(PairingMove m) => m switch
    {
        DefendersMove d => new() { Step = "defenders", Own = [d.Own.Value], Opponent = [d.Opponent.Value] },
        AttackersMove a => new() { Step = "attackers", Own = [a.OwnA.Value, a.OwnB.Value], Opponent = [a.OpponentA.Value, a.OpponentB.Value] },
        ChoicesMove c => new() { Step = "choices", Own = [c.OwnAccepts.Value], Opponent = [c.OpponentAccepts.Value] },
        _ => throw new ArgumentOutOfRangeException(nameof(m)),
    };

    public static PairingSession ToDomain(PairingSessionDoc d) => WithVersion(new PairingSession(
        new PairingSessionId(d.Id), new PreparationId(d.PreparationId), new ParticipantId(d.OpponentParticipantId),
        d.OwnPlayerIds.Select(i => new PlayerId(i)).ToList(),
        d.OpponentPlayerIds.Select(i => new PlayerId(i)).ToList(),
        d.Moves.Select(ToDomain)), d.Version);

    private static PairingMove ToDomain(PairingMoveDoc m) => m.Step switch
    {
        "defenders" => new DefendersMove(new PlayerId(m.Own[0]), new PlayerId(m.Opponent[0])),
        "attackers" => new AttackersMove(new PlayerId(m.Own[0]), new PlayerId(m.Own[1]),
            new PlayerId(m.Opponent[0]), new PlayerId(m.Opponent[1])),
        "choices" => new ChoicesMove(new PlayerId(m.Own[0]), new PlayerId(m.Opponent[0])),
        _ => throw new InvalidOperationException($"Unknown pairing step '{m.Step}'."),
    };

    private static T WithVersion<T>(T aggregate, long version) where T : IAggregateRoot
    {
        aggregate.SetPersistedVersion(version);
        return aggregate;
    }
}
