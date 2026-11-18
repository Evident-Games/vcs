using Game.Content;

namespace Game.Rules.V5.Disciplines;

/// <summary>Context-dependent automatic movement outcomes for V5 discipline powers.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base), pp. 252, 265")]
public static class MovementPowerRules
{
    public static bool CatGraceSucceeds(bool knowsPower, bool balanceTest, bool surfaceSupportsWeight,
        bool dexterityOrAthletics) =>
        knowsPower && balanceTest && surfaceSupportsWeight && dexterityOrAthletics;

    public static bool UncannyGripSucceeds(bool active, bool climbingSkillTest, bool suitableNonMetalSurface) =>
        active && climbingSkillTest && suitableNonMetalSurface;
}
