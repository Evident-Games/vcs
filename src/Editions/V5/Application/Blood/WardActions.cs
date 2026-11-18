using Game.Rules.V5.Dice;
using Game.Rules.V5.Rituals;
using Game.Rules.V5.Disciplines;

namespace Game.Application.Blood;

/// <summary>Resolves the first contact with a prepared V5 object ward without modifying the intruder.</summary>
public sealed class WardActions
{
    public WardContactAttempt ResolveFirstContact(int level, int casterHunger, int casterPool,
        bool matchingIntruder, bool voluntaryContact, Random? random = null, bool casterBeastSubdued = false,
        string? ritualId = null, bool intruderIsCaster = false)
    {
        var requirements = RitualRules.DefaultRequirements(level);
        if (casterHunger is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(casterHunger));
        if (casterPool is < 0 or > 50) throw new ArgumentOutOfRangeException(nameof(casterPool));
        if (!matchingIntruder || !voluntaryContact || WardRules.ExcludesCaster(ritualId, intruderIsCaster))
            return new(null, WardRules.ResolveFirstContact(false, voluntaryContact, false, false));
        var roll = QuellBeastRules.ApplyToRoll(DicePoolRules.Roll(casterPool, casterHunger, requirements.Difficulty, random), casterBeastSubdued);
        return new(roll, WardRules.ResolveFirstContact(true, true, roll.IsSuccess, roll.IsCritical));
    }

    /// <summary>Roll a confirmed retry; the caller is responsible for the intruder's Willpower payment.</summary>
    public DicePoolResult RollRetry(int initialDamage, int intruderPool, int intruderHunger, Random? random = null,
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
        return QuellBeastRules.ApplyToRoll(
            DicePoolRules.Roll(intruderPool, intruderHunger, requirements.Difficulty, random), intruderBeastSubdued);
    }
}

/// <summary>Damage is a proposal; the caller must confirm before applying it to another sheet.</summary>
public sealed record WardContactAttempt(DicePoolResult? Roll, WardContactResult Result);
