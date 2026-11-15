using VCS.Rules.V5.Dice;
using VCS.Rules.V5.Rituals;

namespace VCS.Application.Blood;

/// <summary>Resolves the first contact with a prepared V5 object ward without modifying the intruder.</summary>
public sealed class WardActions
{
    public WardContactAttempt ResolveFirstContact(int level, int casterHunger, int casterPool,
        bool matchingIntruder, bool voluntaryContact, Random? random = null)
    {
        var requirements = RitualRules.DefaultRequirements(level);
        if (casterHunger is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(casterHunger));
        if (casterPool is < 0 or > 50) throw new ArgumentOutOfRangeException(nameof(casterPool));
        if (!matchingIntruder || !voluntaryContact)
            return new(null, WardRules.ResolveFirstContact(matchingIntruder, voluntaryContact, false, false));
        var roll = DicePoolRules.Roll(casterPool, casterHunger, requirements.Difficulty, random);
        return new(roll, WardRules.ResolveFirstContact(true, true, roll.IsSuccess, roll.IsCritical));
    }
}

/// <summary>Damage is a proposal; the caller must confirm before applying it to another sheet.</summary>
public sealed record WardContactAttempt(DicePoolResult? Roll, WardContactResult Result);
