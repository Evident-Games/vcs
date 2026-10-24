using VCS.Desktop.Catalogs;
using VCS.Desktop.Sheet;
using VCS.Domain.Advantages;
using VCS.Domain.Characters;
using VCS.Infrastructure.Catalogs;
using VCS.Infrastructure.Persistence;

namespace VCS.Tests;

public sealed class MeritFamilyTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "VCS-merits-" + Guid.NewGuid().ToString("N"));
    private IReadOnlyList<AdvantageDefinition> _entries = [];
    public async Task InitializeAsync()
    {
        var database = new DatabaseAccess(Path.Combine(_directory, "test.db"));
        await new SqliteDatabaseMaintenance(database).InitializeAsync();
        _entries = await new SqliteAdvantageCatalog(database).ListAsync();
    }
    public Task DisposeAsync() { Directory.Delete(_directory, true); return Task.CompletedTask; }

    private static void Type(MeritRowViewModel row, string text)
    {
        for (var i = 1; i <= text.Length; i++) row.Name = text[..i];
    }

    private sealed class LoreHost : IMeritSheetHost
    {
        public TraitGroupViewModel Loresheets { get; } = new(TraitCategory.Loresheet, []);
        public int Removed;
        public PredatorAllocationViewModel? PredatorOption(TraitItemViewModel grant) => null;
        public IReadOnlyList<PredatorAllocationViewModel> PendingPredatorOptions(TraitCategory category) => [];
        public int PredatorRemaining(TraitCategory category) => 0;
        public int PredatorRoom(PredatorAllocationViewModel option) => 0;
        public void AllocatePredator(PredatorAllocationViewModel option, int points) { }
        public string? AddLoresheet(AdvantageDefinition benefit)
        {
            Loresheets.AddEntry(new(TraitCategory.Loresheet, benefit.Id, benefit.Name, benefit.Minimum,
                Loresheet: new(benefit.LoresheetCollection, benefit.IsLineage, benefit.Description, LoresheetPayment.AdvantagePoints)));
            return null;
        }
        public void RemoveLoresheet(TraitItemViewModel item)
        {
            Removed++;
            Loresheets.RemoveCommand.Execute(item);
        }
    }

    [Fact]
    public void Loresheets_are_advantage_rows_with_their_collection_below()
    {
        var host = new LoreHost();
        using var advantages = new TraitGroupViewModel(TraitCategory.Advantage, []);
        using var flaws = new TraitGroupViewModel(TraitCategory.Flaw, []);
        using var sheet = new AdvantageCatalogViewModel(_entries, advantages, flaws, host);
        sheet.AddAdvantageRowCommand.Execute(null);
        var row = Assert.Single(sheet.AdvantageRows);
        Assert.Contains(row.CategorySuggestions, x => x.Value == AdvantageCatalogViewModel.LoresheetFamily && x.Detail.Contains("benefícios"));
        sheet.AddFlawRowCommand.Execute(null);
        Assert.DoesNotContain(sheet.FlawRows[0].CategorySuggestions, x => x.Value == AdvantageCatalogViewModel.LoresheetFamily);
        row.Category = AdvantageCatalogViewModel.LoresheetFamily;
        Assert.All(row.NameSuggestions, x => Assert.True(x.HasNote));
        var benefit = _entries.First(x => x.Category == TraitCategory.Loresheet);
        Assert.Contains(row.NameSuggestions, x => x.Value == benefit.Name && x.Note == benefit.LoresheetCollection);
        row.Name = benefit.Name;
        var item = Assert.Single(host.Loresheets.Items);
        Assert.Empty(advantages.Items);
        Assert.Same(row, Assert.Single(sheet.AdvantageRows));
        Assert.True(row.IsLoresheet);
        Assert.True(row.IsFixed);
        Assert.False(row.CanEditName);
        Assert.False(row.CanEditRating);
        Assert.Equal(benefit.LoresheetCollection, row.Subtitle);
        Assert.Equal(AdvantageCatalogViewModel.LoresheetFamily, row.Category);
        Assert.Null(item.MeritGroup);
        row.RemoveCommand.Execute(null);
        Assert.Equal(1, host.Removed);
        Assert.Empty(host.Loresheets.Items);
        Assert.Empty(sheet.AdvantageRows);
    }

    [Fact]
    public void Rows_pick_catalog_entries_by_typing_within_a_category()
    {
        using var advantages = new TraitGroupViewModel(TraitCategory.Advantage, []);
        using var flaws = new TraitGroupViewModel(TraitCategory.Flaw, []);
        using var sheet = new AdvantageCatalogViewModel(_entries, advantages, flaws);
        Assert.Empty(sheet.AdvantageRows);
        sheet.AddAdvantageRowCommand.Execute(null);
        var row = Assert.Single(sheet.AdvantageRows);
        Assert.Contains(row.CategorySuggestions, x => x.Value == "Mortais");
        Assert.Contains(row.NameSuggestions, x => x.Value == "Deslumbrante" && x.Detail.StartsWith("Beleza"));
        row.Category = "beleza";
        Assert.All(row.NameSuggestions, x => Assert.Contains(x.Value, new[] { "Visual Belo", "Deslumbrante" }));
        Type(row, "Deslumbrante");
        var item = Assert.Single(advantages.Items);
        Assert.Equal("catalog-deslumbrante", item.Key);
        Assert.Equal(4, item.Rating);
        Assert.True(row.IsCatalog);
        Assert.Equal("Beleza", row.Category);
        Assert.Same(row, Assert.Single(sheet.AdvantageRows));

        sheet.AddAdvantageRowCommand.Execute(null);
        var second = sheet.AdvantageRows[1];
        Assert.DoesNotContain(second.NameSuggestions, x => x.Value == "Deslumbrante");
        second.Name = "Deslumbrante";
        Assert.True(second.HasWarning);
        Assert.Single(advantages.Items);
        second.Name = "";
        Assert.False(second.HasWarning);

        sheet.AddFlawRowCommand.Execute(null);
        Assert.Single(sheet.FlawRows).Name = "Feio";
        Assert.Empty(advantages.Items);
        Assert.Single(flaws.Items);
        Assert.DoesNotContain(row, sheet.AdvantageRows);
        sheet.FlawRows[0].RemoveCommand.Execute(null);
        Assert.Empty(flaws.Items);
        Assert.Empty(sheet.FlawRows);
    }

    [Fact]
    public void Rows_keep_custom_entries_with_category_rating_and_notes()
    {
        using var advantages = new TraitGroupViewModel(TraitCategory.Advantage, []);
        using var flaws = new TraitGroupViewModel(TraitCategory.Flaw, []);
        using var sheet = new AdvantageCatalogViewModel(_entries, advantages, flaws);
        sheet.AddAdvantageRowCommand.Execute(null);
        var row = sheet.AdvantageRows[0];
        row.Category = "Mortais";
        row.Rating = 2;
        row.Notes = "Dono do bar.";
        Type(row, "Contato no porto");
        var item = Assert.Single(advantages.Items);
        Assert.Equal(new MeritInfo("Mortais", "Dono do bar."), item.ToValue().Merit);
        Assert.Equal(2, item.Rating);
        Assert.False(row.IsCatalog);
        row.Category = "Refúgio";
        Assert.Equal("Refúgio", item.MeritGroup);
        Assert.Same(item, Assert.Single(advantages.Items));

        sheet.Families.Single(x => x.Name == "Beleza").Options.Single(x => x.Name == "Visual Belo").IsSelected = true;
        Assert.Equal(2, sheet.AdvantageRows.Count);
        var beauty = sheet.AdvantageRows.Single(x => x.IsCatalog);
        beauty.Notes = "Olhos claros.";
        Assert.Equal(new MeritInfo("Beleza", "Olhos claros."), advantages.Items.Single(x => x.Key == "catalog-visual-belo").ToValue().Merit);
        beauty.Rating = 0;
        Assert.Equal(2, beauty.Rating);

        using var reopened = new AdvantageCatalogViewModel(_entries,
            new TraitGroupViewModel(TraitCategory.Advantage, advantages.Items.Select(x => x.ToValue())), flaws);
        Assert.Equal(["Beleza", "Refúgio"], reopened.AdvantageRows.Select(x => x.Category));
        Assert.Equal("Contato no porto", reopened.AdvantageRows[1].Name);
    }

    [Fact]
    public void Related_advantages_and_flaws_share_families_and_beauty_is_exclusive()
    {
        using var advantages = new TraitGroupViewModel(TraitCategory.Advantage, []);
        using var flaws = new TraitGroupViewModel(TraitCategory.Flaw, []);
        using var sheet = new AdvantageCatalogViewModel(_entries, advantages, flaws);
        var beauty = sheet.Families.Single(x => x.Name == "Beleza");
        Assert.Equal(4, beauty.Options.Count);
        var beautiful = beauty.Options.Single(x => x.Name == "Visual Belo");
        var stunning = beauty.Options.Single(x => x.Name == "Deslumbrante");
        var ugly = beauty.Options.Single(x => x.Name == "Feio");
        beautiful.IsSelected = true;
        Assert.Equal(2, Assert.Single(advantages.Items).Rating);
        stunning.IsSelected = true;
        Assert.False(beautiful.IsSelected);
        Assert.Equal(4, Assert.Single(advantages.Items).Rating);
        ugly.IsSelected = true;
        Assert.Empty(advantages.Items);
        Assert.Equal(1, Assert.Single(flaws.Items).Rating);
        Assert.Empty(beauty.ConflictWarning);
        ugly.IsSelected = false;
        Assert.Empty(flaws.Items);
    }

    [Fact]
    public void Only_confirmed_options_are_exclusive_and_other_families_accumulate()
    {
        using var advantages = new TraitGroupViewModel(TraitCategory.Advantage, []);
        using var flaws = new TraitGroupViewModel(TraitCategory.Flaw, []);
        using var sheet = new AdvantageCatalogViewModel(_entries, advantages, flaws);
        var resources = sheet.Families.Single(x => x.Name == "Recursos");
        resources.Options.Single(x => x.Name == "Recursos").Rating = 5;
        resources.Options.Single(x => x.Name == "Destituído").IsSelected = true;
        Assert.Empty(advantages.Items);
        var members = sheet.Families.Single(x => x.Name == "Membros");
        members.Options.Single(x => x.Name == "Mawla").Rating = 3;
        members.Options.Single(x => x.Name == "Adversário").Rating = 4;
        members.Options.Single(x => x.Name == "Status").Rating = 5;
        members.Options.Single(x => x.Name == "Suspeito").IsSelected = true;
        members.Options.Single(x => x.Name == "Segregado").IsSelected = true;
        Assert.False(members.Options.Single(x => x.Name == "Status").IsSelected);
        Assert.False(members.Options.Single(x => x.Name == "Suspeito").IsSelected);
        Assert.Contains(advantages.Items, x => x.Name == "Mawla" && x.Rating == 3);
        Assert.Contains(flaws.Items, x => x.Name == "Adversário" && x.Rating == 4);
        foreach (var option in sheet.Families.Single(x => x.Name == "Acompanhando o Tempo").Options)
            option.IsSelected = true;
        Assert.Equal(2, sheet.Families.Single(x => x.Name == "Acompanhando o Tempo").Options.Count(x => x.IsSelected));
        Assert.All(sheet.Families, x => Assert.Empty(x.ConflictWarning));
    }

    [Fact]
    public void Existing_conflicts_are_visible_without_data_loss_and_personal_text_survives_new_selection()
    {
        using var advantages = new TraitGroupViewModel(TraitCategory.Advantage,
            [new(TraitCategory.Advantage, "catalog-deslumbrante", "Deslumbrante", 4)]);
        using var flaws = new TraitGroupViewModel(TraitCategory.Flaw,
            [new(TraitCategory.Flaw, "catalog-feio", "Feio", 1)]);
        using var sheet = new AdvantageCatalogViewModel(_entries, advantages, flaws);
        var beauty = sheet.Families.Single(x => x.Name == "Beleza");
        Assert.NotEmpty(beauty.ConflictWarning);
        Assert.Single(advantages.Items);
        Assert.Single(flaws.Items);
        beauty.AddFlawCommand.Execute(null);
        var custom = Assert.Single(beauty.PersonalEntries);
        custom.Name = "Cicatrizes";
        custom.Description = "Descricao pessoal preservada.";
        custom.Rating = 2;
        Assert.Empty(advantages.Items);
        Assert.Single(flaws.Items);
        Assert.Empty(beauty.ConflictWarning);
        beauty.Options.Single(x => x.Name == "Visual Belo").IsSelected = true;
        Assert.Equal(0, custom.Rating);
        Assert.Equal("Descricao pessoal preservada.", custom.ToValue().Merit!.Description);
        Assert.Contains(custom, beauty.PersonalEntries);
    }

    [Fact]
    public void Predator_grants_are_protected_and_block_incompatible_choices()
    {
        using var advantages = new TraitGroupViewModel(TraitCategory.Advantage,
            [new(TraitCategory.Advantage, "predator:test:0", "Deslumbrante", 4)]);
        using var flaws = new TraitGroupViewModel(TraitCategory.Flaw, []);
        using var sheet = new AdvantageCatalogViewModel(_entries, advantages, flaws);
        var beauty = sheet.Families.Single(x => x.Name == "Beleza");
        var grant = Assert.Single(beauty.PersonalEntries);
        Assert.False(grant.CanEditRating);
        beauty.RemovePersonalCommand.Execute(grant);
        Assert.Single(advantages.Items);
        beauty.Options.Single(x => x.Name == "Feio").IsSelected = true;
        Assert.Empty(flaws.Items);
        Assert.Contains("predador", beauty.Message);
        beauty.AddFlawCommand.Execute(null);
        var personal = beauty.PersonalEntries.Single(x => !x.IsPredatorGrant);
        personal.Rating = 2;
        Assert.Equal(0, personal.Rating);
        Assert.NotEmpty(beauty.Message);
        Assert.Null(grant.ToValue().Merit);
    }

    [Fact]
    public void Legacy_custom_entries_are_editable_and_keep_descriptions_in_other_characteristics()
    {
        using var advantages = new TraitGroupViewModel(TraitCategory.Advantage,
            [new(TraitCategory.Advantage, "old-custom", "Caracteristica antiga", 3)]);
        using var flaws = new TraitGroupViewModel(TraitCategory.Flaw, []);
        using var sheet = new AdvantageCatalogViewModel(_entries, advantages, flaws);
        var other = sheet.Families.Single(x => x.Name == "Outras caracteristicas");
        var item = Assert.Single(other.PersonalEntries);
        item.Description = "Texto novo";
        Assert.Equal(new MeritInfo(other.Name, "Texto novo"), item.ToValue().Merit);
        other.RemovePersonalCommand.Execute(item);
        Assert.Empty(advantages.Items);
        Assert.Empty(other.PersonalEntries);
    }

    [Fact]
    public void Predator_grants_with_personal_qualifiers_keep_their_related_family()
    {
        using var advantages = new TraitGroupViewModel(TraitCategory.Advantage,
            [new(TraitCategory.Advantage, "predator:test:0", "Contatos Criminosos", 3)]);
        using var flaws = new TraitGroupViewModel(TraitCategory.Flaw,
            [new(TraitCategory.Flaw, "predator:test:1", "Inimigo (dividas ou banimento)", 2),
             new(TraitCategory.Flaw, "predator:test:2", "Exclusao de Presas (locais sedentarios)", 1)]);
        using var sheet = new AdvantageCatalogViewModel(_entries, advantages, flaws);
        Assert.Equal(2, sheet.Families.Single(x => x.Name == "Mortais").PersonalEntries.Count);
        Assert.Single(sheet.Families.Single(x => x.Name == "Alimentação").PersonalEntries);
        Assert.Empty(sheet.Families.Single(x => x.Name == "Outras caracteristicas").PersonalEntries);
    }

    [Fact]
    public void Personal_metadata_has_explicit_category_and_length_validation()
    {
        var invalidSkill = new TraitValue(TraitCategory.Skill, "custom", "Teste", 1, Merit: new("Mortais", ""));
        Assert.Contains(CharacterRules.Validate(new(), [.. CharacterRules.DefaultAttributes, invalidSkill]),
            x => x.Message.Contains("devem pertencer a vantagens"));
        var invalidMerit = new TraitValue(TraitCategory.Advantage, "custom", "Teste", 1, Merit: new("", new string('x', 20001)));
        var issues = CharacterRules.Validate(new(), [.. CharacterRules.DefaultAttributes, invalidMerit]);
        Assert.Contains(issues, x => x.Field == "Trait.Merit.Group");
        Assert.Contains(issues, x => x.Field == "Trait.Merit.Description");
    }
}
