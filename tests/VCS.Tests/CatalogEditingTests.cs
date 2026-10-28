using VCS.Content.Advantages;
using VCS.Content.Clans;
using VCS.Content.Disciplines;
using VCS.Content.Skills;
using VCS.Infrastructure.Catalogs;
using VCS.Infrastructure.Persistence;
using VCS.Rules.V5.Clans;
using VCS.Sheet;

namespace VCS.Tests;

/// <summary>Clan edits change a process-wide catalog, so these tests run alone.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CatalogEditingCollection
{
    public const string Name = "Catalog editing";
}

[Collection(CatalogEditingCollection.Name)]
public sealed class CatalogEditingTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "VCS-catalogs-" + Guid.NewGuid().ToString("N"));
    private DatabaseAccess _database = null!;

    public async Task InitializeAsync()
    {
        _database = new DatabaseAccess(Path.Combine(_directory, "test.db"));
        await new SqliteDatabaseMaintenance(_database).InitializeAsync();
    }

    public Task DisposeAsync()
    {
        ClanCatalog.Configure([]);
        Directory.Delete(_directory, true);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Skills_can_be_added_edited_reset_and_only_user_ones_deleted()
    {
        var catalog = new SqliteSkillCatalog(_database);
        await catalog.SaveAsync(new SkillDefinition("", "Navegacao", "Mentais", "Cartas e rotas."));
        var added = Assert.Single(await catalog.ListEntriesAsync(), x => x.Value.Name == "Navegacao");
        Assert.False(added.IsBuiltIn);
        Assert.StartsWith("skill-custom-", added.Id);

        var melee = (await catalog.ListEntriesAsync()).Single(x => x.Id == "skill-melee");
        await catalog.SaveAsync(melee.Value with { Description = "Editada." });
        Assert.True((await catalog.ListEntriesAsync()).Single(x => x.Id == "skill-melee").IsModified);
        await Assert.ThrowsAsync<InvalidOperationException>(() => catalog.DeleteAsync("skill-melee"));
        await catalog.ResetAsync("skill-melee");
        Assert.False((await catalog.ListEntriesAsync()).Single(x => x.Id == "skill-melee").IsModified);

        await Assert.ThrowsAsync<InvalidOperationException>(() => catalog.SaveAsync(new SkillDefinition("", "", "Mentais", "")));
        await catalog.DeleteAsync(added.Id);
        Assert.DoesNotContain(await catalog.ListAsync(), x => x.Name == "Navegacao");
    }

    [Fact]
    public async Task Advantages_validate_points_and_track_edits_of_built_in_entries()
    {
        var catalog = new SqliteAdvantageCatalog(_database);
        await catalog.SaveAsync(new AdvantageDefinition
        {
            Name = "Contatos no porto", Group = "Pessoais", Category = TraitCategory.Advantage, Minimum = 1, Maximum = 3
        });
        Assert.Contains(await catalog.ListAsync(), x => x.Name == "Contatos no porto");
        await Assert.ThrowsAsync<InvalidOperationException>(() => catalog.SaveAsync(new AdvantageDefinition
        {
            Name = "Invalida", Group = "Pessoais", Category = TraitCategory.Advantage, Minimum = 3, Maximum = 2
        }));
        var builtIn = (await catalog.ListEntriesAsync()).First(x => x.IsBuiltIn);
        builtIn.Value.Description = "Outra descricao";
        await catalog.SaveAsync(builtIn.Value);
        Assert.True((await catalog.ListEntriesAsync()).Single(x => x.Id == builtIn.Id).IsModified);
        await catalog.ResetAsync(builtIn.Id);
        Assert.False((await catalog.ListEntriesAsync()).Single(x => x.Id == builtIn.Id).IsModified);
    }

    [Fact]
    public async Task New_disciplines_hold_new_powers_and_built_in_edits_apply_to_the_running_catalog()
    {
        var disciplines = new SqliteDisciplineCatalog(_database);
        await disciplines.SaveAsync(new DisciplineDefinition("", "Quimerismo"));
        var chimerstry = DisciplineCatalog.Find("quimerismo")!;
        Assert.StartsWith("discipline-custom-", chimerstry.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => disciplines.SaveAsync(new DisciplineDefinition("", "Celeridade")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => disciplines.SaveAsync(new DisciplineDefinition(DisciplineCatalog.BloodSorcery, "Taumaturgia")));

        var provided = new ProvidedDisciplineCatalog();
        var catalog = new SqlitePowerCatalog(_database, provided);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            catalog.SaveAsync(new PowerDefinition("", "Quimerismo", "Sem disciplina", 1, "Nenhum", "Nenhuma", "Nome em vez de id.")));
        await catalog.SaveAsync(new PowerDefinition("", chimerstry.Id, "Ilusao menor", 1, "Nenhum", "Carisma + Quimerismo", "Cria uma ilusao."));
        var illusion = provided.Powers.Single(x => x.Name == "Ilusao menor");
        Assert.Equal("Quimerismo", illusion.Discipline);
        await Assert.ThrowsAsync<InvalidOperationException>(() => disciplines.DeleteAsync(chimerstry.Id));

        var original = provided.BuiltIn[0];
        await catalog.SaveAsync(original with { Summary = "Resumo editado." });
        Assert.Equal("Resumo editado.", provided.Powers.Single(x => x.Id == original.Id).Summary);
        Assert.True((await catalog.ListEntriesAsync()).Single(x => x.Id == original.Id).IsModified);
        await Assert.ThrowsAsync<InvalidOperationException>(() => catalog.DeleteAsync(original.Id));
        await catalog.ResetAsync(original.Id);
        Assert.Equal(original, provided.Powers.Single(x => x.Id == original.Id));

        var reloaded = new ProvidedDisciplineCatalog();
        await new SqlitePowerCatalog(_database, reloaded).RefreshAsync();
        Assert.Contains(reloaded.Powers, x => x.Name == "Ilusao menor");

        await catalog.DeleteAsync(illusion.Id);
        await disciplines.DeleteAsync(chimerstry.Id);
        Assert.Null(DisciplineCatalog.Find("Quimerismo"));
    }

    [Fact]
    public async Task Clans_can_be_added_and_built_in_clans_edited_then_reset_for_the_rules()
    {
        var catalog = new SqliteClanCatalog(_database);
        await catalog.SaveAsync(new ClanDefinition("", "Ahrimanes", "Linhagem de magos xamas.", ["discipline-animalism", "discipline-presence"], []));
        var ahrimanes = ClanCatalog.Find("ahrimanes")!;
        Assert.StartsWith("clan-custom-", ahrimanes.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => catalog.SaveAsync(new ClanDefinition("", "Brujah", "", [], [])));
        Assert.Equal(["discipline-animalism", "discipline-presence"], ClanCatalog.Find("ahrimanes")!.Disciplines);
        Assert.True(DisciplineAdvancement.IsInClan("Ahrimanes", "Animalismo"));

        var brujah = ClanCatalog.Find("Brujah")!;
        await catalog.SaveAsync(brujah with { Disciplines = ["discipline-celerity", "discipline-potence", "discipline-obfuscate"] }, brujah.Id);
        Assert.True(DisciplineAdvancement.IsInClan("Brujah", "Ofuscação"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => catalog.SaveAsync(brujah with { Name = "Rebeldes" }, brujah.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => catalog.DeleteAsync("clan-brujah"));
        await catalog.ResetAsync("clan-brujah");
        Assert.False(DisciplineAdvancement.IsInClan("Brujah", "Ofuscação"));

        await catalog.SaveAsync(ahrimanes with { Name = "Ahrimanes Renascidos", Disciplines = ["discipline-animalism"] }, ahrimanes.Id);
        Assert.Null(ClanCatalog.Find("Ahrimanes"));
        Assert.Equal(ahrimanes.Id, ClanCatalog.Find("Ahrimanes Renascidos")!.Id);
        await catalog.DeleteAsync(ahrimanes.Id);
        Assert.Equal(ClanCatalog.BuiltIn.Count, ClanCatalog.All.Count);
    }
}
