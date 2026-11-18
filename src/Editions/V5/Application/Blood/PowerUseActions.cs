using Game.Content.Disciplines;
using Game.Rules.V5.Dice;
using Game.Rules.V5.Disciplines;
using Game.Rules.Disciplines;

namespace Game.Application.Blood;

public sealed record PowerUseRequest(PowerUseDefinition Definition, int PowerLevel, int Pool, bool RollRequired,
    int Difficulty, int OpponentSuccesses, int Rating, int Wits, bool VampireTarget,
    int Hunger, int BloodPotency, int RouseChecks, int WillpowerCost,
    int WillpowerSuperficial, int WillpowerAggravated, int WillpowerCapacity, string EditionId = "V5",
    bool BeastSubdued = false);

public sealed record PowerUseResult(PowerActivationResult Costs, DicePoolResult? Roll, PowerResolution Resolution);

/// <summary>Resolves one use without mutating a sheet; the caller commits the result once.</summary>
public sealed class PowerUseActions
{
    private readonly PowerRulesetRegistry _rulesets;

    public PowerUseActions(PowerRulesetRegistry rulesets) => _rulesets = rulesets;

    public PowerUseResult Resolve(PowerUseRequest request, Random? random = null)
    {
        // This workflow includes V5-specific Hunger, Rouse checks and dice. Other editions
        // need their own activation workflow before their effect rules can be used here.
        if (!string.Equals(request.EditionId, "V5", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException($"A ativação de poderes da edição '{request.EditionId}' ainda não está implementada.");
        var ruleset = _rulesets.ForEdition(request.EditionId);
        if (request.Pool is < 0 or > 50 || request.Difficulty is < 0 or > 50 ||
            request.OpponentSuccesses is < 0 or > 100 || request.Rating is < 0 or > 5 || request.Wits is < 0 or > 5)
            throw new ArgumentOutOfRangeException(nameof(request));
        random ??= Random.Shared;
        var costs = new PowerActivationActions().Activate(request.Hunger, request.BloodPotency, request.PowerLevel,
            request.RouseChecks, request.WillpowerCost, request.WillpowerSuperficial,
            request.WillpowerAggravated, request.WillpowerCapacity, random);
        var rollDifficulty = request.Definition.Mode == "opposed" ? request.OpponentSuccesses + 1 : request.Difficulty;
        var roll = request.RollRequired ? DicePoolRules.Roll(request.Pool, costs.Hunger, rollDifficulty, random) : null;
        if (roll is not null) roll = QuellBeastRules.ApplyToRoll(roll, request.BeastSubdued);
        var outcome = ruleset.Resolve(request.Definition, new(request.Rating, roll?.Successes ?? 0,
            request.Difficulty, request.OpponentSuccesses, request.RollRequired, roll?.IsCritical ?? false,
            request.VampireTarget, request.RouseChecks, request.Wits, request.BloodPotency));
        return new(costs, roll, outcome);
    }
}
