using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VCS.Application.Characters;
using VCS.Infrastructure.Catalogs;
using VCS.Infrastructure.Characters;
using VCS.Content;
using VCS.Infrastructure.Persistence;
using VCS.Rules.V5.Validation;
using VCS.Sheet;

namespace VCS.Tests;

/// <summary>Sheets and catalog edits saved with the old pt-BR ids keep working after the switch to English ids.</summary>
public sealed class ContentIdUpgradeTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "VCS-tests-" + Guid.NewGuid().ToString("N"));
    private DatabaseAccess _database = null!;
    private CharacterService _service = null!;
    private SqliteDatabaseMaintenance _maintenance = null!;

    public async Task InitializeAsync()
    {
        _database = new DatabaseAccess(Path.Combine(_directory, "test.db"));
        _service = new CharacterService(new SqliteCharacterRepository(_database));
        _maintenance = new SqliteDatabaseMaintenance(_database);
        await _maintenance.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        return Task.CompletedTask;
    }

    private static TraitValue[] OldTraits() =>
    [
        new(TraitCategory.Advantage, "catalog-visual-belo", "Visual Belo", 2, Merit: new("Visual", "")),
        new(TraitCategory.Flaw, "catalog-feio", "Feio", 1, Merit: new("Visual", "")),
        new(TraitCategory.Advantage, "adv-custom-0123456789abcdef0123456789abcdef", "Vantagem propria", 1, Merit: new("Outros", ""))
    ];

    private const string Personal = "0123456789abcdef0123456789abcdef";

    // Sheet lines saved with current keys, turned back into the keys older versions wrote.
    private static TraitValue[] CurrentTraits() =>
    [
        new(TraitCategory.Discipline, "discipline-celerity", "Celeridade", 1),
        new(TraitCategory.DisciplinePower, "power-celerity-rapid-reflexes", "Reflexos Rápidos", 1),
        new(TraitCategory.Skill, "skill-personal-" + Personal, "Pesquisa", 2, Skill: new("Mentais")),
        new(TraitCategory.Advantage, "advantage-personal-" + Personal, "Sapateiro", 1, Merit: new("Outros", ""))
    ];

    private static readonly (string From, string To)[] OldKeys =
    [
        ("Strength", "attribute-strength"), ("Dexterity", "attribute-dexterity"), ("Stamina", "attribute-stamina"),
        ("Charisma", "attribute-charisma"), ("Manipulation", "attribute-manipulation"), ("Composure", "attribute-composure"),
        ("Intelligence", "attribute-intelligence"), ("Wits", "attribute-wits"), ("Resolve", "attribute-resolve"),
        (CatalogText.StableId("disc-", "Celeridade"), "discipline-celerity"),
        (CatalogText.StableId("power-", "Celeridade\nReflexos Rápidos"), "power-celerity-rapid-reflexes"),
        (Personal, "skill-personal-" + Personal), (Personal, "advantage-personal-" + Personal)
    ];

    [Fact]
    public async Task Old_database_gets_english_ids_and_keeps_catalog_edits_and_revisions()
    {
        var created = await _service.CreateAsync();
        var saved = await _service.SaveAsync(created with { Traits = [.. created.Traits, .. OldTraits(), .. CurrentTraits()] });
        var catalog = new SqliteAdvantageCatalog(_database);
        var beautiful = (await catalog.ListAsync()).Single(x => x.Id == "advantage-looks-beautiful");
        beautiful.Description = "Editada pelo usuario.";
        await catalog.SaveAsync(beautiful);
        var powers = new SqlitePowerCatalog(_database, new ProvidedDisciplineCatalog());
        var reflexes = (await powers.ListEntriesAsync()).Single(x => x.Id == "power-celerity-rapid-reflexes").Value;
        await powers.SaveAsync(reflexes with { Summary = "Poder editado." });
        await using (var db = _database.CreateContext())
            await db.GetService<IMigrator>().MigrateAsync("20261025140036_RenameBloodCheckAndResolve");
        await using (var db = _database.CreateContext())
        {
            Assert.Equal("Editada pelo usuario.", (await db.AdvantageCatalog.SingleAsync(x => x.Id == "catalog-visual-belo")).Description);
            // Keys the sheet held before they were renamed in the code.
            foreach (var (from, to) in OldKeys)
                await db.Database.ExecuteSqlAsync($"UPDATE \"CharacterTraits\" SET \"Key\" = {from} WHERE \"Key\" = {to}");
        }

        await _maintenance.InitializeAsync();

        var upgraded = await _service.GetAsync(saved.Id);
        Assert.Equal(saved.Revision, upgraded.Revision);
        Assert.Empty(CharacterRules.Validate(upgraded.Details, upgraded.Traits));
        var renamed = new Dictionary<string, string>
        {
            ["catalog-visual-belo"] = "advantage-looks-beautiful", ["catalog-feio"] = "flaw-looks-ugly",
            ["adv-custom-0123456789abcdef0123456789abcdef"] = "advantage-custom-0123456789abcdef0123456789abcdef"
        };
        Assert.Equal(saved.Traits.Select(x => renamed.GetValueOrDefault(x.Key, x.Key)).Order(), upgraded.Traits.Select(x => x.Key).Order());
        Assert.Equal("Editada pelo usuario.", (await catalog.ListAsync()).Single(x => x.Id == "advantage-looks-beautiful").Description);
        Assert.Equal("Poder editado.", (await powers.ListEntriesAsync()).Single(x => x.Id == "power-celerity-rapid-reflexes").Value.Summary);
    }

    [Fact]
    public async Task Old_json_file_gets_english_ids_on_import()
    {
        var created = await _service.CreateAsync();
        var path = Path.Combine(_directory, "old.json");
        var files = new JsonCharacterFiles();
        await files.ExportAsync(created, path);
        var json = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        json["FormatVersion"] = 12;
        var traits = json["Character"]!["Traits"]!.AsArray();
        traits.Add(JsonNode.Parse("""{"Category":"Advantage","Key":"catalog-visual-belo","Name":"Visual Belo","Rating":2,"Merit":{"Group":"Visual","Description":""}}"""));
        traits.Add(JsonNode.Parse("""{"Category":"Loresheet","Key":"lore-heresia-cainita-1","Name":"Que Ele tenha Compreensão","Rating":1,"Loresheet":{"Collection":"Heresia Cainita","IsLineage":false,"Description":"Beneficio.","Payment":0}}"""));
        await File.WriteAllTextAsync(path, json.ToJsonString());

        var read = await files.ReadAsync(path);

        Assert.Contains(read.Traits, x => x.Key == "advantage-looks-beautiful");
        Assert.Contains(read.Traits, x => x.Key == "loresheet-cainite-heresy-1");
    }
}
