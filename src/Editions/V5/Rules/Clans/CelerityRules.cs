using Game.Content;

namespace Game.Rules.V5.Clans;

[RuleSource("Vampiro: A Máscara V5 (Livro base), pp. 252–254")]
public static class CelerityRules
{
    public static int FleetnessBonus(int celerity, bool isDexterity, bool isCombat, bool isDodge, bool alreadyUsedThisTurn)
    {
        if (celerity is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(celerity));
        return isDexterity && (!isCombat || isDodge && !alreadyUsedThisTurn) ? celerity : 0;
    }
}
