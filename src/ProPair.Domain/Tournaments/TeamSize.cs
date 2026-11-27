using ProPair.Domain.Common;

namespace ProPair.Domain.Tournaments;

/// <summary>Players per team in a team tournament: 3 to 8 (ETC uses 8, many events 3, 4 or 5).</summary>
public readonly record struct TeamSize
{
    public const int Min = 3;
    public const int Max = 8;

    public int Value { get; }

    public TeamSize(int value)
    {
        if (value is < Min or > Max) throw new DomainException($"Team size {value} is not supported (3–8).");
        Value = value;
    }

    public override string ToString() => $"{Value}v{Value}";
}
