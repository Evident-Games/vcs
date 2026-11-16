using VCS.Rules.V5.Dice;
using VCS.Rules.V5.Rituals;
using VCS.Rules.V5.Disciplines;

namespace VCS.Application.Blood;

/// <summary>V5 ritual preparation. Some rituals defer their roll until a trigger occurs.</summary>
public sealed class RitualActions
{
    public RitualPreparation Prepare(int level, int hunger, int pool, bool deferred, Random? random = null,
        bool beastSubdued = false, string? ritualId = null)
    {
        if (RitualRules.RequiresManualPreparation(ritualId))
            throw new InvalidOperationException("Fuga para o Verdadeiro Santuário requer preparação de dois círculos em três noites e 12 Checagens de Sangue no total; a rolagem ocorre em cada tentativa de viagem. Resolva e registre o processo com o Narrador. Nenhuma checagem foi feita. Fonte: Livro base, p. 281.");
        var requirements = RitualRules.DefaultRequirements(level);
        if (hunger is < 0 or >= 5) throw new InvalidOperationException("Fome 5 impede despertar o Sangue voluntariamente.");
        if (pool is < 0 or > 50) throw new ArgumentOutOfRangeException(nameof(pool));
        random ??= Random.Shared;
        var check = RouseCheckRules.Roll(hunger, random);
        var roll = deferred ? null : DicePoolRules.Roll(pool, check.HungerAfter, requirements.Difficulty, random);
        if (roll is not null) roll = QuellBeastRules.ApplyToRoll(roll, beastSubdued);
        return new(requirements, check, roll);
    }

    /// <summary>Roll a previously paid ritual at its confirmed trigger without another Rouse check.</summary>
    public DicePoolResult ResolveDeferred(int level, int hunger, int pool, Random? random = null, bool beastSubdued = false)
    {
        var requirements = RitualRules.DefaultRequirements(level);
        if (hunger is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(hunger));
        if (pool is < 0 or > 50) throw new ArgumentOutOfRangeException(nameof(pool));
        return QuellBeastRules.ApplyToRoll(DicePoolRules.Roll(pool, hunger, requirements.Difficulty, random), beastSubdued);
    }

    /// <summary>Resolve the awakening ritual after the Storyteller confirms danger.</summary>
    public AwakeningResult ResolveAwakening(int hunger, int pool, Random? random = null, bool beastSubdued = false)
    {
        var roll = ResolveDeferred(1, hunger, pool, random, beastSubdued);
        return new(roll, AwakeningRitualRules.Describe(roll.IsSuccess, roll.IsCritical));
    }
}

public sealed record RitualPreparation(RitualRequirements Requirements, RouseCheckResult Check, DicePoolResult? Roll);

public sealed record AwakeningResult(DicePoolResult Roll, string Effect);
