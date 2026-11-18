using Game.Content;
using Game.Rules.V5.Dice;

namespace Game.Rules.V5.Disciplines;

/// <summary>Effects of a subdued vampire's Beast on its own dice rolls.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base), p. 246")]
public static class QuellBeastRules
{
    public static bool CanUseBloodSurge(bool beastSubdued) => !beastSubdued;

    public static DicePoolResult ApplyToRoll(DicePoolResult roll, bool beastSubdued) =>
        beastSubdued && roll.Outcome == TestOutcome.MessyCritical
            ? roll with { Outcome = TestOutcome.CriticalSuccess }
            : roll;
}
