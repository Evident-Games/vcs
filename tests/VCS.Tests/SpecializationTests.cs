using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;
using VCS.Application.Characters;
using VCS.Desktop.Sheet;
using VCS.Infrastructure.Catalogs;
using VCS.Infrastructure.Characters;
using VCS.Infrastructure.Persistence;
using VCS.Rules.V5.Validation;
using VCS.Sheet;

namespace VCS.Tests;

public sealed class SpecializationTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "VCS-specialization-" + Guid.NewGuid().ToString("N"));
    private DatabaseAccess _database = null!;
    private CharacterService _service = null!;
    public async Task InitializeAsync()
    {
        _database = new(Path.Combine(_directory, "test.db"));
        await new SqliteDatabaseMaintenance(_database).InitializeAsync();
        _service = new(new SqliteCharacterRepository(_database));
    }
    public Task DisposeAsync() { Directory.Delete(_directory, true); return Task.CompletedTask; }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Specializations_follow_autosave_duplicate_json_backup_and_pdf_for_both_sheets(bool npc)
    {
        var dto = npc ? await _service.CreateNpcAsync() : await _service.CreateAsync();
        var autosave = new ManualAutosave();
        using var editor = new CharacterEditorViewModel(dto, _service, NullLogger<CharacterEditorViewModel>.Instance,
            autosave, new ProvidedDisciplineCatalog());
        editor.ConfigureSkillCatalog(await new SqliteSkillCatalog(_database).ListAsync());
        var melee = editor.Skills.Items.Single(x => x.Name == "Armas Brancas");
        melee.Specialization = "Esgrima / estacas";
        var personal = editor.SkillGroups.Single(x => x.Name == "Físicas").PersonalItems.Single(x => x.IsDraft);
        personal.Name = "Habilidade pessoal";
        personal.Rating = 2;
        personal.Specialization = "Especialização pessoal";
        Assert.Single(editor.SkillGroups.Single(x => x.Name == "Físicas").PersonalItems, x => x.IsDraft);
        Assert.True(editor.IsDirty);
        await autosave.FireAsync();
        Assert.False(editor.IsDirty);
        var saved = await _service.GetAsync(editor.Id);
        Assert.Equal("Esgrima / estacas", saved.Traits.Single(x => x.Key == melee.Key).Specialization);
        var duplicate = await _service.DuplicateAsync(editor.Id);
        Assert.Equal(saved.Traits.OrderBy(x => x.Key), duplicate.Traits.OrderBy(x => x.Key));
        var path = Path.Combine(_directory, "sheet.json");
        var files = new JsonCharacterFiles();
        await files.ExportAsync(saved, path);
        Assert.Contains("\"FormatVersion\": 13", await File.ReadAllTextAsync(path));
        var imported = await _service.ImportAsync(await files.ReadAsync(path));
        Assert.Equal(saved.Traits.OrderBy(x => x.Key), imported.Traits.OrderBy(x => x.Key));
        var maintenance = new SqliteDatabaseMaintenance(_database);
        var backup = Path.Combine(_directory, "backup.db");
        await maintenance.BackupAsync(backup);
        melee.Specialization = "";
        await autosave.FireAsync();
        await maintenance.RestoreAsync(backup);
        var restored = await _service.GetAsync(editor.Id);
        Assert.Equal("Esgrima / estacas", restored.Traits.Single(x => x.Key == melee.Key).Specialization);
        var pdf = Path.Combine(_directory, "sheet.pdf");
        await new CharacterPdfExporter(new ProvidedDisciplineCatalog(), new SqliteSkillCatalog(_database))
            .ExportAsync(restored, pdf, [new("Skills", "Habilidades")]);
        using var document = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
        var fields = document.AcroForm.Elements.GetArray("/Fields")!.Elements.Cast<PdfReference>()
            .Select(x => (PdfDictionary)x.Value).ToArray();
        Assert.Contains(fields, x => x.Elements.GetString("/TU") == "Especializacao de Armas Brancas" &&
            x.Elements.GetString("/V") == "Esgrima / estacas");
        Assert.Contains(fields, x => x.Elements.GetString("/V") == "Especialização pessoal");
        Assert.Contains(restored.Traits, x => x.Name == "Habilidade pessoal" && x.Skill == new SkillInfo("Físicas"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task Legacy_json_missing_specialization_imports_empty_text(int version)
    {
        var dto = await _service.CreateAsync();
        dto = dto with { Traits = [.. dto.Traits, new(TraitCategory.Skill, "old", "Antiga", 3)] };
        var path = Path.Combine(_directory, "legacy.json");
        await new JsonCharacterFiles().ExportAsync(dto, path);
        var file = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        file["FormatVersion"] = version;
        foreach (var item in file["Character"]!["Traits"]!.AsArray()) item!.AsObject().Remove("Specialization");
        await File.WriteAllTextAsync(path, file.ToJsonString());
        var restored = await new JsonCharacterFiles().ReadAsync(path);
        Assert.All(restored.Traits, x => Assert.Equal("", x.Specialization));
        Assert.Equal(3, restored.Traits.Single(x => x.Key == "old").Rating);
    }

    [Fact]
    public async Task Previous_database_is_upgraded_without_losing_ratings()
    {
        var old = new DatabaseAccess(Path.Combine(_directory, "old.db"));
        await new SqliteDatabaseMaintenance(old).InitializeAsync();
        var service = new CharacterService(new SqliteCharacterRepository(old));
        var character = await service.CreateAsync();
        await service.SaveAsync(character with { Traits = [.. character.Traits, new(TraitCategory.Skill, "old", "Antiga", 4)] });
        await using (var db = old.CreateContext())
        {
            await db.GetService<IMigrator>().MigrateAsync("20261017220816_SkillsAndNpcSheets");
        }
        await new SqliteDatabaseMaintenance(old).InitializeAsync();
        await using var upgraded = old.CreateContext();
        var trait = await upgraded.Set<CharacterTrait>().SingleAsync(x => x.Key == "old");
        Assert.Equal(4, trait.Rating);
        Assert.Equal("", trait.Specialization);
        Assert.False(upgraded.Database.HasPendingModelChanges());
    }

    [Fact]
    public void Specialization_validation_is_bounded_and_limited_to_skills()
    {
        var attrs = CharacterRules.DefaultAttributes;
        Assert.Empty(CharacterRules.Validate(new(), [.. attrs, new(TraitCategory.Skill, "a", "Teste", 0, new string('x', 500))]));
        Assert.NotEmpty(CharacterRules.Validate(new(), [.. attrs, new(TraitCategory.Skill, "a", "Teste", 0, new string('x', 501))]));
        Assert.NotEmpty(CharacterRules.Validate(new(), [.. attrs, new(TraitCategory.Skill, "a", "Teste", 0, null!)]));
        Assert.NotEmpty(CharacterRules.Validate(new(), [.. attrs, new(TraitCategory.Discipline, "a", "Teste", 0, "Invalida")]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Maximum_specialization_length_never_overflows_pdf_pages(bool lineBreaks)
    {
        var value = lineBreaks ? string.Concat(Enumerable.Repeat("Pesquisa\n", 55)) : new string('W', 500);
        var dto = await _service.CreateAsync();
        dto = dto with { Traits = [.. dto.Traits, new(TraitCategory.Skill, "skill-melee", "Armas Brancas", 2, value)] };
        var path = Path.Combine(_directory, "long.pdf");
        await new CharacterPdfExporter(new ProvidedDisciplineCatalog(), new SqliteSkillCatalog(_database))
            .ExportAsync(dto, path, [new("Skills", "Habilidades")]);
        using var document = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        var fields = document.AcroForm.Elements.GetArray("/Fields")!.Elements.Cast<PdfReference>().Select(x => (PdfDictionary)x.Value);
        Assert.Contains(fields, x => x.Elements.GetString("/V") == value);
        foreach (var page in document.Pages)
            foreach (var reference in page.Elements.GetArray("/Annots")!.Elements.Cast<PdfReference>())
            {
                var rectangle = ((PdfDictionary)reference.Value).Elements.GetRectangle("/Rect");
                Assert.InRange(rectangle.Y1, 0, page.Height.Point);
                Assert.InRange(rectangle.Y2, 0, page.Height.Point);
            }
    }

    private sealed class ManualAutosave : IAutosaveScheduler
    {
        private Func<Task>? _pending;
        public void Schedule(Func<Task> action) => _pending = action;
        public async Task FireAsync() { var action = _pending; _pending = null; if (action is not null) await action(); }
        public void Cancel() => _pending = null;
        public void Dispose() => _pending = null;
    }
}
