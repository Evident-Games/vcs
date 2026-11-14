using VCS.Rules.V5.Dice;

namespace VCS.Rules.V5.Blood;

public enum FrenzyType
{
    Hunger,
    Fury,
    Terror
}

public sealed record FrenzyResistanceResult(
    FrenzyType Type,
    string Trigger,
    int Difficulty,
    DicePoolResult Roll,
    bool Resisted)
{
    public bool EnteredFrenzy => !Resisted;
}

public static class FrenzyRules
{
    /// <summary>Frenzy resistance uses a Willpower pool and never adds Hunger dice.</summary>
    public static FrenzyResistanceResult ResolveResistance(
        FrenzyType type, string trigger, int willpowerPool, int difficulty, IReadOnlyList<int> faces, int humanity = 0)
    {
        if (string.IsNullOrWhiteSpace(trigger)) throw new ArgumentException("Informe o gatilho do Frenesi.", nameof(trigger));
        if (willpowerPool < 0) throw new ArgumentOutOfRangeException(nameof(willpowerPool));
        if (difficulty is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(difficulty));
        if (humanity is < 0 or > 10) throw new ArgumentOutOfRangeException(nameof(humanity));
        var resistancePool = willpowerPool + humanity / 3;
        var roll = DicePoolRules.Resolve(resistancePool, 0, difficulty, faces);
        return new(type, trigger.Trim(), difficulty, roll, roll.IsSuccess);
    }

    public static int FuryFrenzyTerminationDifficulty(bool otherEnemiesStillStand) =>
        otherEnemiesStillStand ? 5 : 3;

    public static FrenzyResistanceResult ResolveFuryTermination(
        string trigger, int willpowerPool, int humanity, bool otherEnemiesStillStand, IReadOnlyList<int> faces) =>
        ResolveResistance(FrenzyType.Fury, trigger, willpowerPool,
            FuryFrenzyTerminationDifficulty(otherEnemiesStillStand), faces, humanity);

    public static bool HungerFrenzyEndsAt(int hunger) => hunger is >= 0 and <= 1;

    public static bool TerrorFrenzyEndsWhenOutOfSight(bool escapedTheTrigger) => escapedTheTrigger;
}

public enum CompulsionType
{
    Hunger,
    Dominance,
    Harm,
    Paranoia,
    Clan
}

public sealed record CompulsionResult(CompulsionType Type, int? TableRoll, bool MustRerollForCaitiffOrThinBlood);

public static class CompulsionRules
{
    /// <summary>Messy criticals trigger the character's clan compulsion; a bestial failure may use this d10 table.</summary>
    public static CompulsionResult FromBestialFailureRoll(int tableRoll, bool isCaitiffOrThinBlood)
    {
        if (tableRoll is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(tableRoll));
        if (isCaitiffOrThinBlood && tableRoll == 10)
            return new(CompulsionType.Clan, tableRoll, MustRerollForCaitiffOrThinBlood: true);

        var type = tableRoll switch
        {
            <= 3 => CompulsionType.Hunger,
            <= 5 => CompulsionType.Dominance,
            <= 7 => CompulsionType.Harm,
            <= 9 => CompulsionType.Paranoia,
            _ => CompulsionType.Clan
        };
        return new(type, tableRoll, false);
    }

    public static CompulsionResult FromMessyCritical() =>
        new(CompulsionType.Clan, null, false);

    public static bool IsTriggered(TestOutcome outcome) =>
        outcome is TestOutcome.MessyCritical or TestOutcome.BestialFailure;
}
