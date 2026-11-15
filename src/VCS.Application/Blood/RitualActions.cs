using VCS.Rules.V5.Dice;
using VCS.Rules.V5.Rituals;

namespace VCS.Application.Blood;

/// <summary>V5 ritual preparation. Some rituals defer their roll until a trigger occurs.</summary>
public sealed class RitualActions
{
    public RitualPreparation Prepare(int level, int hunger, int pool, bool deferred, Random? random = null)
    {
        var requirements = RitualRules.DefaultRequirements(level);
        if (hunger is < 0 or >= 5) throw new InvalidOperationException("Fome 5 impede despertar o Sangue voluntariamente.");
        if (pool is < 0 or > 50) throw new ArgumentOutOfRangeException(nameof(pool));
        random ??= Random.Shared;
        var check = RouseCheckRules.Roll(hunger, random);
        var roll = deferred ? null : DicePoolRules.Roll(pool, check.HungerAfter, requirements.Difficulty, random);
        return new(requirements, check, roll);
    }

    /// <summary>Roll a previously paid ritual at its confirmed trigger without another Rouse check.</summary>
    public DicePoolResult ResolveDeferred(int level, int hunger, int pool, Random? random = null)
    {
        var requirements = RitualRules.DefaultRequirements(level);
        if (hunger is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(hunger));
        if (pool is < 0 or > 50) throw new ArgumentOutOfRangeException(nameof(pool));
        return DicePoolRules.Roll(pool, hunger, requirements.Difficulty, random);
    }

    /// <summary>Resolve the awakening ritual after the Storyteller confirms danger.</summary>
    public AwakeningResult ResolveAwakening(int hunger, int pool, Random? random = null)
    {
        var roll = ResolveDeferred(1, hunger, pool, random);
        return new(roll, AwakeningRitualRules.Describe(roll.IsSuccess, roll.IsCritical));
    }
}

public sealed record RitualPreparation(RitualRequirements Requirements, RouseCheckResult Check, DicePoolResult? Roll);

public sealed record AwakeningResult(DicePoolResult Roll, string Effect);
