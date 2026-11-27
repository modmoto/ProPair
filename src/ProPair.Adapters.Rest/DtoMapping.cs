using ProPair.Application.UseCases;
using ProPair.Contracts;
using ProPair.Domain.GameSystems;
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
        new(v.Tournament.Name, v.Access == AccessLevel.Edit, v.Own.ToDto(), v.Opponent.ToDto(),
            v.Preparation.RatingScale.ToDto(),
            v.Matrix.Cells.Select(c => new CellDto(c.Key.OwnPlayerId.Value, c.Key.OpponentPlayerId.Value, c.Value))
                .ToList());
}
