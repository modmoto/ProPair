using ProPair.Domain.Common;

namespace ProPair.Domain.Pairing;

public enum PairingStep { Defenders, Attackers, Choices, Done }

public sealed record Game(PlayerId Own, PlayerId Opponent);

/// <summary>A step both teams reveal at the same time. Entered by the captain after the reveal.</summary>
public abstract record PairingMove;

public sealed record DefendersMove(PlayerId Own, PlayerId Opponent) : PairingMove;

/// <summary>Own attackers go against the opponent defender, opponent attackers against ours.</summary>
public sealed record AttackersMove(PlayerId OwnA, PlayerId OwnB, PlayerId OpponentA, PlayerId OpponentB) : PairingMove;

/// <summary>
/// OwnAccepts: the opponent attacker we take against our defender.
/// OpponentAccepts: our attacker they take against their defender.
/// </summary>
public sealed record ChoicesMove(PlayerId OwnAccepts, PlayerId OpponentAccepts) : PairingMove;

/// <summary>
/// Immutable state of a team pairing (ETC/WTC style).
/// Round: defenders → 2 attackers each → defenders pick one; refused attackers return.
/// Final round with 4 left: refused play each other, the last two play each other.
/// Final round with 3 left: refused play each other.
/// </summary>
public sealed record PairingState
{
    public required IReadOnlyList<PlayerId> RemainingOwn { get; init; }
    public required IReadOnlyList<PlayerId> RemainingOpponent { get; init; }
    public required IReadOnlyList<Game> Games { get; init; }
    public PairingStep Step { get; init; }
    public int Round { get; init; } = 1;
    public PlayerId? OwnDefender { get; init; }
    public PlayerId? OpponentDefender { get; init; }
    public IReadOnlyList<PlayerId> OwnAttackers { get; init; } = [];
    public IReadOnlyList<PlayerId> OpponentAttackers { get; init; } = [];

    public bool IsFinalRound => RemainingOwn.Count <= 4;

    public static PairingState Start(IReadOnlyList<PlayerId> own, IReadOnlyList<PlayerId> opponent)
    {
        if (own.Count != opponent.Count) throw new DomainException("Both teams need the same number of players.");
        if (own.Count < 3) throw new DomainException("Pairing needs at least 3 players per team.");
        if (own.Distinct().Count() != own.Count || opponent.Distinct().Count() != opponent.Count)
            throw new DomainException("Players must be unique.");
        return new PairingState { RemainingOwn = own, RemainingOpponent = opponent, Games = [] };
    }

    public PairingState Apply(PairingMove move) => (Step, move) switch
    {
        (PairingStep.Defenders, DefendersMove m) => ApplyDefenders(m),
        (PairingStep.Attackers, AttackersMove m) => ApplyAttackers(m),
        (PairingStep.Choices, ChoicesMove m) => ApplyChoices(m),
        (PairingStep.Done, _) => throw new DomainException("Pairing is finished."),
        _ => throw new DomainException($"Expected {Step}, got {move.GetType().Name}."),
    };

    private PairingState ApplyDefenders(DefendersMove m)
    {
        Require(RemainingOwn.Contains(m.Own), "Own defender is not available.");
        Require(RemainingOpponent.Contains(m.Opponent), "Opponent defender is not available.");
        return this with { Step = PairingStep.Attackers, OwnDefender = m.Own, OpponentDefender = m.Opponent };
    }

    private PairingState ApplyAttackers(AttackersMove m)
    {
        Require(m.OwnA != m.OwnB && m.OpponentA != m.OpponentB, "Two different attackers are required.");
        Require(new[] { m.OwnA, m.OwnB }.All(p => RemainingOwn.Contains(p) && p != OwnDefender),
            "Own attackers must be available and not the defender.");
        Require(new[] { m.OpponentA, m.OpponentB }.All(p => RemainingOpponent.Contains(p) && p != OpponentDefender),
            "Opponent attackers must be available and not the defender.");
        return this with
        {
            Step = PairingStep.Choices,
            OwnAttackers = [m.OwnA, m.OwnB],
            OpponentAttackers = [m.OpponentA, m.OpponentB],
        };
    }

    private PairingState ApplyChoices(ChoicesMove m)
    {
        Require(OpponentAttackers.Contains(m.OwnAccepts), "We must accept one of the opponent attackers.");
        Require(OwnAttackers.Contains(m.OpponentAccepts), "They must accept one of our attackers.");

        var ownDefender = OwnDefender!.Value;
        var oppDefender = OpponentDefender!.Value;
        var ownRefused = OwnAttackers.Single(p => p != m.OpponentAccepts);
        var oppRefused = OpponentAttackers.Single(p => p != m.OwnAccepts);

        var games = Games.ToList();
        games.Add(new Game(ownDefender, m.OwnAccepts));
        games.Add(new Game(m.OpponentAccepts, oppDefender));

        var own = RemainingOwn.Where(p => p != ownDefender && p != m.OpponentAccepts).ToList();
        var opp = RemainingOpponent.Where(p => p != oppDefender && p != m.OwnAccepts).ToList();

        if (IsFinalRound)
        {
            games.Add(new Game(ownRefused, oppRefused));
            own.Remove(ownRefused);
            opp.Remove(oppRefused);
            if (own.Count == 1) games.Add(new Game(own[0], opp[0])); // 4 left: the last two play each other
            return new PairingState { RemainingOwn = [], RemainingOpponent = [], Games = games, Step = PairingStep.Done, Round = Round };
        }

        return new PairingState { RemainingOwn = own, RemainingOpponent = opp, Games = games, Round = Round + 1 };
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new DomainException(message);
    }
}
