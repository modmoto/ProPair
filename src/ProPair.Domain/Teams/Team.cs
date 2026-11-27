using ProPair.Domain.Common;
using ProPair.Domain.Ratings;

namespace ProPair.Domain.Teams;

/// <summary>Our team across tournaments. Owns the default rating scale for future preparations.</summary>
public sealed class Team : AggregateRoot<TeamId>
{
    public string Name { get; private set; }
    public RatingScale DefaultRatingScale { get; private set; }

    public Team(TeamId id, string name, RatingScale defaultRatingScale) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Team name is required.");
        Name = name.Trim();
        DefaultRatingScale = defaultRatingScale;
    }

    public static Team Create(string name) => new(TeamId.New(), name, RatingScale.Default);

    public void ChangeDefaultRatingScale(RatingScale scale) => DefaultRatingScale = scale;
}
