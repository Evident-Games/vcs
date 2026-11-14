namespace VCS.Rules.V5.Conflicts;

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

public sealed record ConflictExchange(
    ConflictKind Kind,
    int FirstSuccesses,
    int SecondSuccesses,
    int Margin,
    bool IsTie,
    string? Winner,
    ConflictDamage Damage);

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
        ConflictDamageType damageType = ConflictDamageType.Superficial)
    {
        if (firstSuccesses < 0) throw new ArgumentOutOfRangeException(nameof(firstSuccesses));
        if (secondSuccesses < 0) throw new ArgumentOutOfRangeException(nameof(secondSuccesses));

        if (firstSuccesses == secondSuccesses)
        {
            var firstDamage = Math.Max(0, 1 + firstDamageModifier);
            var secondDamage = Math.Max(0, 1 + secondDamageModifier);
            return new(kind, firstSuccesses, secondSuccesses, 1, true, null,
                new(firstDamage, secondDamage, damageType));
        }

        var firstWins = firstSuccesses > secondSuccesses;
        var margin = Math.Abs(firstSuccesses - secondSuccesses);
        var winnerDamage = Math.Max(0, margin + (firstWins ? firstDamageModifier : secondDamageModifier));
        return new(kind, firstSuccesses, secondSuccesses, margin, false, firstWins ? "first" : "second",
            new(firstWins ? 0 : winnerDamage, firstWins ? winnerDamage : 0, damageType));
    }

    /// <summary>Vampires halve superficial damage to Health, rounding up; aggravated damage is unchanged.</summary>
    public static int DamageAfterVampireSuperficialReduction(
        int damage, ConflictDamageType damageType, bool targetIsVampire)
    {
        if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
        if (!targetIsVampire || damageType != ConflictDamageType.Superficial) return damage;
        return (damage + 1) / 2;
    }

    public static int WeaponDamage(int conflictMargin, int weaponModifier)
    {
        if (conflictMargin < 0) throw new ArgumentOutOfRangeException(nameof(conflictMargin));
        return Math.Max(0, conflictMargin + weaponModifier);
    }
}
