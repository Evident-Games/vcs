using Game.Content.Disciplines;
using Game.Rules.V5.Disciplines;
using Game.Rules.V5.Dice;
using Game.Rules.V5.Rituals;
using Game.Sheet;

namespace Game.Application.Blood;

/// <summary>Resolves the first contact with a prepared V5 object ward without modifying the intruder.</summary>
public sealed class WardActions
{
    public PowerEffectEntry EndAfterObjectDestroyed(PowerEffectEntry ward, bool tableConfirmed)
    {
        if (ward is not { Active: true } || RitualUseCatalog.Find(ward.PowerId)?.Kind != "ward" || !tableConfirmed)
            throw new InvalidOperationException(
                "Selecione uma proteção de objeto ativa e confirme sua destruição ou quebra física com a mesa.");
        var ending = WardRules.DestroyedObjectEnding(ward.Target);
        return ward with
        {
            Active = false,
            Effect = string.IsNullOrWhiteSpace(ward.Effect) ? ending : ward.Effect.TrimEnd() + " " + ending,
            Duration = "Encerrado: objeto destruído ou quebrado"
        };
    }

    public WardContactAttempt ResolveFirstContact(int level, int casterHunger, int casterPool,
        bool matchingIntruder, bool voluntaryContact, Random? random = null, bool casterBeastSubdued = false,
        string? ritualId = null, bool intruderIsCaster = false)
    {
        var requirements = RitualRules.DefaultRequirements(level);
        if (casterHunger is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(casterHunger));
        if (casterPool is < 0 or > 50) throw new ArgumentOutOfRangeException(nameof(casterPool));
        if (!matchingIntruder || !voluntaryContact || WardRules.ExcludesCaster(ritualId, intruderIsCaster))
            return new(null, WardRules.ResolveFirstContact(false, voluntaryContact, false, false), false, false);
        var originalRoll = DicePoolRules.Roll(casterPool, casterHunger, requirements.Difficulty, random);
        var messyCriticalSuppressed = casterBeastSubdued && originalRoll.Outcome == TestOutcome.MessyCritical;
        var roll = QuellBeastRules.ApplyToRoll(originalRoll, casterBeastSubdued);
        return new(roll, WardRules.ResolveFirstContact(true, true, roll.IsSuccess, roll.IsCritical),
            casterBeastSubdued, messyCriticalSuppressed);
    }

    /// <summary>Roll a confirmed retry; the caller is responsible for the intruder's Willpower payment.</summary>
    public WardRetryAttempt RollRetry(int initialDamage, int intruderPool, int intruderHunger, Random? random = null,
        string? ritualId = null, bool intruderIsCaster = false, bool intruderBeastSubdued = false)
    {
        if (WardRules.ExcludesCaster(ritualId, intruderIsCaster))
            throw new InvalidOperationException("A Proteção contra Cainitas não afeta o próprio conjurador.");
        var contact = WardRules.ResolveLaterContact(initialDamage);
        if (!contact.RequiresWillpowerToTouchAgain)
            throw new InvalidOperationException("A proteção não afetou o intruso; não há resistência para repetir o toque.");
        if (intruderPool is < 0 or > 50) throw new ArgumentOutOfRangeException(nameof(intruderPool));
        if (intruderHunger is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(intruderHunger));
        var requirements = WardRules.RetryRequirements(initialDamage == 3);
        var originalRoll = DicePoolRules.Roll(intruderPool, intruderHunger, requirements.Difficulty, random);
        var roll = QuellBeastRules.ApplyToRoll(originalRoll, intruderBeastSubdued);
        return new(roll, intruderBeastSubdued,
            originalRoll.Outcome == TestOutcome.MessyCritical && roll.Outcome == TestOutcome.CriticalSuccess);
    }

    /// <summary>Resolve a confirmed Sense the Unseen contest without revealing facts or changing either character.</summary>
    public WardDetectionAttempt Detect(int detectorPool, int detectorHunger, int casterPool, int casterHunger,
        bool tableConfirmed, Random? random = null, bool detectorBeastSubdued = false,
        bool casterBeastSubdued = false)
    {
        if (!tableConfirmed)
            throw new InvalidOperationException("Confirme que o detector está usando Sentir o Invisível contra esta proteção.");
        if (detectorPool is < 0 or > 50 || casterPool is < 0 or > 50)
            throw new ArgumentOutOfRangeException(nameof(detectorPool), "As paradas devem estar entre 0 e 50 dados.");
        if (detectorHunger is < 0 or > 5 || casterHunger is < 0 or > 5)
            throw new ArgumentOutOfRangeException(nameof(detectorHunger), "A Fome deve estar entre 0 e 5.");

        var detectorOriginal = DicePoolRules.Roll(detectorPool, detectorHunger, 0, random);
        var casterOriginal = DicePoolRules.Roll(casterPool, casterHunger, 0, random);
        var detectorRoll = QuellBeastRules.ApplyToRoll(detectorOriginal, detectorBeastSubdued);
        var casterRoll = QuellBeastRules.ApplyToRoll(casterOriginal, casterBeastSubdued);
        return new(detectorRoll, casterRoll,
            WardRules.IsDetected(detectorRoll.Successes, casterRoll.Successes),
            detectorBeastSubdued,
            detectorOriginal.Outcome == TestOutcome.MessyCritical && detectorRoll.Outcome == TestOutcome.CriticalSuccess,
            casterBeastSubdued,
            casterOriginal.Outcome == TestOutcome.MessyCritical && casterRoll.Outcome == TestOutcome.CriticalSuccess);
    }
}

/// <summary>Damage is a proposal; the caller must confirm before applying it to another sheet.</summary>
public sealed record WardContactAttempt(DicePoolResult? Roll, WardContactResult Result,
    bool CasterBeastSubdued = false, bool MessyCriticalSuppressed = false);

public sealed record WardRetryAttempt(DicePoolResult Roll, bool BeastSubduedConsidered,
    bool MessyCriticalConverted);

public sealed record WardDetectionAttempt(DicePoolResult DetectorRoll, DicePoolResult CasterRoll,
    bool Detected, bool DetectorBeastSubduedConsidered, bool DetectorMessyCriticalConverted,
    bool CasterBeastSubduedConsidered, bool CasterMessyCriticalConverted);
