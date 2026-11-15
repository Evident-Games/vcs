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

public sealed record CompulsionActionResult(
    CompulsionResult Compulsion,
    IReadOnlyList<int> TableRolls);

public sealed record ResonanceActionResult(ResonanceResult Resolution, IReadOnlyList<int> Dice);
public sealed record MendingActionResult(RouseCheckResult RouseCheck, int SuperficialHealed, int HungerAfter);

/// <summary>
/// Coordinates blood-related V5 use cases without depending on a desktop UI.
/// The WPF layer is responsible only for collecting inputs and presenting results.
/// </summary>
public sealed class BloodActions
{
    public ResonanceResult DescribeResonance(BloodResonance resonance, ResonanceIntensity intensity, bool isHumanBlood) =>
        ResonanceRules.Describe(resonance, intensity, isHumanBlood);

    public bool ResonanceExpires(int hunger, bool fedAgain) => ResonanceRules.ResonanceBonusExpires(hunger, fedAgain);
    public bool DyscrasiaExpires(int hunger, bool fedAgain) => ResonanceRules.DyscrasiaExpires(hunger, fedAgain);

    public ResonanceActionResult RollResonance(BloodSource source, bool isHumanBlood,
        bool isExceptionalAnimalBlood = false, Random? random = null)
    {
        if (source == BloodSource.Bagged) throw new InvalidOperationException("Sangue ensacado não concede Ressonância.");
        if (source == BloodSource.Animal && !isHumanBlood)
        {
            random ??= Random.Shared;
            var first = random.Next(1, 11);
            var second = first >= 9 ? random.Next(1, 11) : (int?)null;
            var intensity = ResonanceRules.FromTemperamentDice(first, second);
            int[] animalDice = second is { } secondDie ? [first, secondDie] : [first];
            return new(ResonanceRules.Describe(BloodResonance.Animal, intensity, false, isExceptionalAnimalBlood), animalDice);
        }

        random ??= Random.Shared;
        var resonanceDie = random.Next(1, 11);
        var temperamentDie = random.Next(1, 11);
        var acuteDie = temperamentDie >= 9 ? random.Next(1, 11) : (int?)null;
        var resultingIntensity = ResonanceRules.FromTemperamentDice(temperamentDie, acuteDie);
        int[] rolls = acuteDie is { } acute ? [resonanceDie, temperamentDie, acute] : [resonanceDie, temperamentDie];
        return new(ResonanceRules.Describe(ResonanceRules.FromRandomResonanceDie(resonanceDie), resultingIntensity,
            isHumanBlood, isExceptionalAnimalBlood), rolls);
    }

    public CompulsionActionResult ResolveCompulsion(TestOutcome outcome, bool isCaitiffOrThinBlood, Random? random = null)
    {
        if (!CompulsionRules.IsTriggered(outcome))
            throw new ArgumentException("O resultado não desencadeia uma Compulsão.", nameof(outcome));
        if (outcome == TestOutcome.MessyCritical)
            return new(CompulsionRules.FromMessyCritical(), []);

        random ??= Random.Shared;
        var rolls = new List<int>();
        CompulsionResult result;
        do
        {
            var roll = random.Next(1, 11);
            rolls.Add(roll);
            result = CompulsionRules.FromBestialFailureRoll(roll, isCaitiffOrThinBlood);
        } while (result.MustRerollForCaitiffOrThinBlood);
        return new(result, rolls);
    }

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

    public MendingActionResult MendSuperficialHealth(int superficialDamage, int hunger, int bloodPotency, Random? random = null)
    {
        if (superficialDamage <= 0) throw new InvalidOperationException("Não há dano superficial de Saúde para recuperar.");
        if (bloodPotency is < 0 or > 10) throw new ArgumentOutOfRangeException(nameof(bloodPotency));
        var rouse = RouseCheckRules.Roll(hunger, random);
        var healed = Math.Min(superficialDamage, BloodPotencyEffects.At(bloodPotency).MendSuperficial);
        return new(rouse, healed, rouse.HungerAfter);
    }
}
