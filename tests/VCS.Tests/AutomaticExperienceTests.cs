using VCS.Domain.Characters;

namespace VCS.Tests;

public sealed class AutomaticExperienceTests
{
    private static CharacterDetails Details => new()
    {
        Clan = "Brujah", BloodPotency = 1,
        Advancement = new() { AutomaticExperience = true, Age = VampireAge.Neonate }
    };

    [Theory]
    [InlineData(SkillProfile.Specialist)]
    [InlineData(SkillProfile.Balanced)]
    [InlineData(SkillProfile.Generalist)]
    public void Initial_distributions_are_free_without_registering_a_baseline(SkillProfile profile)
    {
        var attributes = CharacterRules.DefaultAttributes.Select((x, i) =>
            x with { Rating = new[] { 4, 3, 3, 3, 2, 2, 2, 2, 1 }[i] });
        var skills = CharacterAdvancement.SkillDistribution(profile).Select((rating, i) =>
            new TraitValue(TraitCategory.Skill, $"skill-{i}", $"Habilidade {i}", rating));
        TraitValue[] traits = [.. attributes, .. skills,
            new(TraitCategory.Discipline, "potence", "Potência", 2),
            new(TraitCategory.Discipline, "celerity", "Celeridade", 1),
            new(TraitCategory.Advantage, "merit", "Vantagem", 5),
            new(TraitCategory.Advantage, "merit2", "Outra vantagem", 2)];
        var result = AutomaticExperience.Calculate(Details with { Advancement = Details.Advancement! with { Profile = profile } }, traits);
        Assert.Equal(15, result.Total);
        Assert.Equal(0, result.Spent);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Every_excess_level_is_charged_and_shortage_is_only_a_warning()
    {
        TraitValue[] traits = [.. CharacterRules.DefaultAttributes.Select(x => x with { Rating = 5 }),
            new(TraitCategory.Skill, "skill", "Habilidade", 5),
            new(TraitCategory.Discipline, "potence", "Potência", 3),
            new(TraitCategory.Discipline, "celerity", "Celeridade", 2),
            new(TraitCategory.Discipline, "obfuscate", "Ofuscação", 2),
            new(TraitCategory.Advantage, "merit", "Vantagem", 5),
            new(TraitCategory.Advantage, "merit2", "Outra vantagem", 5)];
        var attributeBase = CharacterRules.DefaultAttributes.Select((x, i) =>
            new AttributeBaseRating(x.Key, CharacterAdvancement.AttributeDistribution[i])).ToArray();
        var details = Details with { BloodPotency = 2, Advancement = Details.Advancement! with { AttributeBase = attributeBase } };
        Assert.DoesNotContain(AutomaticExperience.Calculate(Details, traits).Costs, x => x.Section == "Attributes");
        var result = AutomaticExperience.Calculate(details, traits);
        Assert.Equal(470, result.Costs.Where(x => x.Section == "Attributes").Sum(x => x.Cost));
        Assert.Equal(15, result.Costs.Single(x => x.Section == "Skills").Cost);
        Assert.Equal(46, result.Costs.Where(x => x.Section == "Disciplines").Sum(x => x.Cost));
        Assert.Equal(9, result.Costs.Single(x => x.Section == "Advantages").Cost);
        Assert.Equal(20, result.Costs.Single(x => x.Section == "State").Cost);
        Assert.Equal(560, result.Spent);
        Assert.Contains(result.Warnings, x => x.Message == "Faltam 545 XP para pagar os pontos da ficha.");
        var saved = details with { ExperienceTotal = result.Total, ExperienceSpent = result.Spent, CampaignExperienceSpent = result.CampaignSpent };
        Assert.Empty(CharacterRules.Validate(saved, traits));
        Assert.NotEmpty(CharacterRules.Validate(saved with { ExperienceSpent = 0 }, traits));
        var funded = AutomaticExperience.Calculate(details with { CampaignExperienceTotal = 545 }, traits);
        Assert.Equal(560, funded.Total);
        Assert.Equal(545, funded.CampaignSpent);
        Assert.Empty(funded.Warnings);
    }

    [Fact]
    public void Predator_bonus_is_free_but_subsequent_levels_use_the_actual_rating()
    {
        var details = Details with
        {
            PredatorSelection = new("alleycat", "potence", "Potência", "brawl", "Briga", "Agarramento", true, [], -1, 0)
        };
        var initial = AutomaticExperience.Calculate(details,
            [new(TraitCategory.Discipline, "potence", "Potência", 3),
             new(TraitCategory.Discipline, "celerity", "Celeridade", 1)]);
        Assert.Equal(0, initial.Spent);
        var increased = AutomaticExperience.Calculate(details,
            [new(TraitCategory.Discipline, "potence", "Potência", 4),
             new(TraitCategory.Discipline, "celerity", "Celeridade", 1)]);
        Assert.Equal(20, increased.Spent);
        var outOfClan = AutomaticExperience.Calculate(details with
        {
            PredatorSelection = details.PredatorSelection! with { DisciplineKey = "obfuscate", Discipline = "Ofuscação" }
        }, [new(TraitCategory.Discipline, "obfuscate", "Ofuscação", 2)]);
        Assert.Equal(14, outOfClan.Spent);
    }

    [Fact]
    public void Unknown_clan_is_reported_instead_of_assuming_a_discipline_price()
    {
        var result = AutomaticExperience.Calculate(Details with { Clan = "" },
            [new(TraitCategory.Discipline, "obfuscate", "Ofuscação", 3)]);
        Assert.Contains(result.Warnings, x => x.Field == "Disciplines" && x.Message.Contains("cla conhecido"));
    }

    [Fact]
    public void Extra_specializations_cost_three_xp_without_charging_the_free_one()
    {
        var result = AutomaticExperience.Calculate(Details,
            [new(TraitCategory.Skill, "brawl", "Briga", 1, "Uma; Duas; Tres")]);
        Assert.Equal(6, result.Spent);
        Assert.Equal("Especializacoes adicionais", Assert.Single(result.Costs).Name);
    }
}
