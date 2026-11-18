using Game.Rules.V5.Dice;
using Game.Content;

namespace Game.Rules.V5.Conflicts;

public enum ConflictKind
{
    Physical,
    Social,
    Mental
}

public enum ConflictDamageType
{
    Superficial,
    Aggravated
}

public sealed record ConflictDamage(int ToFirst, int ToSecond, ConflictDamageType Type);
public sealed record ConflictRoll(DicePoolResult Dice, int DamageModifier = 0);
public sealed record ConflictOutcome(ConflictExchange Exchange, TestOutcome FirstOutcome, TestOutcome SecondOutcome);

public sealed record ConflictExchange(
    ConflictKind Kind,
    int FirstSuccesses,
    int SecondSuccesses,
    int Margin,
    bool IsTie,
    string? Winner,
    ConflictDamage Damage);

[RuleSource("Vampiro: A Máscara V5 (Livro base)")]
public static class ConflictRules
{
    /// <summary>
    /// Resolves one opposed V5 conflict turn. A tie makes both sides inflict damage at margin 1;
    /// otherwise the higher success total inflicts its margin plus applicable damage modifier.
    /// </summary>
    public static ConflictExchange ResolveOpposed(
        ConflictKind kind,
        int firstSuccesses,
        int secondSuccesses,
        int firstDamageModifier = 0,
        int secondDamageModifier = 0,
        ConflictDamageType damageType = ConflictDamageType.Superficial,
        bool firstTargetIsVampire = false,
        bool secondTargetIsVampire = false)
    {
        if (firstSuccesses < 0) throw new ArgumentOutOfRangeException(nameof(firstSuccesses));
        if (secondSuccesses < 0) throw new ArgumentOutOfRangeException(nameof(secondSuccesses));

        if (firstSuccesses == secondSuccesses)
        {
            var firstDamage = Math.Max(0, 1 + secondDamageModifier);
            var secondDamage = Math.Max(0, 1 + firstDamageModifier);
            if (kind == ConflictKind.Physical)
            {
                firstDamage = DamageAfterVampireSuperficialReduction(firstDamage, damageType, firstTargetIsVampire);
                secondDamage = DamageAfterVampireSuperficialReduction(secondDamage, damageType, secondTargetIsVampire);
            }
            return new(kind, firstSuccesses, secondSuccesses, 1, true, null, new(firstDamage, secondDamage, damageType));
        }

        var firstWins = firstSuccesses > secondSuccesses;
        var margin = Math.Abs(firstSuccesses - secondSuccesses);
        var winnerDamage = Math.Max(0, margin + (firstWins ? firstDamageModifier : secondDamageModifier));
        var firstDamageResult = firstWins ? 0 : winnerDamage;
        var secondDamageResult = firstWins ? winnerDamage : 0;
        if (kind == ConflictKind.Physical)
        {
            firstDamageResult = DamageAfterVampireSuperficialReduction(firstDamageResult, damageType, firstTargetIsVampire);
            secondDamageResult = DamageAfterVampireSuperficialReduction(secondDamageResult, damageType, secondTargetIsVampire);
        }
        return new(kind, firstSuccesses, secondSuccesses, margin, false, firstWins ? "first" : "second",
            new(firstDamageResult, secondDamageResult, damageType));
    }

    /// <summary>Vampires halve superficial damage to Health, rounding up; aggravated damage is unchanged.</summary>
    public static int DamageAfterVampireSuperficialReduction(
        int damage, ConflictDamageType damageType, bool targetIsVampire)
    {
        if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
        if (!targetIsVampire || damageType != ConflictDamageType.Superficial) return damage;
        return (damage + 1) / 2;
    }

    public static ConflictOutcome ResolveOpposed(
        ConflictKind kind, ConflictRoll first, ConflictRoll second, ConflictDamageType damageType,
        bool firstTargetIsVampire = false, bool secondTargetIsVampire = false)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        var exchange = ResolveOpposed(kind, first.Dice.Successes, second.Dice.Successes,
            first.DamageModifier, second.DamageModifier, damageType, firstTargetIsVampire, secondTargetIsVampire);
        var tied = exchange.IsTie;
        var firstOutcome = ResolveConflictRoll(first.Dice, tied || first.Dice.Successes > second.Dice.Successes);
        var secondOutcome = ResolveConflictRoll(second.Dice, tied || second.Dice.Successes > first.Dice.Successes);
        return new(exchange, firstOutcome, secondOutcome);
    }

    private static TestOutcome ResolveConflictRoll(DicePoolResult roll, bool won)
    {
        if (!won) return roll.HungerDice.Contains(1) ? TestOutcome.BestialFailure : TestOutcome.Failure;
        var critical = roll.Tens >= 2;
        if (critical && roll.HungerDice.Contains(10)) return TestOutcome.MessyCritical;
        return critical ? TestOutcome.CriticalSuccess : TestOutcome.Success;
    }

    public static int WeaponDamage(int conflictMargin, int weaponModifier)
    {
        if (conflictMargin < 0) throw new ArgumentOutOfRangeException(nameof(conflictMargin));
        return Math.Max(0, conflictMargin + weaponModifier);
    }
}
