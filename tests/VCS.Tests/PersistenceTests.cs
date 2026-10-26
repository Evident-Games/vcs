using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VCS.Application.Characters;
using VCS.Infrastructure.Catalogs;
using VCS.Infrastructure.Characters;
using VCS.Infrastructure.Persistence;
using VCS.Rules.V5.Validation;

namespace VCS.Tests;

public sealed class PersistenceTests : IAsyncLifetime
{
    [Fact]
    public void Provisional_database_has_its_own_location()
    {
        var expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VCS", "Provisorio", "vcs.db");
        Assert.Equal(expected, DatabaseAccess.DefaultPath);
        Assert.NotEqual(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VCS", "vcs.db"), DatabaseAccess.DefaultPath);
    }

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
        Directory.Delete(_directory, true);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Upgrade_renames_blood_check_and_resolve_in_saved_powers()
    {
        var created = await _service.CreateAsync();
        var power = new VCS.Sheet.TraitValue(VCS.Sheet.TraitCategory.DisciplinePower, "p", "Visao", 1,
            Power: new("Auspícios", "1 Teste de Incitar o Sangue", "Perseverança + Auspícios"));
        await _service.SaveAsync(created with { Traits = [.. created.Traits, power] });
        await using (var db = _database.CreateContext())
            await db.GetService<IMigrator>().MigrateAsync("20261025130648_EditableClansAndPowers");
        await _maintenance.InitializeAsync();
        var saved = (await _service.GetAsync(created.Id)).Traits.Single(x => x.Key == "p").Power!;
        Assert.Equal("1 checagem de sangue", saved.Cost);
        Assert.Equal("Determinação + Auspícios", saved.DicePool);
    }

    [Fact]
    public async Task Save_warm_up_rolls_back_and_leaves_data_unchanged()
    {
        await _service.WarmUpAsync();
        var created = await _service.CreateAsync();
        var draft = created with { Details = created.Details with { Name = "Antes" } };
        var saved = await _service.SaveAsync(draft);
        await _service.WarmUpAsync();
        var reloaded = await _service.GetAsync(saved.Id);
        Assert.Equal(saved.Revision, reloaded.Revision);
        Assert.Equal(saved.UpdatedUtc, reloaded.UpdatedUtc);
        Assert.Equal("Antes", reloaded.Details.Name);
        Assert.Equal(saved.Traits.Length, reloaded.Traits.Length);
        var next = await _service.SaveAsync(reloaded with { Details = reloaded.Details with { Name = "Depois" } });
        Assert.Equal(saved.Revision + 1, next.Revision);
    }

    [Fact]
    public async Task Migration_crud_search_and_cascade_work()
    {
        var created = await _service.CreateAsync();
        var saved = await _service.SaveAsync(created with
        {
            Details = created.Details with { Name = "Alice", Clan = "Meu cla", Chronicle = "Lisboa" },
            Traits = [.. created.Traits, new(VCS.Sheet.TraitCategory.Skill, "custom", "Pesquisa pessoal", 3)]
        });
        Assert.Equal(2, saved.Revision);
        Assert.Single(await _service.ListAsync("ALICE"));
        Assert.Single(await _service.ListAsync("CLA"));
        Assert.Single(await _service.ListAsync("Lisboa"));
        Assert.Empty(await _service.ListAsync("ausente"));
        Assert.Equal(10, (await _service.GetAsync(saved.Id)).Traits.Length);
        var copy = await _service.DuplicateAsync(saved.Id);
        Assert.NotEqual(saved.Id, copy.Id);
        await _service.DeleteAsync((await _service.ListAsync("Alice")).Single(x => x.Id == saved.Id));
        Assert.Single(await _service.ListAsync());
        await using var db = _database.CreateContext();
        Assert.Equal(10, await db.Set<VCS.Sheet.CharacterTrait>().CountAsync());
        Assert.Equal(19, (await db.Database.GetAppliedMigrationsAsync()).Count());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task Initialization_can_be_repeated_without_losing_characters()
    {
        var created = await _service.CreateAsync();
        await _maintenance.InitializeAsync();
        await _maintenance.InitializeAsync();
        var reopened = await _service.GetAsync(created.Id);
        Assert.Equal(created.Details, reopened.Details);
        Assert.Equal(created.Revision, reopened.Revision);
        Assert.Equal(created.CreatedUtc, reopened.CreatedUtc);
        Assert.Equal(created.Traits.OrderBy(x => x.Key), reopened.Traits.OrderBy(x => x.Key));
        Assert.Single(await _service.ListAsync());
    }

    [Fact]
    public async Task Stale_save_and_delete_are_rejected()
    {
        var created = await _service.CreateAsync();
        await _service.SaveAsync(created with { Details = created.Details with { Name = "Editado" } });
        await Assert.ThrowsAsync<CharacterConflictException>(() => _service.SaveAsync(created));
        await Assert.ThrowsAsync<CharacterConflictException>(() => _service.DeleteAsync(
            new(created.Id, "", "", "", created.UpdatedUtc, created.Revision)));
        Assert.Equal("Editado", (await _service.GetAsync(created.Id)).Details.Name);
    }

    [Fact]
    public async Task Repository_conflict_rolls_back_trait_deletion()
    {
        var created = await _service.CreateAsync();
        var repository = new SqliteCharacterRepository(_database);
        var first = (await repository.GetAsync(created.Id))!;
        var stale = (await repository.GetAsync(created.Id))!;
        CharacterRules.Update(first, first.Details with { Name = "Primeiro" }, first.Traits.Select(x => x.ToValue()).ToArray());
        await repository.SaveAsync(first, created.Revision);
        CharacterRules.Update(stale, stale.Details with { Name = "Obsoleto" }, stale.Traits.Select(x => x.ToValue()).ToArray());
        await Assert.ThrowsAsync<CharacterConflictException>(() => repository.SaveAsync(stale, created.Revision));
        var saved = await _service.GetAsync(created.Id);
        Assert.Equal("Primeiro", saved.Details.Name);
        Assert.Equal(9, saved.Traits.Length);
        Assert.Equal(2, saved.Revision);
    }

    [Fact]
    public async Task Json_roundtrip_imports_as_new_character()
    {
        var created = await _service.CreateAsync();
        var files = new JsonCharacterFiles();
        var path = Path.Combine(_directory, "character.json");
        await files.ExportAsync(created, path);
        var imported = await _service.ImportAsync(await files.ReadAsync(path));
        Assert.NotEqual(created.Id, imported.Id);
        Assert.Equal(created.Details, imported.Details);
        Assert.Equal(created.Traits, imported.Traits);
        await File.WriteAllTextAsync(path, """{"FormatVersion":99,"Character":null}""");
        await Assert.ThrowsAsync<InvalidDataException>(() => files.ReadAsync(path));
    }

    [Fact]
    public async Task Backup_restore_preserves_snapshot_and_creates_safety_copy()
    {
        var original = await _service.CreateAsync();
        var path = Path.Combine(_directory, "backup.db");
        await _maintenance.BackupAsync(path);
        await _service.SaveAsync(original with { Details = original.Details with { Name = "Depois" } });
        var safety = await _maintenance.RestoreAsync(path);
        Assert.True(File.Exists(safety));
        Assert.Equal("Novo personagem", (await _service.GetAsync(original.Id)).Details.Name);
        await _maintenance.RestoreAsync(safety);
        Assert.Equal("Depois", (await _service.GetAsync(original.Id)).Details.Name);
    }

    [Fact]
    public async Task Invalid_backup_does_not_change_live_data()
    {
        var original = await _service.CreateAsync();
        var path = Path.Combine(_directory, "invalid.db");
        await File.WriteAllTextAsync(path, "not sqlite");
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => _maintenance.RestoreAsync(path));
        Assert.Equal(original.Details, (await _service.GetAsync(original.Id)).Details);
        await Assert.ThrowsAsync<IOException>(() => _maintenance.BackupAsync(_database.Path));
    }

    [Fact]
    public async Task Backup_from_future_schema_is_rejected_without_overwriting_live_database()
    {
        var original = await _service.CreateAsync();
        var path = Path.Combine(_directory, "future.db");
        await _maintenance.BackupAsync(path);
        await using (var db = _database.CreateContext(path))
        {
            await db.Database.ExecuteSqlRawAsync(
                """INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('99999999999999_Future', '99.0.0')""");
        }
        await Assert.ThrowsAsync<InvalidDataException>(() => _maintenance.RestoreAsync(path));
        var futureMaintenance = new SqliteDatabaseMaintenance(new DatabaseAccess(path));
        await Assert.ThrowsAsync<InvalidDataException>(() => futureMaintenance.InitializeAsync());
        Assert.Equal(original.Details, (await _service.GetAsync(original.Id)).Details);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("""{"FormatVersion":1,"Character":null}""")]
    [InlineData("""{"FormatVersion":1,"Character":{"Details":null,"Traits":[]}}""")]
    [InlineData("""{"FormatVersion":1,"Character":{"Details":{},"Traits":null}}""")]
    [InlineData("""{"FormatVersion":1,"Character":{"Details":{},"Traits":[null]}}""")]
    public async Task Invalid_json_shapes_are_rejected(string json)
    {
        var path = Path.Combine(_directory, "invalid.json");
        await File.WriteAllTextAsync(path, json);
        await Assert.ThrowsAsync<InvalidDataException>(() => new JsonCharacterFiles().ReadAsync(path));
        Assert.Empty(await _service.ListAsync());
    }

    [Fact]
    public async Task Oversized_and_unknown_json_are_rejected()
    {
        var files = new JsonCharacterFiles();
        var path = Path.Combine(_directory, "invalid.json");
        await File.WriteAllBytesAsync(path, new byte[5 * 1024 * 1024 + 1]);
        await Assert.ThrowsAsync<InvalidDataException>(() => files.ReadAsync(path));
        await File.WriteAllTextAsync(path, """{"FormatVersion":1,"Character":null,"Unknown":true}""");
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => files.ReadAsync(path));
    }

    [Fact]
    public async Task Selected_powers_survive_duplicate_json_backup_restore_and_removal()
    {
        var power = new ProvidedDisciplineCatalog().Powers.First(x => !x.IsRitual);
        var ritual = new ProvidedDisciplineCatalog().Powers.First(x => x.IsRitual);
        var created = await _service.CreateAsync();
        var saved = await _service.SaveAsync(created with
        {
            Traits = [.. created.Traits,
                new(VCS.Sheet.TraitCategory.DisciplinePower, power.Id, power.Name, power.Level),
                new(VCS.Sheet.TraitCategory.DisciplinePower, ritual.Id, ritual.Name, ritual.Level)]
        });
        var expected = saved.Traits.Where(x => x.Category == VCS.Sheet.TraitCategory.DisciplinePower).OrderBy(x => x.Key).ToArray();
        var copy = await _service.DuplicateAsync(saved.Id);
        Assert.Equal(expected, copy.Traits.Where(x => x.Category == VCS.Sheet.TraitCategory.DisciplinePower).OrderBy(x => x.Key));
        var jsonPath = Path.Combine(_directory, "powers.json");
        var files = new JsonCharacterFiles();
        await files.ExportAsync(saved, jsonPath);
        Assert.Contains("\"FormatVersion\": 13", await File.ReadAllTextAsync(jsonPath));
        var imported = await _service.ImportAsync(await files.ReadAsync(jsonPath));
        Assert.Equal(expected, imported.Traits.Where(x => x.Category == VCS.Sheet.TraitCategory.DisciplinePower).OrderBy(x => x.Key));
        var backupPath = Path.Combine(_directory, "powers.db");
        await _maintenance.BackupAsync(backupPath);
        await _service.SaveAsync(saved with { Traits = created.Traits });
        Assert.DoesNotContain((await _service.GetAsync(saved.Id)).Traits, x => x.Category == VCS.Sheet.TraitCategory.DisciplinePower);
        await _maintenance.RestoreAsync(backupPath);
        var restored = await _service.GetAsync(saved.Id);
        Assert.Equal(expected, restored.Traits.Where(x => x.Category == VCS.Sheet.TraitCategory.DisciplinePower).OrderBy(x => x.Key));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Legacy_json_without_extended_fields_still_imports(int version)
    {
        var created = await _service.CreateAsync();
        var path = Path.Combine(_directory, "legacy.json");
        var files = new JsonCharacterFiles();
        await files.ExportAsync(created, path);
        var json = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        json["FormatVersion"] = version;
        var details = json["Character"]!["Details"]!.AsObject();
        string[] originalFields =
        [
            "Name", "MortalName", "Clan", "Generation", "Sire", "Predator", "Concept", "Chronicle", "Description",
            "Notes", "Hunger", "Humanity", "BloodPotency", "HealthSuperficial", "HealthAggravated",
            "WillpowerSuperficial", "WillpowerAggravated"
        ];
        foreach (var key in details.Select(x => x.Key).Except(originalFields).ToArray()) details.Remove(key);
        await File.WriteAllTextAsync(path, json.ToJsonString());
        var imported = await _service.ImportAsync(await files.ReadAsync(path));
        Assert.Equal(created.Details with { Advancement = null }, imported.Details);
        Assert.Equal(9, imported.Traits.Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Existing_initial_schema_is_upgraded_without_losing_character(bool restoreBackup)
    {
        var created = await _service.CreateAsync();
        await using (var db = _database.CreateContext())
        {
            await db.GetService<IMigrator>().MigrateAsync("20261017194113_InitialCreate");
        }
        if (restoreBackup)
        {
            var backup = Path.Combine(_directory, "legacy.db");
            await _maintenance.BackupAsync(backup);
            await _maintenance.InitializeAsync();
            await _service.SaveAsync(created with { Details = created.Details with { Name = "Depois" } });
            await _maintenance.RestoreAsync(backup);
        }
        else await _maintenance.InitializeAsync();
        var upgraded = await _service.GetAsync(created.Id);
        Assert.Equal(created.Id, upgraded.Id);
        Assert.Equal(created.Details.Name, upgraded.Details.Name);
        Assert.Equal(created.Traits.OrderBy(x => x.Key), upgraded.Traits.OrderBy(x => x.Key));
        Assert.Equal("", upgraded.Details.Ambition);
        Assert.Equal("", upgraded.Details.History);
        Assert.Equal(0, upgraded.Details.ExperienceTotal);
        Assert.Equal(63, (await new SqliteAdvantageCatalog(_database).ListAsync()).Count);
    }

    [Fact]
    public async Task Seeded_advantages_have_exact_names_costs_and_survive_repeated_initialization()
    {
        var catalog = new SqliteAdvantageCatalog(_database);
        var entries = await catalog.ListAsync();
        Assert.Equal(63, entries.Count);
        Assert.Equal(18, entries.Count(x => x.Category == VCS.Sheet.TraitCategory.Advantage));
        Assert.Equal(25, entries.Count(x => x.Category == VCS.Sheet.TraitCategory.Flaw));
        Assert.Equal(63, entries.Select(x => x.Id).Distinct().Count());
        Assert.Equal(11, entries.Select(x => x.Group).Distinct().Count());
        Assert.Equal(20, entries.Count(x => x.Category == VCS.Sheet.TraitCategory.Loresheet));
        Assert.All(entries, x =>
        {
            Assert.InRange(x.Minimum, 1, 5);
            Assert.InRange(x.Maximum, x.Minimum, 5);
            Assert.NotEmpty(x.Description);
            Assert.NotEmpty(x.PointsLabel);
        });
        var visual = Assert.Single(entries, x => x.Name == "Visual Belo");
        Assert.Equal(2, visual.Minimum);
        Assert.Equal(2, visual.Maximum);
        Assert.Equal("Adiciona +1 dado em todos os testes Sociais do personagem.", visual.Description);
        var enemy = Assert.Single(entries, x => x.Name == "Inimigo");
        Assert.Equal("1+", enemy.PointsLabel);
        Assert.Equal(5, enemy.Maximum);
        var haven = Assert.Single(entries, x => x.Name == "Refúgio");
        Assert.Equal(1, haven.Minimum);
        Assert.Equal(3, haven.Maximum);
        await _maintenance.InitializeAsync();
        await _maintenance.InitializeAsync();
        Assert.Equal(63, (await catalog.ListAsync()).Count);
    }

    [Fact]
    public async Task Catalog_selections_roundtrip_without_changing_other_character_values()
    {
        var entries = await new SqliteAdvantageCatalog(_database).ListAsync();
        var original = await _service.CreateAsync();
        var saved = await _service.SaveAsync(original with
        {
            Traits = [.. original.Traits, .. entries.Where(x => x.Category != VCS.Sheet.TraitCategory.Loresheet)
                .Select(x => new VCS.Sheet.TraitValue(x.Category, x.Id, x.Name, x.Maximum))]
        });
        Assert.Equal(original.Details, saved.Details);
        var copy = await _service.DuplicateAsync(saved.Id);
        Assert.Equal(saved.Traits.OrderBy(x => x.Key), copy.Traits.OrderBy(x => x.Key));
        var files = new JsonCharacterFiles();
        var path = Path.Combine(_directory, "advantages.json");
        await files.ExportAsync(saved, path);
        var imported = await _service.ImportAsync(await files.ReadAsync(path));
        Assert.Equal(saved.Traits.OrderBy(x => x.Key), imported.Traits.OrderBy(x => x.Key));
        var backup = Path.Combine(_directory, "advantages.db");
        await _maintenance.BackupAsync(backup);
        await _service.SaveAsync(saved with { Traits = original.Traits });
        await _maintenance.RestoreAsync(backup);
        Assert.Equal(saved.Traits.OrderBy(x => x.Key), (await _service.GetAsync(saved.Id)).Traits.OrderBy(x => x.Key));
        Assert.Equal(63, (await new SqliteAdvantageCatalog(_database).ListAsync()).Count);
    }

    [Fact]
    public async Task Extended_fields_survive_duplicate_json_and_restore()
    {
        var created = await _service.CreateAsync();
        var details = created.Details with
        {
            Ambition = "Ambicao", Desire = "Desejo", Resonance = "Ressonancia", ChronicleTenets = "Principios",
            Touchstones = "Pilares", Convictions = "Conviccoes", ClanBane = "Perdicao", TrueAge = "120",
            ApparentAge = "30", BirthDate = "1900", DeathDate = "1930", Appearance = "Aparencia",
            DistinguishingFeatures = "Marcas", History = "Historia", BloodSurge = "2", DamageMend = "2",
            PowerBonus = "1", RouseReroll = "1", FeedingPenalty = "Nenhuma", BaneSeverity = "2",
            ExperienceTotal = 30, ExperienceSpent = 10, Advancement = null
        };
        var saved = await _service.SaveAsync(created with { Details = details });
        Assert.Equal(details with { Name = details.Name + " (copia)" }, (await _service.DuplicateAsync(saved.Id)).Details);
        var path = Path.Combine(_directory, "extended.json");
        var files = new JsonCharacterFiles();
        await files.ExportAsync(saved, path);
        Assert.Equal(details, (await _service.ImportAsync(await files.ReadAsync(path))).Details);
        var backup = Path.Combine(_directory, "extended.db");
        await _maintenance.BackupAsync(backup);
        await _service.SaveAsync(saved with { Details = details with { History = "Alterada" } });
        await _maintenance.RestoreAsync(backup);
        Assert.Equal(details, (await _service.GetAsync(saved.Id)).Details);
    }
}
