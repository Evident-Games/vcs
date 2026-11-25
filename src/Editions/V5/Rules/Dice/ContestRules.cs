using Game.Content;

namespace Game.Rules.V5.Dice;

/// <summary>V5 contests: the acting character wins a non-total-failure tie.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base), pp. 121–122")]
public static class ContestRules
{
    public static int ActingDifficulty(int opponentSuccesses)
    {
        if (opponentSuccesses < 0) throw new ArgumentOutOfRangeException(nameof(opponentSuccesses));
        return opponentSuccesses;
    }

    public static bool ActingCharacterWins(int actingSuccesses, int opponentSuccesses)
    {
        if (actingSuccesses < 0) throw new ArgumentOutOfRangeException(nameof(actingSuccesses));
        return actingSuccesses > 0 && actingSuccesses >= ActingDifficulty(opponentSuccesses);
    }
}
