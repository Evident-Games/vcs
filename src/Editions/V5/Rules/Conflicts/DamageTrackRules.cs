using VCS.Content;

namespace VCS.Rules.V5.Conflicts;

public sealed record DamageTrackResult(int Superficial, int Aggravated, int UnmarkedDamage)
{
    public bool IsImpaired(int capacity) => Superficial + Aggravated >= capacity;
}

/// <summary>Applies damage to a track; superficial overflow upgrades existing superficial boxes.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base)")]
public static class DamageTrackRules
{
    public static DamageTrackResult Apply(int capacity, int superficial, int aggravated, int damage, ConflictDamageType type)
    {
        if (capacity < 1 || superficial < 0 || aggravated < 0 || superficial + aggravated > capacity || damage < 0)
            throw new ArgumentOutOfRangeException(nameof(damage), "Trilha ou quantidade de dano inválida.");
        var remaining = damage;
        while (remaining > 0 && aggravated < capacity)
        {
            if (type == ConflictDamageType.Superficial && superficial + aggravated < capacity) superficial++;
            else
            {
                if (superficial + aggravated >= capacity && superficial > 0) superficial--;
                aggravated++;
            }
            remaining--;
        }
        return new(superficial, aggravated, remaining);
    }
}
