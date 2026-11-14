using VCS.Rules.V5.Blood;
using VCS.Rules.V5.Dice;

namespace VCS.Application.Blood;

public enum FeedingApplicationStatus
{
    Applied,
    NoHungerSlaked,
    FractionalHunger
}

public sealed record FeedingActionResult(
    FeedingResult Resolution,
    int HungerAfter,
    FeedingApplicationStatus Status);

public sealed record DiceTestActionResult(
    RouseCheckResult? BloodSurgeCheck,
    int BloodSurgeDice,
    int HungerAfter,
    DicePoolResult Roll);

public sealed record FrenzyActionResult(
    FrenzyTriggerResult Trigger,
    FrenzyResistanceResult? Resistance);

/// <summary>
/// Coordinates blood-related V5 use cases without depending on a desktop UI.
/// The WPF layer is responsible only for collecting inputs and presenting results.
/// </summary>
public sealed class BloodActions
{
    public string BloodSurge(int bloodPotency) => BloodPotencyEffects.BloodSurge(bloodPotency);
    public string DamageMend(int bloodPotency) => BloodPotencyEffects.DamageMend(bloodPotency);
    public string PowerBonus(int bloodPotency) => BloodPotencyEffects.PowerBonus(bloodPotency);
    public string RouseReroll(int bloodPotency) => BloodPotencyEffects.RouseReroll(bloodPotency);
    public string FeedingPenalty(int bloodPotency) => BloodPotencyEffects.FeedingPenalty(bloodPotency);
    public string BaneSeverity(int bloodPotency) => BloodPotencyEffects.BaneSeverity(bloodPotency);

    public FeedingResult PreviewFeeding(
        BloodSource source,
        int bloodPotency,
        int hunger,
        bool humanWasDrainedToDeath = false,
        bool animalIsLarge = false,
        bool animalIsExceptional = false) =>
        FeedingRules.Resolve(source, bloodPotency, hunger, hunger,
            humanWasDrainedToDeath, animalIsLarge, animalIsExceptional);

    public FeedingActionResult ApplyFeeding(
        BloodSource source,
        int bloodPotency,
        int hunger,
        bool humanWasDrainedToDeath = false,
        bool animalIsLarge = false,
        bool animalIsExceptional = false)
    {
        var resolution = PreviewFeeding(source, bloodPotency, hunger,
            humanWasDrainedToDeath, animalIsLarge, animalIsExceptional);
        if (resolution.HungerSlaked <= 0)
            return new(resolution, hunger, FeedingApplicationStatus.NoHungerSlaked);
        if (decimal.Truncate(resolution.HungerSlaked) != resolution.HungerSlaked)
            return new(resolution, hunger, FeedingApplicationStatus.FractionalHunger);

        return new(resolution, (int)resolution.HungerAfter, FeedingApplicationStatus.Applied);
    }

    public FrenzyTriggerResult EvaluateFrenzyTrigger(
        FrenzyTrigger trigger,
        int hunger,
        bool isPresent = true,
        bool isPillarOrLover = false) =>
        FrenzyTriggerRules.Evaluate(trigger, hunger, isPresent, isPillarOrLover);

    public FrenzyActionResult ResistFrenzy(
        FrenzyTrigger trigger,
        int hunger,
        int willpowerPool,
        int humanity,
        bool isPresent = true,
        bool isPillarOrLover = false,
        Random? random = null)
    {
        var evaluation = EvaluateFrenzyTrigger(trigger, hunger, isPresent, isPillarOrLover);
        var resistance = evaluation.Applies
            ? FrenzyRules.RollResistance(evaluation.Type, evaluation.Description, willpowerPool,
                evaluation.Difficulty, humanity, random)
            : null;
        return new(evaluation, resistance);
    }

    public DiceTestActionResult RollTest(
        int dicePool,
        int difficulty,
        int hunger,
        int bloodPotency,
        bool useHungerDice,
        bool useBloodSurge,
        Random? random = null)
    {
        var hungerAfter = hunger;
        var surgeDice = 0;
        RouseCheckResult? surgeCheck = null;
        if (useBloodSurge)
        {
            surgeDice = BloodPotencyEffects.At(bloodPotency).BloodSurgeDice;
            surgeCheck = RouseCheckRules.Roll(hunger, random);
            hungerAfter = surgeCheck.HungerAfter;
        }

        var roll = DicePoolRules.Roll(dicePool + surgeDice,
            useHungerDice ? hungerAfter : 0, difficulty, random);
        return new(surgeCheck, surgeDice, hungerAfter, roll);
    }

    public RouseCheckResult MakeRouseCheck(int hunger, Random? random = null) =>
        RouseCheckRules.Roll(hunger, random);
}
