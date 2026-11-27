using System.Security.Cryptography;
using ProPair.Domain.Common;
using ProPair.Domain.Ratings;

namespace ProPair.Domain.Preparations;

/// <summary>Secret token for link-based access without login.</summary>
public readonly record struct ShareToken(string Value)
{
    public static ShareToken New() => new(Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16)));
    public override string ToString() => Value;
}

public enum AccessLevel { None, Read, Edit }

/// <summary>Our team's preparation for one tournament. The rating scale is copied from the team and fixed.</summary>
public sealed class Preparation : AggregateRoot<PreparationId>
{
    public TeamId TeamId { get; }
    public TournamentId TournamentId { get; }
    public ParticipantId OwnParticipantId { get; }
    public RatingScale RatingScale { get; }
    public ShareToken EditToken { get; }
    public ShareToken ReadToken { get; }

    public Preparation(PreparationId id, TeamId teamId, TournamentId tournamentId, ParticipantId ownParticipantId,
        RatingScale ratingScale, ShareToken editToken, ShareToken readToken) : base(id)
    {
        TeamId = teamId;
        TournamentId = tournamentId;
        OwnParticipantId = ownParticipantId;
        RatingScale = ratingScale;
        EditToken = editToken;
        ReadToken = readToken;
    }

    public static Preparation Start(Teams.Team team, Tournaments.Tournament tournament, ParticipantId ownParticipantId)
    {
        tournament.GetParticipant(ownParticipantId); // must exist
        return new Preparation(PreparationId.New(), team.Id, tournament.Id, ownParticipantId,
            team.DefaultRatingScale, ShareToken.New(), ShareToken.New());
    }

    public AccessLevel AccessFor(string token) =>
        token == EditToken.Value ? AccessLevel.Edit
        : token == ReadToken.Value ? AccessLevel.Read
        : AccessLevel.None;
}
