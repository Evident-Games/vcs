using VCS.Content;

namespace VCS.Rules.V5.Conflicts;

[RuleSource("Vampiro: A Máscara V5 (Livro base), pp. 258–260, 264–265")]
public static class DisciplineCombatRules
{
    public static int ProwessDamage(int potence, bool meleeWeapon)
    {
        if (potence is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(potence));
        return meleeWeapon ? (potence + 1) / 2 : potence;
    }

    public static int SuperficialDamage(int damage, int fortitude, bool toughness, bool vampire)
    {
        if (damage < 0 || fortitude is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(damage));
        if (damage == 0) return 0;
        var reduced = toughness ? Math.Max(1, damage - fortitude) : damage;
        return vampire ? (reduced + 1) / 2 : reduced;
    }

    public static int InnerFacadeDifficulty(int fortitude)
    {
        if (fortitude is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(fortitude));
        return (fortitude + 1) / 2;
    }
}
