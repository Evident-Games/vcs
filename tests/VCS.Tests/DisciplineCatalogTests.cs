using VCS.Desktop.ViewModels;
using VCS.Domain.Characters;
using VCS.Infrastructure.Files;

namespace VCS.Tests;

public sealed class DisciplineCatalogTests
{
    private readonly ProvidedDisciplineCatalog _catalog = new();

    [Fact]
    public void Rows_resolve_catalog_names_and_keep_custom_power_details()
    {
        using var disciplines = new TraitGroupViewModel(TraitCategory.Discipline, []);
        using var browser = new DisciplineCatalogViewModel(_catalog, disciplines, []);
        var definition = _catalog.Powers.First(x => !x.IsRitual);
        var row = browser.AddRow(false);
        row.Name = definition.Name.ToUpperInvariant();
        Assert.True(row.IsCatalog);
        Assert.Equal(definition.Id, row.Key);
        Assert.Equal(definition.RequiredDiscipline, row.Discipline);
        Assert.Equal(definition.Cost, row.Cost);
        Assert.True(browser.AllPowers.Single(x => x.Definition.Id == definition.Id).IsKnown);
        var catalogValue = Assert.Single(browser.SelectedValues());
        Assert.Null(catalogValue.Power);

        row.Duration = "Uma cena";
        Assert.Equal("Uma cena", Assert.Single(browser.SelectedValues()).Power!.Duration);

        var custom = browser.AddRow(false);
        custom.Discipline = "Disciplina da casa";
        custom.Name = "Toque pessoal";
        custom.Level = 3;
        custom.Cost = "Um teste de Despertar";
        custom.Description = "Efeito escrito pelo jogador.";
        Assert.False(custom.IsCatalog);
        Assert.StartsWith("power-", custom.Key);
        var ritual = browser.AddRow(true);
        ritual.Name = "Selo de cinzas";
        ritual.Ingredients = "Cinzas";
        ritual.Process = "Desenhar o selo.";

        var values = browser.SelectedValues();
        Assert.Equal(3, values.Length);
        using var reloaded = new DisciplineCatalogViewModel(_catalog, disciplines, values);
        var restored = Assert.Single(reloaded.PowerRows, x => x.Key == custom.Key);
        Assert.Equal(("Disciplina da casa", "Toque pessoal", 3, "Um teste de Despertar", "Efeito escrito pelo jogador."),
            (restored.Discipline, restored.Name, restored.Level, restored.Cost, restored.Description));
        Assert.Equal("Uma cena", Assert.Single(reloaded.PowerRows, x => x.Key == definition.Id).Duration);
        var restoredRitual = Assert.Single(reloaded.RitualRows);
        Assert.Equal(("Selo de cinzas", "Cinzas", "Desenhar o selo."), (restoredRitual.Name, restoredRitual.Ingredients, restoredRitual.Process));

        row.RemoveCommand.Execute(null);
        Assert.False(browser.AllPowers.Single(x => x.Definition.Id == definition.Id).IsKnown);
        Assert.Equal(2, browser.SelectedValues().Length);
    }

    [Fact]
    public void Name_suggestions_follow_the_row_discipline()
    {
        using var disciplines = new TraitGroupViewModel(TraitCategory.Discipline, []);
        using var browser = new DisciplineCatalogViewModel(_catalog, disciplines, []);
        var row = browser.AddRow(false);
        Assert.Equal(_catalog.Powers.Count(x => !x.IsRitual), row.NameSuggestions.Count);
        row.Discipline = "dominacao";
        Assert.NotEmpty(row.NameSuggestions);
        Assert.All(row.NameSuggestions, x => Assert.Contains(_catalog.Powers,
            p => p.Name == x.Value && p.RequiredDiscipline == "Dominação"));
        Assert.Equal(5, browser.AddRow(true).NameSuggestions.Count);
    }

    [Fact]
    public void Dot_spread_groups_ratings_from_highest()
    {
        Assert.Equal("1 × ●●●● · 3 × ●●● · 4 × ●● · 1 × ●", CharacterEditorViewModel.DotSpread([2, 4, 3, 1, 2, 3, 2, 3, 2]));
        Assert.Equal("", CharacterEditorViewModel.DotSpread([]));
    }
    [Fact]
    public void All_eighty_four_supplied_entries_are_available_with_stable_unique_ids()
    {
        Assert.Equal(84, _catalog.Powers.Count);
        Assert.Equal(79, _catalog.Powers.Count(x => !x.IsRitual));
        Assert.Equal(5, _catalog.Powers.Count(x => x.IsRitual));
        Assert.Equal(11, _catalog.Powers.Where(x => !x.IsRitual).Select(x => x.Discipline).Distinct().Count());
        Assert.Equal(84, _catalog.Powers.Select(x => x.Id).Distinct().Count());
        Assert.Equal(_catalog.Powers, new ProvidedDisciplineCatalog().Powers);
        Assert.All(_catalog.Powers, power =>
        {
            Assert.InRange(power.Level, 1, 5);
            Assert.InRange(power.Id.Length, 1, 80);
            Assert.False(string.IsNullOrWhiteSpace(power.Cost));
            Assert.False(string.IsNullOrWhiteSpace(power.DicePool));
            Assert.False(string.IsNullOrWhiteSpace(power.Summary));
        });
        Assert.Equal(new[] { 1, 2, 3, 4, 5 },
            _catalog.Powers.Where(x => x.IsRitual).Select(x => x.Level).Order());
    }

    [Fact]
    public void Parser_preserves_supplied_text_and_rejects_missing_fields_and_duplicates()
    {
        const string text = """
            Disciplina: Disciplina pessoal
            Nome da habilidade: Teste pessoal (Nível 2)
            Custo: Custo escolhido
            Parada de dados: Dados escolhidos
            Resumo: Texto do usuario, sem alteracoes.
            """;
        var power = Assert.Single(ProvidedDisciplineCatalog.Parse(text));
        Assert.Equal("Teste pessoal", power.Name);
        Assert.Equal(2, power.Level);
        Assert.Equal("Custo escolhido", power.Cost);
        Assert.Equal("Dados escolhidos", power.DicePool);
        Assert.Equal("Texto do usuario, sem alteracoes.", power.Summary);
        Assert.Throws<InvalidDataException>(() => ProvidedDisciplineCatalog.Parse(text.Replace("Custo:", "Ausente:")));
        Assert.Throws<InvalidDataException>(() => ProvidedDisciplineCatalog.Parse(text + "\n" + text));
        Assert.Throws<InvalidDataException>(() => ProvidedDisciplineCatalog.Parse(text.Replace("Nível 2", "Nível 6")));
    }

    [Fact]
    public void Search_is_accent_insensitive_and_filters_never_remove_selections()
    {
        using var disciplines = new TraitGroupViewModel(TraitCategory.Discipline, []);
        using var browser = new DisciplineCatalogViewModel(_catalog, disciplines, []);
        var power = browser.AllPowers.First(x => x.Definition.Discipline == "Dominação" && x.Definition.Level == 5);
        power.IsKnown = true;
        browser.SearchText = "dominacao";
        Assert.Contains(power, browser.Powers);
        Assert.Contains(browser.Powers, x => x.Definition.Discipline == "Fortitude");
        browser.SelectedDiscipline = "Dominação";
        Assert.All(browser.Powers, x => Assert.Equal("Dominação", x.Definition.Discipline));
        browser.SearchText = "NAO EXISTE";
        Assert.Empty(browser.Powers);
        Assert.Null(browser.SelectedPower);
        Assert.Single(browser.SelectedValues());
        browser.SearchText = "";
        browser.KnownOnly = true;
        Assert.Same(power, Assert.Single(browser.Powers));
        power.IsKnown = false;
        Assert.Empty(browser.SelectedValues());
        Assert.Empty(browser.Powers);
    }

    [Fact]
    public void Above_level_is_only_warning_and_existing_custom_discipline_is_reused()
    {
        using var disciplines = new TraitGroupViewModel(TraitCategory.Discipline,
            [new(TraitCategory.Discipline, "legacy-custom", "  DOMINACAO  ", 1)]);
        using var browser = new DisciplineCatalogViewModel(_catalog, disciplines, []);
        var power = browser.AllPowers.First(x => x.Definition.Discipline == "Dominação" && x.Definition.Level == 5);
        power.IsKnown = true;
        Assert.Single(disciplines.Items);
        Assert.Contains("nivel 5", power.Warning);
        Assert.Single(browser.SelectedValues());
        disciplines.Items[0].Rating = 5;
        Assert.Empty(power.Warning);
        disciplines.RemoveCommand.Execute(disciplines.Items[0]);
        Assert.NotEmpty(power.Warning);
        Assert.True(power.IsKnown);
    }

    [Fact]
    public void Rituals_are_separate_and_refer_to_blood_sorcery_without_raising_rating()
    {
        using var disciplines = new TraitGroupViewModel(TraitCategory.Discipline, []);
        using var browser = new DisciplineCatalogViewModel(_catalog, disciplines, []);
        var ritual = browser.Rituals.First(x => x.Definition.Level == 5);
        ritual.IsKnown = true;
        var discipline = Assert.Single(disciplines.Items);
        Assert.Equal("Feitiçaria de Sangue", discipline.Name);
        Assert.Equal(0, discipline.Rating);
        Assert.NotEmpty(ritual.Warning);
        browser.SelectedDiscipline = "Feitiçaria de Sangue";
        Assert.Equal(6, browser.Powers.Count);
        Assert.Equal(5, browser.Rituals.Count);
        browser.SelectedDiscipline = "Animalismo";
        Assert.Empty(browser.Rituals);
        Assert.Single(browser.SelectedValues());
    }

    [Fact]
    public void Unknown_power_from_another_catalog_is_preserved_with_explicit_notice()
    {
        using var disciplines = new TraitGroupViewModel(TraitCategory.Discipline, []);
        var unknown = new TraitValue(TraitCategory.DisciplinePower, "external-power", "Poder externo", 2);
        using var browser = new DisciplineCatalogViewModel(_catalog, disciplines, [unknown]);
        var entry = Assert.Single(browser.AllPowers, x => x.IsUnavailable);
        Assert.True(entry.IsKnown);
        Assert.Contains("preservada", entry.Warning);
        Assert.Equal(unknown, Assert.Single(browser.SelectedValues()));
    }

    [Theory]
    [InlineData("Quimerismo", "Ofuscação", 2, "Presença", 1)]
    [InlineData("Fata Morgana", "Ofuscação", 3, "Presença", 2)]
    [InlineData("Obeah", "Auspícios", 2, "Fortitude", 1)]
    [InlineData("Aliviando a Alma Bestial", "Auspícios", 5, "Dominação", 3)]
    public void New_amalgams_have_correct_levels_and_non_blocking_requirement_warnings(
        string name, string discipline, int level, string amalgam, int requirement)
    {
        var definition = Assert.Single(_catalog.Powers, x => x.Name == name);
        Assert.Equal(discipline, definition.Discipline);
        Assert.Equal(level, definition.Level);
        Assert.Equal(amalgam, definition.AmalgamDiscipline);
        Assert.Equal(requirement, definition.AmalgamLevel);
        Assert.False(definition.IsRitual);
        using var disciplines = new TraitGroupViewModel(TraitCategory.Discipline,
            [new(TraitCategory.Discipline, "primary", discipline, level),
             new(TraitCategory.Discipline, "amalgam", amalgam, 0)]);
        using var browser = new DisciplineCatalogViewModel(_catalog, disciplines, []);
        var entry = browser.AllPowers.Single(x => x.Definition.Name == name);
        entry.IsKnown = true;
        Assert.Contains("amalgama", entry.Warning);
        Assert.Single(browser.SelectedValues());
        disciplines.Items.Single(x => x.Key == "amalgam").Rating = requirement;
        Assert.Empty(entry.Warning);
        Assert.Contains(amalgam, entry.Heading);
    }
}
