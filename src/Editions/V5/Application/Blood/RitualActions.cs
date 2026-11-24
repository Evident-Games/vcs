using Game.Rules.V5.Dice;
using Game.Rules.V5.Rituals;
using Game.Rules.V5.Disciplines;
using Game.Content.Disciplines;

namespace Game.Application.Blood;

/// <summary>V5 ritual preparation. Some rituals defer their roll until a trigger occurs.</summary>
public sealed class RitualActions
{
    public RitualPreparation Prepare(int level, int hunger, int pool, bool deferred, Random? random = null,
        bool beastSubdued = false, string? ritualId = null,
        bool cainiteWardFrenzyConditionConfirmed = false, bool exceptionApproved = false,
        bool wardObjectSizeConfirmed = true)
    {
        if (ritualId is not null && DisciplineCatalog.UnverifiedPowerIds.Contains(ritualId))
            throw new InvalidOperationException("A fonte deste ritual não foi verificada no livro base fornecido. Resolva manualmente com o Narrador; nenhuma checagem foi feita.");
        if (ritualId is not null && RitualUseCatalog.Find(ritualId) is { Kind: not ("ward" or "awakening" or "sanctuary") })
            throw new InvalidOperationException("Use o fluxo específico deste ritual para pagar custos e resolver seu gatilho; nenhuma checagem foi feita.");
        if (RitualRules.RequiresManualPreparation(ritualId))
        {
            var sanctuary = TrueSanctuaryRules.Preparation();
            throw new InvalidOperationException($"Fuga para o Verdadeiro Santuário requer {sanctuary.CircleCount} círculos, cada um preparado em {sanctuary.ConsecutiveNightsPerCircle} noites consecutivas, {sanctuary.MinutesPerCirclePerNight} minutos e {sanctuary.RouseChecksPerCirclePerNight} Checagens de Sangue por noite; {sanctuary.TotalRouseChecks} checagens no total. A rolagem ocorre em cada tentativa de viagem. Resolva e registre o processo com o Narrador. Nenhuma checagem foi feita. Fonte: Livro base, p. 281.");
        }
        WardRules.ValidatePreparation(ritualId, cainiteWardFrenzyConditionConfirmed, exceptionApproved,
            wardObjectSizeConfirmed);
        var requirements = RitualRules.DefaultRequirements(level);
        if (hunger is < 0 or >= 5) throw new InvalidOperationException("Fome 5 impede despertar o Sangue voluntariamente.");
        if (pool is < 0 or > 50) throw new ArgumentOutOfRangeException(nameof(pool));
        random ??= Random.Shared;
        var check = RouseCheckRules.Roll(hunger, random);
        var originalRoll = deferred ? null : DicePoolRules.Roll(pool, check.HungerAfter, requirements.Difficulty, random);
        var roll = originalRoll is null ? null : QuellBeastRules.ApplyToRoll(originalRoll, beastSubdued);
        return new(requirements, check, roll, beastSubdued && roll is not null,
            originalRoll?.Outcome == TestOutcome.MessyCritical && roll?.Outcome == TestOutcome.CriticalSuccess);
    }

    /// <summary>Roll a previously paid ritual at its confirmed trigger without another Rouse check.</summary>
    public DicePoolResult ResolveDeferred(int level, int hunger, int pool, Random? random = null, bool beastSubdued = false)
        => ResolveDeferredWithAudit(level, hunger, pool, random, beastSubdued).Roll;

    private static RitualRollResolution ResolveDeferredWithAudit(int level, int hunger, int pool, Random? random,
        bool beastSubdued)
    {
        var requirements = RitualRules.DefaultRequirements(level);
        if (hunger is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(hunger));
        if (pool is < 0 or > 50) throw new ArgumentOutOfRangeException(nameof(pool));
        var originalRoll = DicePoolRules.Roll(pool, hunger, requirements.Difficulty, random);
        var roll = QuellBeastRules.ApplyToRoll(originalRoll, beastSubdued);
        return new(roll, beastSubdued,
            originalRoll.Outcome == TestOutcome.MessyCritical && roll.Outcome == TestOutcome.CriticalSuccess);
    }

    /// <summary>Resolve the awakening ritual after the Storyteller confirms danger.</summary>
    public AwakeningResult ResolveAwakening(int hunger, int pool, Random? random = null, bool beastSubdued = false)
    {
        var resolution = ResolveDeferredWithAudit(1, hunger, pool, random, beastSubdued);
        return new(resolution.Roll, AwakeningRitualRules.Describe(resolution.Roll.IsSuccess, resolution.Roll.IsCritical),
            resolution.BeastSubduedConsidered, resolution.MessyCriticalConverted);
    }
}

public sealed record RitualPreparation(RitualRequirements Requirements, RouseCheckResult Check, DicePoolResult? Roll,
    bool BeastSubduedConsidered, bool MessyCriticalConverted);

public sealed record AwakeningResult(DicePoolResult Roll, string Effect, bool BeastSubduedConsidered,
    bool MessyCriticalConverted);

internal sealed record RitualRollResolution(DicePoolResult Roll, bool BeastSubduedConsidered,
    bool MessyCriticalConverted);
