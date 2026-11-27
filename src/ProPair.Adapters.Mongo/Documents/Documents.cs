using MongoDB.Bson.Serialization.Attributes;

namespace ProPair.Adapters.Mongo.Documents;

// Persistence models. Kept separate from the domain so the domain stays free of Mongo attributes.

public interface IVersionedDocument
{
    Guid Id { get; }
    long Version { get; set; }
}

public sealed class RatingLevelDoc
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Color { get; set; } = "";
    public decimal Score { get; set; }
}

public sealed class NoteDoc
{
    public string Text { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class PlayerNoteDoc
{
    public Guid PlayerId { get; set; }
    public NoteDoc Note { get; set; } = new();
}

public sealed class ListMarkDoc
{
    public Guid PlayerId { get; set; }
    public int LineIndex { get; set; }
    public string LineText { get; set; } = "";
    public string Color { get; set; } = "";
    public string? Note { get; set; }
    public string Author { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class CellNoteDoc
{
    public Guid Own { get; set; }
    public Guid Opponent { get; set; }
    public NoteDoc Note { get; set; } = new();
}

public sealed class TeamDoc : IVersionedDocument
{
    [BsonId] public Guid Id { get; set; }
    public long Version { get; set; }
    public string Name { get; set; } = "";
    public List<RatingLevelDoc> DefaultRatingScale { get; set; } = [];
}

public sealed class PlayerDoc
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string ArmyId { get; set; } = "";
    public string ListText { get; set; } = "";
}

public sealed class ParticipantDoc
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public List<PlayerDoc> Roster { get; set; } = [];
}

public sealed class TournamentDoc : IVersionedDocument
{
    [BsonId] public Guid Id { get; set; }
    public long Version { get; set; }
    public string ExternalId { get; set; } = "";
    public string Name { get; set; } = "";
    public string GameSystemId { get; set; } = "";
    public int TeamSize { get; set; }
    public DateOnly? StartDate { get; set; }
    public List<ParticipantDoc> Participants { get; set; } = [];
}

public sealed class PreparationDoc : IVersionedDocument
{
    [BsonId] public Guid Id { get; set; }
    public long Version { get; set; }
    public Guid TeamId { get; set; }
    public Guid TournamentId { get; set; }
    public Guid OwnParticipantId { get; set; }
    public List<RatingLevelDoc> RatingScale { get; set; } = [];
    public string EditToken { get; set; } = "";
    public string ReadToken { get; set; } = "";
    public List<PlayerNoteDoc> PlayerNotes { get; set; } = [];
    public List<ListMarkDoc> ListMarks { get; set; } = [];
}

public sealed class CellDoc
{
    public Guid Own { get; set; }
    public Guid Opponent { get; set; }
    public string Level { get; set; } = "";
}

public sealed class RatingChangeDoc
{
    public Guid Own { get; set; }
    public Guid Opponent { get; set; }
    public string? Old { get; set; }
    public string? New { get; set; }
    public string Author { get; set; } = "";
    public DateTimeOffset At { get; set; }
}

public sealed class MatrixDoc : IVersionedDocument
{
    [BsonId] public Guid Id { get; set; }
    public long Version { get; set; }
    public Guid PreparationId { get; set; }
    public Guid OpponentParticipantId { get; set; }
    public List<Guid> OwnPlayerIds { get; set; } = [];
    public List<Guid> OpponentPlayerIds { get; set; } = [];
    public List<CellDoc> Cells { get; set; } = [];
    public List<RatingChangeDoc> History { get; set; } = [];
    public List<CellNoteDoc> Notes { get; set; } = [];
}

public sealed class PairingMoveDoc
{
    public string Step { get; set; } = "";   // "defenders" | "attackers" | "choices"
    public List<Guid> Own { get; set; } = [];
    public List<Guid> Opponent { get; set; } = [];
}

public sealed class PairingSessionDoc : IVersionedDocument
{
    [BsonId] public Guid Id { get; set; }
    public long Version { get; set; }
    public Guid PreparationId { get; set; }
    public Guid OpponentParticipantId { get; set; }
    public List<Guid> OwnPlayerIds { get; set; } = [];
    public List<Guid> OpponentPlayerIds { get; set; } = [];
    public List<PairingMoveDoc> Moves { get; set; } = [];
}
