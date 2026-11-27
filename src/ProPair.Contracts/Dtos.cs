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
    IReadOnlyList<CellNoteDto> CellNotes, IReadOnlyList<PlayerNoteDto> PlayerNotes, IReadOnlyList<ListMarkDto> ListMarks);

/// <summary>Color: red | orange | yellow | green | blue | pink.</summary>
public sealed record ListMarkDto(Guid PlayerId, int LineIndex, string Color, string? Note, string Author, DateTimeOffset UpdatedAt);

/// <summary>Color null removes the mark. LineText guards against marking a line of a changed list.</summary>
public sealed record SetListMarkRequest(string LineText, string? Color, string? Note, string Author);

public sealed record NoteDto(string Text, string Author, DateTimeOffset UpdatedAt);

public sealed record CellNoteDto(Guid OwnPlayerId, Guid OpponentPlayerId, NoteDto Note);

public sealed record PlayerNoteDto(Guid PlayerId, NoteDto Note);

/// <summary>Empty text removes the note.</summary>
public sealed record SetNoteRequest(string? Text, string Author);

public sealed record RatingChangeDto(string? OldLevelKey, string? NewLevelKey, string Author, DateTimeOffset At);

public sealed record SetRatingRequest(Guid OwnPlayerId, Guid OpponentPlayerId, string? LevelKey, string Author);

public sealed record ErrorDto(string Message);

public sealed record GameDto(Guid OwnPlayerId, Guid OpponentPlayerId, double Score);

/// <summary>An option in the current step. Expected/Worst are final totals (our view); Probability 0–1.</summary>
public sealed record MoveOptionDto(IReadOnlyList<Guid> Players, double Expected, double Worst, double Probability);

/// <summary>Step: defenders | attackers | choices.</summary>
public sealed record PredictedStepDto(int Round, string Step, IReadOnlyList<Guid> Own, IReadOnlyList<Guid> Opponent,
    double OwnProbability, double OpponentProbability);

public sealed record PredictedGameDto(int Round, Guid OwnPlayerId, Guid OpponentPlayerId, double Score);

/// <summary>Real points: most likely value, its probability (0–1), and the average.</summary>
public sealed record OutcomeDto(double Value, double Probability, double Mean);

public sealed record PredictedPathDto(MoveOptionDto Option, IReadOnlyList<PredictedStepDto> Steps,
    IReadOnlyList<PredictedGameDto> Games, double Total, OutcomeDto ThisRound, OutcomeDto Final);

/// <summary>How much a rating level counts in the avoid-minus decision (e.g. "-" 1.5, "0/-" 1.25).</summary>
public sealed record MinusFactorDto(string LevelKey, double Factor);

/// <param name="Mode">optimal | avoidminus | safe. Weighting only affects the order; all shown values are real points.</param>
/// <param name="Expected">Value used for the decision (weighted in avoidminus) – not meant for display.</param>
public sealed record PairingAdviceDto(string Mode, IReadOnlyList<MinusFactorDto> MinusFactors, double Expected, IReadOnlyList<MoveOptionDto> Own, IReadOnlyList<MoveOptionDto> Opponent,
    IReadOnlyList<PredictedPathDto> TopPicks);

/// <summary>Step: defenders | attackers | choices. Own/Opponent: defender (1), attackers (2) or the accepted one (1).</summary>
public sealed record PairingMoveDto(string Step, IReadOnlyList<Guid> Own, IReadOnlyList<Guid> Opponent);

/// <summary>Full move list from a device; replaces the stored one (one captain, last write wins).</summary>
public sealed record SyncMovesRequest(IReadOnlyList<PairingMoveDto> Moves);

/// <summary>Precomputed round-start values for offline advice. Keys/Values are parallel arrays.</summary>
public sealed record RoundValuesDto(string Mode, IReadOnlyList<Guid> OwnOrder, IReadOnlyList<Guid> OpponentOrder,
    IReadOnlyList<int> Keys, IReadOnlyList<double> Values);

public sealed record PairingDto(
    string TournamentName, bool CanEdit, ParticipantDto Own, ParticipantDto Opponent,
    IReadOnlyList<RatingLevelDto> Scale, IReadOnlyList<CellDto> Cells,
    string Step, int Round, IReadOnlyList<Guid> RemainingOwn, IReadOnlyList<Guid> RemainingOpponent,
    Guid? OwnDefender, Guid? OpponentDefender, IReadOnlyList<Guid> OwnAttackers, IReadOnlyList<Guid> OpponentAttackers,
    IReadOnlyList<GameDto> Games, double FixedScore, PairingAdviceDto? Advice,
    int UnratedCells, int MoveCount, IReadOnlyList<PairingMoveDto> Moves);

public sealed record DefendersRequest(Guid Own, Guid Opponent);

public sealed record AttackersRequest(IReadOnlyList<Guid> Own, IReadOnlyList<Guid> Opponent);

/// <summary>OwnAccepts: their attacker we take. OpponentAccepts: our attacker they take.</summary>
public sealed record ChoicesRequest(Guid OwnAccepts, Guid OpponentAccepts);
