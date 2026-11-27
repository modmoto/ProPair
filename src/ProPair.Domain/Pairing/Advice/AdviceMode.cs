namespace ProPair.Domain.Pairing.Advice;

/// <summary>How the team wants to be advised.</summary>
public enum AdviceMode
{
    /// <summary>Maximise the expected total; a minus counts exactly as much as a plus.</summary>
    Optimal,

    /// <summary>Minus matchups weigh more than plus ones (loss aversion) – avoid bad games first.</summary>
    AvoidMinus,

    /// <summary>Assume the opponent sees our move: maximise what we get at least (worst case).</summary>
    Safe,
}

/// <summary>Builds the advisor for a mode. All modes share the same game tree; they differ in scoring or step solver.</summary>
public static class AdviceModes
{
    /// <summary>Minus ratings are multiplied by this in <see cref="AdviceMode.AvoidMinus"/>: a "-" (−2) then costs as much as "+" and "0/+" together bring.</summary>
    public const double MinusWeight = 1.5;

    public static IPairingAdvisor Create(AdviceMode mode) => mode switch
    {
        AdviceMode.AvoidMinus => new WeightedScoreAdvisor(new GameTreeAdvisor(new EquilibriumSolver()),
            s => s < 0 ? s * MinusWeight : s),
        AdviceMode.Safe => new GameTreeAdvisor(new PessimisticSolver()),
        _ => new GameTreeAdvisor(new EquilibriumSolver()),
    };
}
