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
}

public sealed record RitualPreparation(RitualRequirements Requirements, RouseCheckResult Check, DicePoolResult? Roll);
