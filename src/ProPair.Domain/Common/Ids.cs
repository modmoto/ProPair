namespace ProPair.Domain.Common;

public readonly record struct TeamId(Guid Value)
{
    public static TeamId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}

public readonly record struct TournamentId(Guid Value)
{
    public static TournamentId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}

public readonly record struct PreparationId(Guid Value)
{
    public static PreparationId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}

public readonly record struct MatrixId(Guid Value)
{
    public static MatrixId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}

public readonly record struct ParticipantId(Guid Value)
{
    public static ParticipantId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}

public readonly record struct PlayerId(Guid Value)
{
    public static PlayerId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
