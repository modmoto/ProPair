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
        new(t.Id.Value, t.ExternalId, t.Name, t.TeamSize.Value, t.StartDate, t.Participants.Select(ToDto).ToList());

    public static ParticipantDto ToDto(this Participant p) =>
        new(p.Id.Value, p.Name, p.Roster.Select(ToDto).ToList());

    public static PlayerDto ToDto(this Player p)
    {
        var army = GameSystem.NinthAge.FindArmy(p.ArmyId);
        return new(p.Id.Value, p.Name, p.ArmyId, army?.Name ?? p.ArmyId, army?.Short ?? p.ArmyId, p.ListText);
    }

    public static IReadOnlyList<RatingLevelDto> ToDto(this RatingScale s) =>
        s.Levels.Select(l => new RatingLevelDto(l.Key, l.Label, l.Color, l.Score)).ToList();

    public static PreparationDto ToDto(this PreparationView v)
    {
        var canEdit = v.Access == AccessLevel.Edit;
        return new(v.Tournament.Name, v.Tournament.TeamSize.Value, canEdit,
            canEdit ? v.Preparation.EditToken.Value : null, v.Preparation.ReadToken.Value,
            v.Own.ToDto(), v.Opponents.Select(o => new OpponentDto(o.Participant.ToDto(), o.RatedCells)).ToList(),
            v.Preparation.RatingScale.ToDto());
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
                .Select(n => new PlayerNoteDto(n.Key.Value, n.Value.ToDto())).ToList());

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
            v.Analysis.Fixed, v.Analysis.Expected,
            v.Analysis.Options.Select(o => new PairingOptionDto(o.Players.Select(p => p.Value).ToList(), o.Worst, o.Average)).ToList(),
            v.UnratedCells, v.Session.Moves.Count);
    }
}
