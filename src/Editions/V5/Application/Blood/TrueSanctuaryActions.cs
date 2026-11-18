using Game.Rules.V5.Dice;
using Game.Rules.V5.Disciplines;
using Game.Rules.V5.Rituals;
using Game.Sheet;

namespace Game.Application.Blood;

/// <summary>Resolve a confirmed sanctuary travel attempt without moving or changing any character.</summary>
public sealed class TrueSanctuaryActions
{
    public SanctuaryPreparationCheckResult PrepareNextCheck(int circle, DateOnly nightDate,
        IReadOnlyList<RitualPreparationStep> paidChecks, int hunger, bool conditionsConfirmed,
        bool scheduleExceptionApproved = false, Random? random = null)
    {
        if (!conditionsConfirmed)
            throw new InvalidOperationException("Confirme com o Narrador o círculo, os ingredientes e o tempo de preparação desta noite.");
        var advance = TrueSanctuaryRules.NextPreparationCheck(circle, nightDate, paidChecks, scheduleExceptionApproved);
        if (hunger is < 0 or >= 5)
            throw new InvalidOperationException("Fome 5 impede despertar o Sangue voluntariamente; nenhum custo foi rolado.");
        return new(advance, RouseCheckRules.Roll(hunger, random));
    }

    public SanctuaryTravelResult AttemptTravel(int pool, int hunger, bool circlesComplete, bool circlesIntact,
        bool atDepartureCircle, bool concentratedForTurn, bool alreadyAttemptedThisScene,
        Random? random = null, bool beastSubdued = false, bool sceneLimitExceptionApproved = false)
    {
        if (!TrueSanctuaryRules.CanAttemptTravel(circlesComplete, circlesIntact, atDepartureCircle,
            concentratedForTurn, alreadyAttemptedThisScene && !sceneLimitExceptionApproved))
            throw new InvalidOperationException("Confirme os círculos completos e intactos, a posição, a concentração e o limite de uma tentativa por cena.");
        if (pool is < 0 or > 50) throw new ArgumentOutOfRangeException(nameof(pool));
        if (hunger is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(hunger));
        var difficulty = RitualRules.DefaultRequirements(5).Difficulty;
        var originalRoll = DicePoolRules.Roll(pool, hunger, difficulty, random);
        var roll = QuellBeastRules.ApplyToRoll(originalRoll, beastSubdued);
        return new(roll, beastSubdued,
            originalRoll.Outcome == TestOutcome.MessyCritical && roll.Outcome == TestOutcome.CriticalSuccess);
    }
}

public sealed record SanctuaryPreparationCheckResult(SanctuaryPreparationAdvance Advance, RouseCheckResult Check);

public sealed record SanctuaryTravelResult(DicePoolResult Roll, bool BeastSubduedConsidered,
    bool MessyCriticalConverted);
