using VCS.Content;

namespace VCS.Rules.V5.Conflicts;

[RuleSource("Vampiro: A Máscara V5 (Livro base), pp. 244, 249, 259, 267")]
public static class DisciplinePoolRules
{
    // Multiple Disciplines enhancing the same roll do not add their bonuses together.
    public static int StrongestBonus(params int[] bonuses) => bonuses.Length == 0 ? 0 : Math.Max(0, bonuses.Max());

    public static int AweBonus(int presence, bool active, bool targetResisted, string? skill, bool otherCharismaApproved)
    {
        if (presence is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(presence));
        return active && !targetResisted && (skill is "skill-persuasion" or "skill-performance" || otherCharismaApproved)
            ? presence : 0;
    }
}
