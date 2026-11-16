using VCS.Rules.V5.Dice;
using VCS.Rules.V5.Disciplines;
using VCS.Rules.V5.Rituals;

namespace VCS.Application.Blood;

/// <summary>Resolve a confirmed sanctuary travel attempt without moving or changing any character.</summary>
public sealed class TrueSanctuaryActions
{
    public DicePoolResult AttemptTravel(int pool, int hunger, bool circlesComplete, bool circlesIntact,
        bool atDepartureCircle, bool concentratedForTurn, bool alreadyAttemptedThisScene,
        Random? random = null, bool beastSubdued = false, bool sceneLimitExceptionApproved = false)
    {
        if (!TrueSanctuaryRules.CanAttemptTravel(circlesComplete, circlesIntact, atDepartureCircle,
            concentratedForTurn, alreadyAttemptedThisScene && !sceneLimitExceptionApproved))
            throw new InvalidOperationException("Confirme os círculos completos e intactos, a posição, a concentração e o limite de uma tentativa por cena.");
        if (pool is < 0 or > 50) throw new ArgumentOutOfRangeException(nameof(pool));
        if (hunger is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(hunger));
        var difficulty = RitualRules.DefaultRequirements(5).Difficulty;
        return QuellBeastRules.ApplyToRoll(DicePoolRules.Roll(pool, hunger, difficulty, random), beastSubdued);
    }
}
