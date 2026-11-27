using ProPair.Domain.Common;

namespace ProPair.Domain.Ratings;

/// <summary>One step of a rating scale, e.g. "0/+" (light green, score 1).</summary>
public sealed record RatingLevel(string Key, string Label, string Color, decimal Score);

/// <summary>Ordered set of rating levels, best first. Immutable value object.</summary>
public sealed class RatingScale : IEquatable<RatingScale>
{
    public IReadOnlyList<RatingLevel> Levels { get; }

    public RatingScale(IEnumerable<RatingLevel> levels)
    {
        Levels = levels.ToList();
        if (Levels.Count < 2) throw new DomainException("A rating scale needs at least two levels.");
        if (Levels.Select(l => l.Key).Distinct().Count() != Levels.Count)
            throw new DomainException("Rating level keys must be unique.");
    }

    public static RatingScale Default { get; } = new([
        new("plus", "+", "#2e9e44", 2),
        new("zero-plus", "0/+", "#9fd86b", 1),
        new("zero", "0", "#f2d54b", 0),
        new("zero-minus", "0/-", "#f39a3d", -1),
        new("minus", "-", "#d9423a", -2),
    ]);

    public RatingLevel? Find(string key) => Levels.FirstOrDefault(l => l.Key == key);
    public bool Contains(string key) => Find(key) is not null;

    public bool Equals(RatingScale? other) => other is not null && Levels.SequenceEqual(other.Levels);
    public override bool Equals(object? obj) => Equals(obj as RatingScale);
    public override int GetHashCode() => Levels.Aggregate(0, (h, l) => HashCode.Combine(h, l));
}
