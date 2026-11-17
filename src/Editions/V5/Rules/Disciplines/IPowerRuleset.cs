using VCS.Content.Disciplines;

namespace VCS.Rules.Disciplines;

/// <summary>Result shape for the current V5 power workflow.</summary>
public sealed record PowerResolution(bool Succeeded, string Effect, string Duration, int Margin = 0,
    int HealthDamage = 0, bool Aggravated = false, int WillpowerDamage = 0, int HungerIncrease = 0,
    int HealthBonus = 0, int PoolBonus = 0, int PotencyIncrease = 0, int Turns = 0);

/// <summary>V5 inputs needed to resolve a known discipline power.</summary>
public sealed record PowerResolutionContext(int Rating, int Successes, int Difficulty, int OpponentSuccesses,
    bool Rolled, bool Critical, bool VampireTarget, int RouseChecks, int Wits, int BloodPotency);

/// <summary>Power rules contract used by the V5 workflow; not yet edition-neutral.</summary>
public interface IPowerRuleset
{
    string EditionId { get; }
    PowerResolution Resolve(PowerUseDefinition use, PowerResolutionContext context);
}

/// <summary>Selects an implementation for the existing power workflow.</summary>
public sealed class PowerRulesetRegistry
{
    private readonly IReadOnlyDictionary<string, IPowerRuleset> _rulesets;
    public PowerRulesetRegistry(IEnumerable<IPowerRuleset> rulesets) =>
        _rulesets = rulesets.ToDictionary(x => x.EditionId, StringComparer.OrdinalIgnoreCase);

    public IPowerRuleset ForEdition(string editionId) => _rulesets.TryGetValue(editionId, out var ruleset)
        ? ruleset
        : throw new NotSupportedException($"A edição '{editionId}' ainda não possui um módulo de regras instalado.");
}
