using VCS.Rules.V5.Dice;

namespace VCS.Application.Blood;

public sealed record PowerActivationResult(int Hunger, int WillpowerSuperficial, IReadOnlyList<RouseCheckResult> Checks);

/// <summary>Applies explicit activation costs, including the Blood Potency discipline reroll.</summary>
public sealed class PowerActivationActions
{
    public PowerActivationResult Activate(int hunger, int bloodPotency, int powerLevel, int rouseChecks,
        int willpowerCost, int willpowerSuperficial, int willpowerAggravated, int willpowerCapacity, Random? random = null)
    {
        if (rouseChecks is < 0 or > 10 || willpowerCost is < 0 or > 10 || powerLevel is < 1 or > 5 || bloodPotency is < 0 or > 10)
            throw new ArgumentOutOfRangeException(nameof(rouseChecks));
        if (hunger is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(hunger));
        if (hunger == 5 && rouseChecks > 0) throw new InvalidOperationException("Fome 5: não é possível despertar o Sangue voluntariamente.");
        if (willpowerCapacity < 0 || willpowerSuperficial < 0 || willpowerAggravated < 0 ||
            (long)willpowerCost + willpowerSuperficial + willpowerAggravated > willpowerCapacity)
            throw new InvalidOperationException("Vontade disponível insuficiente para pagar o custo.");
        random ??= Random.Shared;
        var checks = new List<RouseCheckResult>();
        for (var i = 0; i < rouseChecks; i++)
        {
            var first = random.Next(1, 11);
            int? reroll = first < 6 && RouseCheckRules.CanRerollForDisciplinePower(bloodPotency, powerLevel)
                ? random.Next(1, 11) : null;
            var check = RouseCheckRules.ResolveForDisciplinePower(hunger, bloodPotency, powerLevel, first, reroll);
            checks.Add(check);
            hunger = check.HungerAfter;
        }
        return new(hunger, willpowerSuperficial + willpowerCost, checks);
    }
}
