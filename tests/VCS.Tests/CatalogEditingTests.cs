using VCS.Application.Catalogs;
using VCS.Domain.Advantages;
using VCS.Domain.Characters;
using VCS.Domain.Clans;
using VCS.Domain.Skills;
using VCS.Infrastructure.Catalogs;
using VCS.Infrastructure.Persistence;

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
    public async Task New_powers_create_disciplines_and_built_in_edits_apply_to_the_running_catalog()
    {
        var provided = new ProvidedDisciplineCatalog();
        var catalog = new SqlitePowerCatalog(_database, provided);
        await catalog.SaveAsync(new PowerDefinition("", "Quimerismo", "Ilusao menor", 1, "Nenhum", "Carisma + Quimerismo", "Cria uma ilusao."));
        Assert.Contains(provided.Powers, x => x.Discipline == "Quimerismo" && x.Name == "Ilusao menor");

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
    }

    [Fact]
    public async Task Clans_can_be_added_and_built_in_clans_edited_then_reset_for_the_rules()
    {
        var catalog = new SqliteClanCatalog(_database);
        await catalog.SaveAsync(new ClanDefinition("Ahrimanes", "Linhagem de magos xamas.", ["Animalismo", "Presenca"], []));
        Assert.Equal(["Animalismo", "Presenca"], ClanCatalog.Find("ahrimanes")!.Disciplines);
        Assert.True(DisciplineAdvancement.IsInClan("Ahrimanes", "Animalismo"));

        var brujah = ClanCatalog.Find("Brujah")!;
        await catalog.SaveAsync(brujah with { Disciplines = ["Celeridade", "Potência", "Ofuscação"] }, "Brujah");
        Assert.True(DisciplineAdvancement.IsInClan("Brujah", "Ofuscação"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => catalog.SaveAsync(brujah with { Name = "Rebeldes" }, "Brujah"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => catalog.DeleteAsync("Brujah"));
        await catalog.ResetAsync("Brujah");
        Assert.False(DisciplineAdvancement.IsInClan("Brujah", "Ofuscação"));

        await catalog.SaveAsync(new ClanDefinition("Ahrimanes Renascidos", "", ["Animalismo"], []), "Ahrimanes");
        Assert.Null(ClanCatalog.Find("Ahrimanes"));
        Assert.NotNull(ClanCatalog.Find("Ahrimanes Renascidos"));
        await catalog.DeleteAsync("Ahrimanes Renascidos");
        Assert.Equal(ClanCatalog.BuiltIn.Count, ClanCatalog.All.Count);
    }
}
