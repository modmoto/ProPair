using ProPair.Domain.Common;

namespace ProPair.Domain.Tournaments;

/// <summary>Players per team in a team tournament. Supported: 3, 5, 8.</summary>
public readonly record struct TeamSize
{
    public static readonly int[] Supported = [3, 5, 8];

    public int Value { get; }

    public TeamSize(int value)
    {
        if (!Supported.Contains(value)) throw new DomainException($"Team size {value} is not supported.");
        Value = value;
    }

    public override string ToString() => $"{Value}v{Value}";
}
