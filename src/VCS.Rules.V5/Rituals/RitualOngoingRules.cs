using VCS.Content;

namespace VCS.Rules.V5.Rituals;

/// <summary>Context must be explicitly supplied by the table; never infer a social test or damage source.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base), pp. 278–281")]
public static class RitualOngoingRules
{
    public static int PoolModifier(bool heartOfStone, bool activeSocialTest, bool intimidation,
        bool dominate, bool resistingPresence) => !heartOfStone ? 0
        : resistingPresence ? 3 : activeSocialTest && !intimidation && !dominate ? -3 : 0;

    public static int FireDamage(int damage, bool firewalker, bool fire)
    {
        if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
        return firewalker && fire ? (damage + 1) / 2 : damage;
    }

    public static bool IgnoresDamage(bool incorporeal, bool ordinaryPhysicalSource,
        bool protectedByHaven, bool sunlight) => incorporeal && ordinaryPhysicalSource || protectedByHaven && sunlight;
}
