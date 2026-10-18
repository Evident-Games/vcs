using VCS.Desktop.ViewModels;
using VCS.Domain.Characters;
using VCS.Infrastructure.Persistence;
using VCS.Infrastructure.Repositories;
using VCS.Infrastructure.Files;

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
