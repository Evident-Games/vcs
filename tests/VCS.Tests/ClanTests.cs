using VCS.Content.Disciplines;
using VCS.Content.Clans;
using VCS.Desktop.Catalogs;
using VCS.Desktop.Sheet;
using VCS.Infrastructure.Catalogs;
using VCS.Rules.V5.Clans;
using VCS.Sheet;

namespace VCS.Tests;

public sealed class ClanTests
{
    [Theory]
    [InlineData("Banu Haqim", "Celeridade|Feitiçaria de Sangue|Ofuscação")]
    [InlineData("Brujah", "Celeridade|Potência|Presença")]
    [InlineData("Gangrel", "Animalismo|Fortitude|Proteanismo")]
    [InlineData("Malkavianos", "Auspícios|Dominação|Ofuscação")]
    [InlineData("O Ministério", "Ofuscação|Presença|Proteanismo")]
    [InlineData("Nosferatu", "Animalismo|Ofuscação|Potência")]
    [InlineData("Ravnos", "Animalismo|Ofuscação|Presença")]
    [InlineData("Salubri", "Auspícios|Dominação|Fortitude")]
    [InlineData("Toreador", "Auspícios|Celeridade|Presença")]
    [InlineData("Tremere", "Auspícios|Dominação|Feitiçaria de Sangue")]
    [InlineData("Tzimisce", "Animalismo|Dominação|Proteanismo")]
    [InlineData("Ventrue", "Dominação|Fortitude|Presença")]
    [InlineData("Hecata", "Auspícios|Fortitude|Oblívio")]
    [InlineData("Lasombra", "Dominação|Oblívio|Potência")]
    public void Each_clan_has_its_exact_three_disciplines(string name, string disciplines)
    {
        var clan = Assert.IsType<ClanDefinition>(ClanCatalog.Find(name));
        Assert.Equal(14, ClanCatalog.All.Count);
        Assert.Equal(3, clan.Disciplines.Distinct().Count());
        Assert.Equal(disciplines.Split('|').Order(), clan.Disciplines.Select(DisciplineCatalog.NameOf).Order());
        Assert.False(string.IsNullOrWhiteSpace(clan.Description));
        Assert.False(string.IsNullOrWhiteSpace(clan.Bane));
    }

    [Theory]
    [InlineData("assamitas", "Banu Haqim")]
    [InlineData("  SETITAS ", "O Ministério")]
    [InlineData("ministerio", "O Ministério")]
    [InlineData("Malkavian", "Malkavianos")]
    [InlineData("Giovanni", "Hecata")]
    public void Aliases_resolve_without_rewriting_the_character(string alias, string canonical) =>
        Assert.Equal(canonical, ClanCatalog.Find(alias)?.Name);

    [Theory]
    [InlineData("Brujah", "Celeridade", 0, 5)]
    [InlineData("Brujah", "Celeridade", 2, 15)]
    [InlineData("Brujah", "Dominacao", 0, 7)]
    [InlineData("Brujah", "Dominacao", 2, 21)]
    [InlineData("Giovanni", "Oblivio", 4, 25)]
    [InlineData("Setitas", "Proteanismo", 4, 25)]
    public void Cost_uses_new_level_and_correct_multiplier(string clan, string discipline, int level, int expected) =>
        Assert.Equal(expected, DisciplineAdvancement.NextLevelCost(clan, discipline, level));

    [Fact]
    public void Unknown_clan_maximum_level_and_invalid_level_are_handled_explicitly()
    {
        Assert.Null(DisciplineAdvancement.IsInClan("Meu cla", "Animalismo"));
        Assert.Null(DisciplineAdvancement.NextLevelCost("", "Animalismo", 2));
        Assert.Null(DisciplineAdvancement.NextLevelCost("Brujah", "Animalismo", 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => DisciplineAdvancement.NextLevelCost("Brujah", "Animalismo", -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => DisciplineAdvancement.NextLevelCost("Brujah", "Animalismo", 6));
    }

    [Fact]
    public void Changing_clan_updates_costs_without_removing_powers_or_changing_discipline_levels()
    {
        using var disciplines = new TraitGroupViewModel(TraitCategory.Discipline,
            [new(TraitCategory.Discipline, "custom", "Celeridade", 2)]);
        using var browser = new DisciplineCatalogViewModel(new ProvidedDisciplineCatalog(), disciplines, []);
        var power = browser.AllPowers.First(x => x.Definition.Discipline == "Celeridade");
        power.IsKnown = true;
        browser.Clan = "Brujah";
        Assert.Contains("15 XP", power.AdvancementInfo);
        browser.Clan = "Tremere";
        Assert.Contains("21 XP", power.AdvancementInfo);
        Assert.Contains("Fora do cla", power.AdvancementInfo);
        Assert.True(power.IsKnown);
        Assert.Equal(2, Assert.Single(disciplines.Items).Rating);
        browser.Clan = "Personalizado";
        Assert.Contains("nao definido", power.AdvancementInfo);
        var ritual = browser.AllPowers.First(x => x.Definition.IsRitual);
        Assert.Contains("nao calculam", ritual.AdvancementInfo);
    }
}
