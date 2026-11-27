using ProPair.Application.UseCases;
using ProPair.Contracts;
using ProPair.Domain.GameSystems;
using ProPair.Domain.Matrices;
using ProPair.Domain.Preparations;
using ProPair.Domain.Ratings;
using ProPair.Domain.Tournaments;

namespace ProPair.Adapters.Rest;

internal static class DtoMapping
{
    public static TournamentDto ToDto(this Tournament t) =>
        new(t.Id.Value, t.ExternalId, t.Name, t.TeamSize?.Value, t.StartDate, t.Participants.Select(ToDto).ToList(),
            t.EndDate, t.ListsVisible, t.LastSyncedAt, IsDemo(t));

    public static TournamentSummaryDto ToSummaryDto(this Tournament t) =>
        new(t.Id.Value, t.ExternalId, t.Name, t.TeamSize?.Value, t.StartDate, t.EndDate, t.Participants.Count,
            t.ListsVisible, t.LastSyncedAt, IsDemo(t), t.Participants.Count(p => p.IsIncomplete));

    private static bool IsDemo(Tournament t) => t.ExternalId.StartsWith(FillDemoRatings.DemoPrefix);

    public static ParticipantDto ToDto(this Participant p) =>
        new(p.Id.Value, p.Name, p.Roster.Select(ToDto).ToList(), p.LeftOut);

    public static PlayerDto ToDto(this Player p)
    {
        var army = GameSystem.NinthAge.FindArmy(p.ArmyId);
        return new(p.Id.Value, p.Name, p.ArmyId, army?.Name ?? p.ArmyId, army?.Short ?? p.ArmyId, p.ListText, p.ListChangedAt, p.IsDummy);
    }

    public static IReadOnlyList<RatingLevelDto> ToDto(this RatingScale s) =>
        s.Levels.Select(l => new RatingLevelDto(l.Key, l.Label, l.Color, l.Score)).ToList();

    public static PreparationDto ToDto(this PreparationView v)
    {
        var canEdit = v.Access == AccessLevel.Edit;
        var t = v.Tournament;
        return new(t.Name, t.TeamSize?.Value ?? v.Own.Roster.Count, canEdit,
            canEdit ? v.Preparation.EditToken.Value : null, v.Preparation.ReadToken.Value,
            v.Own.ToDto(), v.Opponents.Select(o => new OpponentDto(o.Participant.ToDto(), o.RatedCells)).ToList(),
            v.Preparation.RatingScale.ToDto(), t.Id.Value, t.ListsVisible, t.LastSyncedAt, IsDemo(t));
    }

    public static MatrixDto ToDto(this MatrixView v) =>
        new(v.Tournament.Name, v.Access == AccessLevel.Edit,
            v.Tournament.ExternalId.StartsWith(FillDemoRatings.DemoPrefix), v.Own.ToDto(), v.Opponent.ToDto(),
            v.Preparation.RatingScale.ToDto(),
            v.Matrix.Cells.Select(c => new CellDto(c.Key.OwnPlayerId.Value, c.Key.OpponentPlayerId.Value, c.Value))
                .ToList(),
            v.Matrix.Notes.Select(n => new CellNoteDto(n.Key.OwnPlayerId.Value, n.Key.OpponentPlayerId.Value, n.Value.ToDto()))
                .ToList(),
            v.Preparation.PlayerNotes
                .Where(n => v.Own.Roster.Concat(v.Opponent.Roster).Any(p => p.Id == n.Key))
                .Select(n => new PlayerNoteDto(n.Key.Value, n.Value.ToDto())).ToList(),
            v.Own.Roster.Concat(v.Opponent.Roster)
                .SelectMany(p => v.Preparation.MarksFor(p.Id, p.ListText).Select(m => new ListMarkDto(
                    p.Id.Value, m.LineIndex, m.Color.ToString().ToLowerInvariant(), m.Note, m.Author, m.UpdatedAt)))
                .ToList());

    public static NoteDto ToDto(this Domain.Common.Note n) => new(n.Text, n.Author, n.UpdatedAt);

    public static PairingDto ToDto(this PairingView v)
    {
        var matrix = v.Matrix.ToDto();
        var state = v.Session.State;
        var scale = v.Matrix.Preparation.RatingScale;
        double Score(Domain.Pairing.Game g) =>
            v.Matrix.Matrix.RatingAt(new CellKey(g.Own, g.Opponent)) is { } key ? (double)(scale.Find(key)?.Score ?? 0) : 0;

        return new PairingDto(matrix.TournamentName, matrix.CanEdit, matrix.Own, matrix.Opponent, matrix.Scale, matrix.Cells,
            state.Step.ToString().ToLowerInvariant(), state.Round,
            state.RemainingOwn.Select(p => p.Value).ToList(), state.RemainingOpponent.Select(p => p.Value).ToList(),
            state.OwnDefender?.Value, state.OpponentDefender?.Value,
            state.OwnAttackers.Select(p => p.Value).ToList(), state.OpponentAttackers.Select(p => p.Value).ToList(),
            state.Games.Select(g => new GameDto(g.Own.Value, g.Opponent.Value, Score(g))).ToList(),
            v.FixedScore, v.Advice is { } advice ? advice.ToDto(v.Mode!.Value, scale) : null,
            v.UnratedCells, v.Session.Moves.Count, v.Session.Moves.Select(ToDto).ToList(),
            matrix.ListMarks, matrix.PlayerNotes, matrix.CellNotes); // lists and notes during the pairing, also offline
    }

    public static PairingMoveDto ToDto(this Domain.Pairing.PairingMove move) => move switch
    {
        Domain.Pairing.DefendersMove d => new("defenders", [d.Own.Value], [d.Opponent.Value]),
        Domain.Pairing.AttackersMove a => new("attackers", [a.OwnA.Value, a.OwnB.Value], [a.OpponentA.Value, a.OpponentB.Value]),
        Domain.Pairing.ChoicesMove c => new("choices", [c.OwnAccepts.Value], [c.OpponentAccepts.Value]),
        _ => throw new ArgumentOutOfRangeException(nameof(move)),
    };

    public static Domain.Pairing.PairingMove ToDomain(this PairingMoveDto m)
    {
        static Domain.Common.PlayerId P(Guid g) => new(g);
        return m.Step switch
        {
            "defenders" when m.Own.Count == 1 && m.Opponent.Count == 1 => new Domain.Pairing.DefendersMove(P(m.Own[0]), P(m.Opponent[0])),
            "attackers" when m.Own.Count == 2 && m.Opponent.Count == 2 =>
                new Domain.Pairing.AttackersMove(P(m.Own[0]), P(m.Own[1]), P(m.Opponent[0]), P(m.Opponent[1])),
            "choices" when m.Own.Count == 1 && m.Opponent.Count == 1 => new Domain.Pairing.ChoicesMove(P(m.Own[0]), P(m.Opponent[0])),
            _ => throw new Domain.Common.DomainException($"Invalid move '{m.Step}'."),
        };
    }

    private static List<Guid> Ids(IEnumerable<Domain.Common.PlayerId> ids) => ids.Select(i => i.Value).ToList();

    private static MoveOptionDto ToDto(this Domain.Pairing.Advice.MoveOption o) =>
        new(Ids(o.Players), o.Expected, o.Worst, o.Probability);

    private static PairingAdviceDto ToDto(this Domain.Pairing.Advice.PairingAdvice a, Domain.Pairing.Advice.AdviceMode mode,
        Domain.Ratings.RatingScale scale) => new(
        mode.ToString().ToLowerInvariant(),
        scale.Levels.Where(l => l.Score < 0).OrderBy(l => l.Score)
            .Select(l => new MinusFactorDto(l.Key, Domain.Pairing.Advice.AdviceModes.MinusFactor((double)l.Score)))
            .ToList(),
        a.Expected,
        a.Own.Select(ToDto).ToList(),
        a.Opponent.Select(ToDto).ToList(),
        a.TopPicks.Select(p => new PredictedPathDto(p.Option.ToDto(),
            p.Steps.Select(s => new PredictedStepDto(s.Round, s.Step.ToString().ToLowerInvariant(), Ids(s.Own), Ids(s.Opponent),
                s.OwnProbability, s.OpponentProbability)).ToList(),
            p.Games.Select(g => new PredictedGameDto(g.Round, g.Own.Value, g.Opponent.Value, g.Score)).ToList(),
            p.Total,
            new OutcomeDto(p.ThisRound.Value, p.ThisRound.Probability, p.ThisRound.Mean),
            new OutcomeDto(p.Final.Value, p.Final.Probability, p.Final.Mean))).ToList());
}
