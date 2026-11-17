using VCS.Content.Disciplines;

namespace VCS.Rules.Disciplines;

/// <summary>Common result shape; each game edition supplies its own interpretation.</summary>
public sealed record PowerResolution(bool Succeeded, string Effect, string Duration, int Margin = 0,
    int HealthDamage = 0, bool Aggravated = false, int WillpowerDamage = 0, int HungerIncrease = 0,
    int HealthBonus = 0, int PoolBonus = 0, int PotencyIncrease = 0, int Turns = 0);

/// <summary>Edition-neutral inputs needed to resolve a known discipline power.</summary>
public sealed record PowerResolutionContext(int Rating, int Successes, int Difficulty, int OpponentSuccesses,
    bool Rolled, bool Critical, bool VampireTarget, int RouseChecks, int Wits, int BloodPotency);

/// <summary>A plug-in point for the rule implementation of one Vampire edition.</summary>
public interface IPowerRuleset
{
    string EditionId { get; }
    PowerResolution Resolve(PowerUseDefinition use, PowerResolutionContext context);
}

/// <summary>Selects an edition implementation without making the sheet depend on its mechanics.</summary>
public sealed class PowerRulesetRegistry
{
    private readonly IReadOnlyDictionary<string, IPowerRuleset> _rulesets;
    public PowerRulesetRegistry(IEnumerable<IPowerRuleset> rulesets) =>
        _rulesets = rulesets.ToDictionary(x => x.EditionId, StringComparer.OrdinalIgnoreCase);

    public IPowerRuleset ForEdition(string editionId) => _rulesets.TryGetValue(editionId, out var ruleset)
        ? ruleset
        : throw new NotSupportedException($"A edição '{editionId}' ainda não possui um módulo de regras instalado.");
}
