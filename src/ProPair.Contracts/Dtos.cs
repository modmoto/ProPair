namespace ProPair.Contracts;

public sealed record SourceTournamentDto(string ExternalId, string Name, DateOnly? StartDate, int TeamSize, int TeamCount);

public sealed record ImportTournamentRequest(string ExternalId);

public sealed record PlayerDto(Guid Id, string Name, string ArmyId, string ArmyName, string ArmyShort, string ListText);

public sealed record ParticipantDto(Guid Id, string Name, IReadOnlyList<PlayerDto> Players);

public sealed record TournamentDto(
    Guid Id, string ExternalId, string Name, int TeamSize, DateOnly? StartDate, IReadOnlyList<ParticipantDto> Participants);

public sealed record StartPreparationRequest(Guid ParticipantId);

public sealed record PreparationCreatedDto(string EditToken, string ReadToken);

public sealed record RatingLevelDto(string Key, string Label, string Color, decimal Score);

public sealed record OpponentDto(ParticipantDto Participant, int RatedCells);

public sealed record PreparationDto(
    string TournamentName, int TeamSize, bool CanEdit, string? EditToken, string ReadToken,
    ParticipantDto Own, IReadOnlyList<OpponentDto> Opponents, IReadOnlyList<RatingLevelDto> Scale);

public sealed record CellDto(Guid OwnPlayerId, Guid OpponentPlayerId, string LevelKey);

public sealed record MatrixDto(
    string TournamentName, bool CanEdit, bool IsDemo, ParticipantDto Own, ParticipantDto Opponent,
    IReadOnlyList<RatingLevelDto> Scale, IReadOnlyList<CellDto> Cells,
    IReadOnlyList<CellNoteDto> CellNotes, IReadOnlyList<PlayerNoteDto> PlayerNotes);

public sealed record NoteDto(string Text, string Author, DateTimeOffset UpdatedAt);

public sealed record CellNoteDto(Guid OwnPlayerId, Guid OpponentPlayerId, NoteDto Note);

public sealed record PlayerNoteDto(Guid PlayerId, NoteDto Note);

/// <summary>Empty text removes the note.</summary>
public sealed record SetNoteRequest(string? Text, string Author);

public sealed record RatingChangeDto(string? OldLevelKey, string? NewLevelKey, string Author, DateTimeOffset At);

public sealed record SetRatingRequest(Guid OwnPlayerId, Guid OpponentPlayerId, string? LevelKey, string Author);

public sealed record ErrorDto(string Message);

public sealed record GameDto(Guid OwnPlayerId, Guid OpponentPlayerId, double Score);

/// <summary>Our option in the current step: Worst = opponent answers optimally, Average = over all answers.</summary>
public sealed record PairingOptionDto(IReadOnlyList<Guid> Players, double Worst, double Average);

public sealed record PairingDto(
    string TournamentName, bool CanEdit, ParticipantDto Own, ParticipantDto Opponent,
    IReadOnlyList<RatingLevelDto> Scale, IReadOnlyList<CellDto> Cells,
    string Step, int Round, IReadOnlyList<Guid> RemainingOwn, IReadOnlyList<Guid> RemainingOpponent,
    Guid? OwnDefender, Guid? OpponentDefender, IReadOnlyList<Guid> OwnAttackers, IReadOnlyList<Guid> OpponentAttackers,
    IReadOnlyList<GameDto> Games, double FixedScore, double ExpectedScore, IReadOnlyList<PairingOptionDto> Options,
    int UnratedCells, int MoveCount);

public sealed record DefendersRequest(Guid Own, Guid Opponent);

public sealed record AttackersRequest(IReadOnlyList<Guid> Own, IReadOnlyList<Guid> Opponent);

/// <summary>OwnAccepts: their attacker we take. OpponentAccepts: our attacker they take.</summary>
public sealed record ChoicesRequest(Guid OwnAccepts, Guid OpponentAccepts);
