using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;
using VCS.Application.Abstractions;
using VCS.Application.Characters;
using VCS.Desktop.ViewModels;
using VCS.Domain.Characters;
using VCS.Infrastructure.Files;
using VCS.Infrastructure.Persistence;
using VCS.Infrastructure.Repositories;

namespace VCS.Tests;

public sealed class NpcAndPdfTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "VCS-npc-" + Guid.NewGuid().ToString("N"));
    private DatabaseAccess _database = null!;
    private CharacterService _service = null!;
    public async Task InitializeAsync()
    {
        _database = new(Path.Combine(_directory, "test.db"));
        await new SqliteDatabaseMaintenance(_database).InitializeAsync();
        _service = new(new SqliteCharacterRepository(_database));
    }
    public Task DisposeAsync() { Directory.Delete(_directory, true); return Task.CompletedTask; }

    [Fact]
    public async Task Skill_seed_is_shared_complete_and_idempotent()
    {
        var catalog = new SqliteSkillCatalog(_database);
        var entries = await catalog.ListAsync();
        Assert.Equal(27, entries.Count);
        Assert.Equal(27, entries.Select(x => x.Id).Distinct().Count());
        Assert.All(entries.GroupBy(x => x.Group), x => Assert.Equal(9, x.Count()));
        Assert.Equal(3, entries.Select(x => x.Group).Distinct().Count());
        Assert.All(entries, x => Assert.False(string.IsNullOrWhiteSpace(x.Description)));
        await new SqliteDatabaseMaintenance(_database).InitializeAsync();
        Assert.Equal(27, (await catalog.ListAsync()).Count);
        await using var db = _database.CreateContext();
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Skills_keep_existing_ratings_and_custom_entries_for_both_sheet_types(bool npc)
    {
        var dto = npc ? await _service.CreateNpcAsync() : await _service.CreateAsync();
        dto = await _service.SaveAsync(dto with { Traits = [.. dto.Traits,
            new(TraitCategory.Skill, "legacy", "Percepcao", 4), new(TraitCategory.Skill, "custom", "Pessoal", 2)] });
        using var editor = Editor(dto);
        editor.ConfigureSkillCatalog(await new SqliteSkillCatalog(_database).ListAsync());
        Assert.False(editor.IsDirty);
        Assert.Equal(27, editor.SkillGroups.Sum(x => x.Items.Count));
        Assert.Equal(28, editor.Skills.Items.Count);
        Assert.Equal(4, editor.SkillGroups.Single(x => x.Name == "Mentais").Items.Single(x => x.Key == "legacy").Rating);
        editor.SkillGroups[0].Items[0].Rating = 3;
        Assert.True(await editor.FlushAsync());
        Assert.Contains((await _service.GetAsync(dto.Id)).Traits, x => x.Key == "custom" && x.Rating == 2);
    }

    [Fact]
    public async Task Npc_slots_tracks_duplicate_json_and_backup_preserve_data()
    {
        var dto = await _service.CreateNpcAsync();
        using var editor = Editor(dto);
        editor.ConfigureAdvantageCatalog(await new SqliteAdvantageCatalog(_database).ListAsync());
        Assert.True(editor.IsNpc);
        Assert.Equal(6, editor.NpcDisciplines.Count);
        Assert.Equal(6, editor.NpcMerits.Count);
        Assert.False(editor.IsDirty);
        editor.NpcPhysical = 4; editor.NpcSocial = 2; editor.NpcMental = 3;
        editor.NpcHealthCapacity = 12; editor.NpcWillpowerCapacity = 8;
        editor.HealthSuperficial = 2; editor.HealthAggravated = 3; editor.WillpowerAggravated = 5;
        var slot = editor.NpcDisciplines[0];
        slot.SelectedDiscipline = "Ofuscação"; slot.Rating = 3;
        slot.Powers.Single(x => x.Definition.Name == "Quimerismo").IsKnown = true;
        Assert.All(slot.Powers, x => Assert.Equal("Ofuscação", x.Definition.Discipline));
        editor.NpcMerits[0].Selected = editor.NpcMerits[0].Options.Single(x => x.Name == "Deslumbrante");
        editor.NpcMerits[0].Rating = 4;
        editor.AddNpcDisciplineCommand.Execute(null); editor.AddNpcMeritCommand.Execute(null);
        Assert.Equal(7, editor.NpcDisciplines.Count); Assert.Equal(7, editor.NpcMerits.Count);
        Assert.True(await editor.FlushAsync());
        var saved = await _service.GetAsync(dto.Id);
        Assert.Equal(12, saved.Details.NpcHealthCapacity);
        Assert.Equal(5, saved.Details.WillpowerAggravated);
        var copy = await _service.DuplicateAsync(dto.Id);
        Assert.True(copy.Details.IsNpc);
        Assert.Equal(saved.Traits.OrderBy(x => x.Key), copy.Traits.OrderBy(x => x.Key));
        var files = new JsonCharacterFiles();
        var json = Path.Combine(_directory, "npc.json");
        await files.ExportAsync(saved, json);
        var imported = await _service.ImportAsync(await files.ReadAsync(json));
        Assert.Equal(saved.Details, imported.Details);
        Assert.All(await _service.ListAsync(), x => Assert.True(x.IsNpc));
        var backup = Path.Combine(_directory, "backup.db");
        var maintenance = new SqliteDatabaseMaintenance(_database);
        await maintenance.BackupAsync(backup);
        await _service.CreateAsync();
        await maintenance.RestoreAsync(backup);
        Assert.Equal(3, (await _service.ListAsync()).Count);
        Assert.Equal(saved.Details, (await _service.GetAsync(dto.Id)).Details);
        editor.NpcHealthCapacity = 4;
        Assert.False(await editor.FlushAsync());
        Assert.Equal(12, (await _service.GetAsync(dto.Id)).Details.NpcHealthCapacity);
    }

    [Theory]
    [InlineData(0, 5, 0)]
    [InlineData(21, 5, 0)]
    [InlineData(5, 0, 0)]
    [InlineData(5, 21, 0)]
    [InlineData(5, 5, 1)]
    public void Invalid_npc_capacities_and_non_cross_willpower_are_rejected(int health, int willpower, int superficial)
    {
        Assert.Throws<CharacterValidationException>(() => Character.Create(new()
        {
            IsNpc = true, NpcHealthCapacity = health, NpcWillpowerCapacity = willpower, WillpowerSuperficial = superficial
        }));
    }

    [Fact]
    public async Task Npc_can_reopen_and_export_more_than_six_filled_slots_without_truncation()
    {
        using var editor = Editor(await _service.CreateNpcAsync());
        editor.ConfigureAdvantageCatalog(await new SqliteAdvantageCatalog(_database).ListAsync());
        editor.AddNpcDisciplineCommand.Execute(null);
        editor.AddNpcMeritCommand.Execute(null);
        var names = editor.NpcDisciplines[0].Options.Where(x => x.Length != 0).Take(7).ToArray();
        for (var i = 0; i < 7; i++)
        {
            editor.NpcDisciplines[i].SelectedDiscipline = names[i];
            editor.NpcDisciplines[i].Rating = 2;
            editor.NpcMerits[i].Selected = editor.NpcMerits[i].Options[i];
            editor.NpcMerits[i].Rating = 2;
        }
        editor.NpcDisciplines[1].SelectedDiscipline = names[0];
        Assert.NotEmpty(editor.NpcDisciplines[1].Error);
        Assert.Equal(names[1], editor.NpcDisciplines[1].SelectedDiscipline);
        editor.NpcMerits[1].Selected = editor.NpcMerits[1].Options[0];
        Assert.NotEmpty(editor.NpcMerits[1].Error);
        Assert.Same(editor.NpcMerits[1].Options[1], editor.NpcMerits[1].Selected);
        Assert.True(await editor.FlushAsync());
        using var reopened = Editor(await _service.GetAsync(editor.Id));
        reopened.ConfigureAdvantageCatalog(await new SqliteAdvantageCatalog(_database).ListAsync());
        Assert.Equal(7, reopened.NpcDisciplines.Count);
        Assert.Equal(7, reopened.NpcMerits.Count);
        var path = Path.Combine(_directory, "expanded.pdf");
        await new CharacterPdfExporter(new ProvidedDisciplineCatalog(), new SqliteSkillCatalog(_database))
            .ExportAsync(await _service.GetAsync(editor.Id), path, []);
        using var document = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        var titles = document.AcroForm.Elements.GetArray("/Fields")!.Elements.Cast<PdfReference>()
            .Select(x => ((PdfDictionary)x.Value).Elements.GetString("/TU")).ToArray();
        foreach (var name in names) Assert.Contains($"{name} / 5", titles);
        foreach (var merit in reopened.NpcMerits)
            Assert.Contains($"{merit.Kind}: {merit.Selected!.Name} / 5", titles);
    }

    [Fact]
    public async Task Failed_or_cancelled_pdf_does_not_replace_existing_file()
    {
        var path = Path.Combine(_directory, "existing.pdf");
        await File.WriteAllTextAsync(path, "anterior");
        var exporter = new CharacterPdfExporter(new ProvidedDisciplineCatalog(), new SqliteSkillCatalog(_database));
        var character = await _service.CreateAsync();
        await Assert.ThrowsAsync<InvalidDataException>(() => exporter.ExportAsync(character, path, [new("Unknown", "Desconhecida")]));
        Assert.Equal("anterior", await File.ReadAllTextAsync(path));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => exporter.ExportAsync(character, path,
            [new("Notes", "Notas")], new CancellationToken(true)));
        Assert.Equal("anterior", await File.ReadAllTextAsync(path));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Pdf_has_editable_fields_appearances_all_content_and_adaptive_pages(bool npc)
    {
        var dto = npc ? await _service.CreateNpcAsync() : await _service.CreateAsync();
        var notes = string.Join("\n", Enumerable.Range(1, 180).Select(x => $"Nota {x}: memória, ação e convicções."));
        dto = await _service.SaveAsync(dto with { Details = dto.Details with { Name = "Érica", Notes = notes } });
        var path = Path.Combine(_directory, "sheet.pdf");
        var exporter = new CharacterPdfExporter(new ProvidedDisciplineCatalog(), new SqliteSkillCatalog(_database));
        var skillNames = (await new SqliteSkillCatalog(_database).ListAsync()).Select(x => x.Name).ToHashSet();
        await exporter.ExportAsync(dto, path, [new("Identity", "Identidade"), new("Skills", "Habilidades"), new("State", "Estado"), new("Notes", "Notas")]);
        using var document = PdfReader.Open(path, PdfDocumentOpenMode.Modify);
        Assert.True(document.PageCount >= 5);
        var fields = document.AcroForm.Elements.GetArray("/Fields")!.Elements
            .Cast<PdfReference>().Select(x => Assert.IsAssignableFrom<PdfDictionary>(x.Value)).ToArray();
        Assert.Equal(fields.Length, fields.Select(x => x.Elements.GetString("/T")).Distinct().Count());
        Assert.Contains(fields, x => x.Elements.GetString("/V") == "Érica");
        Assert.Equal(27 * 5, fields.Count(x => x.Elements.GetName("/FT") == "/Btn" &&
            skillNames.Contains(x.Elements.GetString("/TU").Split(" / ")[0])));
        var reconstructed = string.Join("\n", fields.Where(x => x.Elements.GetString("/TU").StartsWith("Notas"))
            .Select(x => x.Elements.GetString("/V")));
        Assert.Equal(notes, reconstructed);
        Assert.All(fields, x =>
        {
            Assert.NotNull(x.Elements.GetDictionary("/AP"));
            Assert.Equal(4, x.Elements.GetInteger("/F"));
            Assert.Equal(0, x.Elements.GetInteger("/Ff") & 1);
        });
        foreach (var page in document.Pages)
            foreach (var reference in page.Elements.GetArray("/Annots")!.Elements.Cast<PdfReference>())
            {
                var field = (PdfDictionary)reference.Value;
                var rectangle = field.Elements.GetRectangle("/Rect");
                Assert.InRange(rectangle.X1, 0, page.Width.Point);
                Assert.InRange(rectangle.Y1, 0, page.Height.Point);
                Assert.InRange(rectangle.X2, 0, page.Width.Point);
                Assert.InRange(rectangle.Y2, 0, page.Height.Point);
            }
        var name = fields.Single(x => x.Elements.GetString("/TU") == "Nome");
        name.Elements.SetString("/V", "Nome editado no PDF");
        var savedPath = Path.Combine(_directory, "edited.pdf");
        document.Save(savedPath);
        using var edited = PdfReader.Open(savedPath, PdfDocumentOpenMode.Import);
        Assert.Contains(edited.AcroForm.Elements.GetArray("/Fields")!.Elements.Cast<PdfReference>(),
            x => ((PdfDictionary)x.Value).Elements.GetString("/V") == "Nome editado no PDF");
    }

    private CharacterEditorViewModel Editor(CharacterDto dto) => new(dto, _service,
        NullLogger<CharacterEditorViewModel>.Instance, new NoAutosave(), new ProvidedDisciplineCatalog());
    private sealed class NoAutosave : VCS.Desktop.Services.IAutosaveScheduler
    {
        public void Schedule(Func<Task> action) { }
        public void Cancel() { }
        public void Dispose() { }
    }
}
