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
    /// <summary>
    /// <see cref="AdviceMode.AvoidMinus"/>: every minus point adds this much weight. With the default scale
    /// "0/-" (−1) counts 1.25× and "-" (−2) 1.5× – a full minus then costs as much as "+" and "0/+" together bring.
    /// </summary>
    public const double ExtraWeightPerMinusPoint = 0.25;

    /// <summary>How much a rating with this score counts in <see cref="AdviceMode.AvoidMinus"/> (1 for zero and plus).</summary>
    public static double MinusFactor(double score) => score < 0 ? 1 + ExtraWeightPerMinusPoint * -score : 1;

    public static IPairingAdvisor Create(AdviceMode mode) => mode switch
    {
        AdviceMode.AvoidMinus => new GameTreeAdvisor(new EquilibriumSolver(), s => s * MinusFactor(s)),
        AdviceMode.Safe => new GameTreeAdvisor(new PessimisticSolver()),
        _ => new GameTreeAdvisor(new EquilibriumSolver()),
    };
}
