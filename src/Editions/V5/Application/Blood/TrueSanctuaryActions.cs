using Game.Rules.V5.Dice;
using Game.Rules.V5.Disciplines;
using Game.Rules.V5.Rituals;
using Game.Sheet;

namespace Game.Application.Blood;

/// <summary>Resolve a confirmed sanctuary travel attempt without moving or changing any character.</summary>
public sealed class TrueSanctuaryActions
{
    public SanctuaryPairInvalidation InvalidatePairAfterCircleDamage(
        IReadOnlyList<PowerEffectEntry> preparationEntries, int damagedCircle, bool tableConfirmed)
    {
        var summary = TrueSanctuaryRules.CircleDamageEnding(damagedCircle, tableConfirmed);
        var active = preparationEntries.Where(x => x.Active && x.IsSelf &&
            x.PowerId == "ritual-sanctuary-preparation-check" && x.PreparationStep is not null).ToArray();
        if (active.Length == 0)
            throw new InvalidOperationException("Não há preparação ativa do Santuário para inutilizar.");
        var ended = active.Select(x => x with
        {
            Active = false,
            Effect = $"{x.Effect} {summary}",
            Duration = "Par de círculos inerte após dano confirmado"
        }).ToArray();
        return new(ended, summary);
    }

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
        bool carriesPerson, bool carriesObjects, string payloadDescription, bool objectMassConfirmed,
        Random? random = null, bool beastSubdued = false, bool sceneLimitExceptionApproved = false)
    {
        if (!TrueSanctuaryRules.CanAttemptTravel(circlesComplete, circlesIntact, atDepartureCircle,
            concentratedForTurn, alreadyAttemptedThisScene && !sceneLimitExceptionApproved))
            throw new InvalidOperationException("Confirme os círculos completos e intactos, a posição, a concentração e o limite de uma tentativa por cena.");
        var payload = TrueSanctuaryRules.ValidatePayload(carriesPerson, carriesObjects, payloadDescription,
            objectMassConfirmed);
        if (pool is < 0 or > 50) throw new ArgumentOutOfRangeException(nameof(pool));
        if (hunger is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(hunger));
        var difficulty = RitualRules.DefaultRequirements(5).Difficulty;
        var originalRoll = DicePoolRules.Roll(pool, hunger, difficulty, random);
        var roll = QuellBeastRules.ApplyToRoll(originalRoll, beastSubdued);
        return new(roll, payload, beastSubdued,
            originalRoll.Outcome == TestOutcome.MessyCritical && roll.Outcome == TestOutcome.CriticalSuccess);
    }
}

public sealed record SanctuaryPreparationCheckResult(SanctuaryPreparationAdvance Advance, RouseCheckResult Check);

public sealed record SanctuaryPairInvalidation(IReadOnlyList<PowerEffectEntry> EndedEntries, string Summary);

public sealed record SanctuaryTravelResult(DicePoolResult Roll, SanctuaryTravelPayload Payload,
    bool BeastSubduedConsidered,
    bool MessyCriticalConverted);
