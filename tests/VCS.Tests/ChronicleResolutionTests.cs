using VCS.Rules.V5.Blood;
using VCS.Rules.V5.Chronicle;
using VCS.Rules.V5.Conflicts;
using VCS.Rules.V5.Dice;

namespace VCS.Tests;

public class ChronicleResolutionTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 4)]
    [InlineData(3, 5)]
    [InlineData(4, 8)]
    public void TensGrantBonusOnlyInPairs(int count, int expected)
    {
        var result = DicePoolRules.Resolve(count, 0, 1, Enumerable.Repeat(10, count).ToArray());
        Assert.Equal(expected, result.Successes);
    }

    [Fact]
    public void HuntingDifficultyNeverDropsBelowOne() => Assert.Equal(1, HuntingRules.Difficulty(2, 0, -2, -5));

    [Fact]
    public void SuperficialOverflowUpgradesExistingDamage()
    {
        var result = DamageTrackRules.Apply(5, 4, 0, 3, ConflictDamageType.Superficial);
        Assert.Equal(3, result.Superficial);
        Assert.Equal(2, result.Aggravated);
        Assert.Equal(0, result.UnmarkedDamage);
    }

    [Fact]
    public void DiablerieLossCanCausePossession()
    {
        var result = DiablerieRules.Resolve(3, 13, 10, 1, 4);
        Assert.True(result.Possessed);
        Assert.Equal(12, result.Generation);
        Assert.Equal(5, result.RestrictedExperience);
        Assert.Equal(3, result.AuraYears);
    }
}
