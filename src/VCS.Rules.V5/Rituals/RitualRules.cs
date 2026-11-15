using VCS.Content;

namespace VCS.Rules.V5.Rituals;

/// <summary>Default ritual requirements; individual rituals can explicitly override them.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base), p. 274")]
public static class RitualRules
{
    public static RitualRequirements DefaultRequirements(int level)
    {
        if (level is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(level));
        return new(level + 1, level * 5, 1);
    }
}

/// <summary>Difficulty, uninterrupted preparation in minutes and required Rouse checks.</summary>
public sealed record RitualRequirements(int Difficulty, int PreparationMinutes, int RouseChecks);
