using VCS.Content;

namespace VCS.Rules.V5.Rituals;

/// <summary>Core V5 object ward resolution, independent of the interface and target persistence.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base), pp. 275, 279")]
public static class WardRules
{
    /// <summary>Resolves a ward's first voluntary contact after the ritual dice have been rolled.</summary>
    public static WardContactResult ResolveFirstContact(bool matchingIntruder, bool voluntaryContact, bool ritualSucceeded, bool critical)
    {
        if (!matchingIntruder || !voluntaryContact)
            return new(false, 0, false);
        if (!ritualSucceeded)
            return new(true, 0, false);
        return new(true, critical ? 3 : 1, true);
    }

    /// <summary>After an intruder was affected, determine the test and cost for another touch.</summary>
    public static WardRetryRequirements RetryRequirements(bool firstContactWasCritical) =>
        new(1, firstContactWasCritical ? 7 : 4);

    /// <summary>Translate a stored first-contact result into the effect of a later eligible contact.</summary>
    public static WardContactResult ResolveLaterContact(int initialDamage)
    {
        if (initialDamage is not (0 or 1 or 3)) throw new ArgumentOutOfRangeException(nameof(initialDamage));
        return new(false, initialDamage, initialDamage > 0);
    }
}

/// <summary>The caller must obtain confirmation before applying damage to another character.</summary>
public sealed record WardContactResult(bool ConsumesFirstContact, int AggravatedHealthDamage, bool RequiresWillpowerToTouchAgain);

/// <summary>Willpower cost and Stamina plus Resolve difficulty for a further attempt.</summary>
public sealed record WardRetryRequirements(int WillpowerCost, int Difficulty);
