using VCS.Domain.Blood;
using VCS.Domain.Characters;

namespace VCS.Tests;

public sealed class HumanityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Stains_damage_and_remorse_are_separate_explicit_operations(bool npc)
    {
        var details = new CharacterDetails { IsNpc = npc, Stains = 4, Humanity = 1 };
        var damaged = HumanityRules.ApplyStainDamage(details, CharacterRules.DefaultAttributes);
        Assert.Equal(1, damaged.WillpowerAggravated);
        Assert.Equal(4, damaged.Stains);
        Assert.Equal(1, damaged.Humanity);
        var remorse = HumanityRules.FailRemorse(damaged);
        Assert.Equal(0, remorse.Stains);
        Assert.Equal(0, remorse.Humanity);
        Assert.Equal(1, remorse.WillpowerAggravated);
        Assert.Empty(CharacterRules.Validate(remorse, CharacterRules.DefaultAttributes));
        Assert.Contains("Wassail", HumanityRules.At(0).Description);
        Assert.Throws<InvalidOperationException>(() => HumanityRules.FailRemorse(remorse));
        Assert.Throws<InvalidOperationException>(() => HumanityRules.FailRemorse(remorse with { Stains = 1 }));
        Assert.Equal(4, details.Stains);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Full_willpower_is_rejected_without_overwriting_existing_damage(bool npc)
    {
        var details = new CharacterDetails
        {
            IsNpc = npc, Stains = 1, NpcWillpowerCapacity = 2,
            WillpowerSuperficial = npc ? 0 : 1, WillpowerAggravated = npc ? 2 : 1
        };
        Assert.Throws<InvalidOperationException>(() => HumanityRules.ApplyStainDamage(details, CharacterRules.DefaultAttributes));
        Assert.Throws<InvalidOperationException>(() => HumanityRules.ApplyStainDamage(details with { Stains = 0 }, CharacterRules.DefaultAttributes));
        Assert.Equal(npc ? 2 : 1, details.WillpowerAggravated);
    }

    [Fact]
    public void Every_level_is_available_with_exact_social_reference_and_stains_validation()
    {
        Assert.Equal(Enumerable.Range(0, 11), HumanityRules.Levels.Select(x => x.Level).Order());
        Assert.Equal(new[] { -8, -6, -4, -2, -1 }, Enumerable.Range(1, 5).Select(x => HumanityRules.At(x).SocialPenalty));
        Assert.Equal(-5, HumanityRules.At(1).BlushSocialPenalty);
        Assert.Equal(-4, HumanityRules.At(2).BlushSocialPenalty);
        Assert.NotEmpty(CharacterRules.Validate(new() { Stains = -1 }, CharacterRules.DefaultAttributes));
        Assert.Empty(CharacterRules.Validate(new() { Stains = 15 }, CharacterRules.DefaultAttributes));
        Assert.Equal(9, HumanityRules.FailRemorse(new() { Humanity = 10, Stains = 1 }).Humanity);
    }
}
