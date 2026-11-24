using Game.Content;
using Game.Rules.V5.Dice;

namespace Game.Rules.V5.Rituals;

/// <summary>Core V5 object ward resolution, independent of the interface and target persistence.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base), pp. 275, 279–280")]
public static class WardRules
{
    public const string CainiteWardId = "ritual-ward-against-cainites";

    /// <summary>A failed terror frenzy test prevents inscription of the Cainite ward for that night.</summary>
    public static void ValidatePreparation(string? ritualId, bool nightlyFrenzyConditionConfirmed,
        bool exceptionApproved)
    {
        if (ritualId == CainiteWardId && !nightlyFrenzyConditionConfirmed && !exceptionApproved)
            throw new InvalidOperationException(
                "Proteção contra Cainitas exige confirmar que o conjurador não falhou em um teste de frenesi de terror nesta noite.");
    }

    /// <summary>The cainite object ward does not affect its caster; identity is confirmed by the table.</summary>
    public static bool ExcludesCaster(string? ritualId, bool intruderIsCaster) =>
        intruderIsCaster && ritualId == CainiteWardId;

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

    /// <summary>The character using Sense the Unseen is acting and therefore wins a tied contest.</summary>
    public static bool IsDetected(int detectorSuccesses, int casterSuccesses)
    {
        if (detectorSuccesses < 0) throw new ArgumentOutOfRangeException(nameof(detectorSuccesses));
        if (casterSuccesses < 0) throw new ArgumentOutOfRangeException(nameof(casterSuccesses));
        return ContestRules.ActingCharacterWins(detectorSuccesses, casterSuccesses);
    }

    /// <summary>Translate a stored first-contact result into the effect of a later eligible contact.</summary>
    public static WardContactResult ResolveLaterContact(int initialDamage)
    {
        if (initialDamage is not (0 or 1 or 3)) throw new ArgumentOutOfRangeException(nameof(initialDamage));
        return new(false, initialDamage, initialDamage > 0);
    }

    public static string DestroyedObjectEnding(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
            throw new ArgumentException("Identifique o objeto protegido.", nameof(target));
        return $"Encerramento confirmado: {target.Trim()} foi fisicamente destruído ou quebrado; a proteção terminou.";
    }
}

/// <summary>The caller must obtain confirmation before applying damage to another character.</summary>
public sealed record WardContactResult(bool ConsumesFirstContact, int AggravatedHealthDamage, bool RequiresWillpowerToTouchAgain);

/// <summary>Willpower cost and Stamina plus Resolve difficulty for a further attempt.</summary>
public sealed record WardRetryRequirements(int WillpowerCost, int Difficulty);
