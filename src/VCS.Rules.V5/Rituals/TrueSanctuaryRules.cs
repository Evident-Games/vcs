using VCS.Content;

namespace VCS.Rules.V5.Rituals;

/// <summary>Preparation requirements for Escape to the True Sanctuary.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base), p. 281")]
public static class TrueSanctuaryRules
{
    public static TrueSanctuaryPreparationRequirements Preparation() => new(2, 3, 120, 2);
}

/// <summary>Each circle is consecrated over consecutive nights; the table tracks the schedule.</summary>
public sealed record TrueSanctuaryPreparationRequirements(
    int CircleCount, int ConsecutiveNightsPerCircle, int MinutesPerCirclePerNight, int RouseChecksPerCirclePerNight)
{
    public int TotalRouseChecks => CircleCount * ConsecutiveNightsPerCircle * RouseChecksPerCirclePerNight;
}
