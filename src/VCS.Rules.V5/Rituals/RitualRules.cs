using VCS.Content;

namespace VCS.Rules.V5.Rituals;

/// <summary>Default ritual requirements; individual rituals can explicitly override them.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base), pp. 274, 281")]
public static class RitualRules
{
    /// <summary>Rituals whose preparation must not use the generic one-check workflow.</summary>
    public static bool RequiresManualPreparation(string? ritualId) =>
        ritualId == "ritual-escape-to-true-sanctuary";

    public static RitualRequirements DefaultRequirements(int level)
    {
        if (level is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(level));
        return new(level + 1, level * 5, 1);
    }
}

/// <summary>Difficulty, uninterrupted preparation in minutes and required Rouse checks.</summary>
public sealed record RitualRequirements(int Difficulty, int PreparationMinutes, int RouseChecks);
