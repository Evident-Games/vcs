using Game.Content;

namespace Game.Rules.V5.Rituals;

/// <summary>Validates the caster-owned sacrifice and vessel used by Firewalker.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base), p. 278")]
public static class FirewalkerRules
{
    public static void ValidatePreparation(bool casterFingertipConfirmed, bool goldenVesselConfirmed,
        bool casterBloodConfirmed, bool exceptionApproved)
    {
        if (exceptionApproved) return;
        if (!casterFingertipConfirmed)
            throw new InvalidOperationException("Confirme que a ponta do dedo escolhida pertence ao conjurador.");
        if (!goldenVesselConfirmed)
            throw new InvalidOperationException("Confirme a tigela ou o cálice dourado usado no processo.");
        if (!casterBloodConfirmed)
            throw new InvalidOperationException("Confirme que o Sangue preparado para a queima pertence ao conjurador.");
    }
}
