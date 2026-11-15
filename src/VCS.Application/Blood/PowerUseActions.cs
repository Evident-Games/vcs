using VCS.Content.Disciplines;
using VCS.Rules.V5.Dice;
using VCS.Rules.V5.Disciplines;

namespace VCS.Application.Blood;

public sealed record PowerUseRequest(PowerUseDefinition Definition, int PowerLevel, int Pool, bool RollRequired,
    int Difficulty, int OpponentSuccesses, int Rating, int Wits, bool VampireTarget,
    int Hunger, int BloodPotency, int RouseChecks, int WillpowerCost,
    int WillpowerSuperficial, int WillpowerAggravated, int WillpowerCapacity);

public sealed record PowerUseResult(PowerActivationResult Costs, DicePoolResult? Roll, PowerResolution Resolution);

/// <summary>Resolves one use without mutating a sheet; the caller commits the result once.</summary>
public sealed class PowerUseActions
{
    public PowerUseResult Resolve(PowerUseRequest request, Random? random = null)
    {
        if (request.Pool is < 0 or > 50 || request.Difficulty is < 0 or > 50 ||
            request.OpponentSuccesses is < 0 or > 100 || request.Rating is < 0 or > 5 || request.Wits is < 0 or > 5)
            throw new ArgumentOutOfRangeException(nameof(request));
        random ??= Random.Shared;
        var costs = new PowerActivationActions().Activate(request.Hunger, request.BloodPotency, request.PowerLevel,
            request.RouseChecks, request.WillpowerCost, request.WillpowerSuperficial,
            request.WillpowerAggravated, request.WillpowerCapacity, random);
        var rollDifficulty = request.Definition.Mode == "opposed" ? request.OpponentSuccesses + 1 : request.Difficulty;
        var roll = request.RollRequired ? DicePoolRules.Roll(request.Pool, costs.Hunger, rollDifficulty, random) : null;
        var outcome = PowerResolutionRules.Resolve(request.Definition, new(request.Rating, roll?.Successes ?? 0,
            request.Difficulty, request.OpponentSuccesses, request.RollRequired, roll?.IsCritical ?? false,
            request.VampireTarget, request.RouseChecks, request.Wits, request.BloodPotency));
        return new(costs, roll, outcome);
    }
}
