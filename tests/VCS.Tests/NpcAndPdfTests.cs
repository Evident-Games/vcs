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
        var mentalPersonal = editor.SkillGroups.Single(x => x.Name == "Mentais").PersonalItems;
        Assert.Contains(mentalPersonal, x => x.Name == "Pessoal" && !x.IsDraft);
        Assert.Single(mentalPersonal, x => x.IsDraft);
        editor.SkillGroups[0].Items[0].Rating = 3;
        Assert.True(await editor.FlushAsync());
        Assert.Contains((await _service.GetAsync(dto.Id)).Traits, x => x.Key == "custom" &&
            x.Rating == 2 && x.Skill == new SkillInfo("Mentais"));
    }

    [Fact]
    public async Task Npc_slots_tracks_duplicate_json_and_backup_preserve_data()
    {
        var dto = await _service.CreateNpcAsync();
        using var editor = Editor(dto);
        editor.ConfigureAdvantageCatalog(await new SqliteAdvantageCatalog(_database).ListAsync());
        Assert.True(editor.IsNpc);
        Assert.Equal(CharacterEditorViewModel.NpcDisciplineSlots, editor.NpcDisciplines.Count);
        Assert.All(editor.NpcDisciplines, x => Assert.Equal("", x.SelectedDiscipline));
        Assert.Equal(CharacterEditorViewModel.NpcMeritSlotsPerKind, editor.AdvantageCatalog!.AdvantageRows.Count);
        Assert.Equal(CharacterEditorViewModel.NpcMeritSlotsPerKind, editor.AdvantageCatalog.FlawRows.Count);
        Assert.Equal("", editor.AdvantageSelectionGuidance);
        Assert.Equal(63, editor.AdvantageCatalog!.Families.Sum(x => x.Options.Count));
        Assert.False(editor.IsDirty);
        editor.NpcPhysical = 4; editor.NpcSocial = 2; editor.NpcMental = 3;
        editor.NpcHealthCapacity = 12; editor.NpcWillpowerCapacity = 8;
        editor.HealthSuperficial = 2; editor.HealthAggravated = 3; editor.WillpowerAggravated = 5;
        var slot = editor.NpcDisciplines[0];
        var disciplineCount = editor.Disciplines.Items.Count;
        slot.SelectedDiscipline = "Ofuscação";
        slot.Rating = 3;
        Assert.Equal(disciplineCount, editor.Disciplines.Items.Count);
        Assert.Equal(3, editor.Disciplines.Items.Single(x => x.Name == "Ofuscação").Rating);
        Assert.NotEmpty(slot.Powers);
        slot.Powers.Single(x => x.Definition.Name == "Quimerismo").IsKnown = true;
        Assert.All(slot.Powers, x => Assert.Equal("Ofuscação", x.Definition.Discipline));
        editor.AdvantageCatalog.Families.Single(x => x.Name == "Beleza").Options.Single(x => x.Name == "Deslumbrante").IsSelected = true;
        editor.AddNpcDisciplineCommand.Execute(null);
        Assert.Equal(CharacterEditorViewModel.NpcDisciplineSlots + 1, editor.NpcDisciplines.Count);
        Assert.Equal(4, Assert.Single(editor.Advantages.Items).Rating);
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

    [Fact]
    public async Task Npc_discipline_slots_accept_custom_names_like_player_disciplines()
    {
        using var editor = Editor(await _service.CreateNpcAsync());
        var first = editor.NpcDisciplines[0];
        var second = editor.NpcDisciplines[1];

        // Typing letter by letter renames the same personal entry instead of piling up entries.
        foreach (var length in new[] { 1, 5, 11 }) first.SelectedDiscipline = "Disciplina personalizada"[..length];
        first.SelectedDiscipline = "Disciplina personalizada";
        first.Rating = 3;
        second.SelectedDiscipline = "Outra disciplina personalizada";
        second.Rating = 2;

        Assert.DoesNotContain("Disciplina personalizada", first.Options.Select(x => x.Value));
        Assert.DoesNotContain("Outra disciplina personalizada", second.Options.Select(x => x.Value));
        Assert.Contains(editor.Disciplines.Items, x => x.Name == "Disciplina personalizada" && x.Rating == 3);
        Assert.Contains(editor.Disciplines.Items, x => x.Name == "Outra disciplina personalizada" && x.Rating == 2);
        Assert.Single(editor.Disciplines.Items, x => x.Name.StartsWith("Disciplina", StringComparison.Ordinal));
        Assert.Equal(editor.Disciplines.Items.Count, editor.Disciplines.Items.Select(x => x.Key).Distinct().Count());
        Assert.True(await editor.FlushAsync());
        using var reopened = Editor(await _service.GetAsync(editor.Id));
        Assert.Contains(reopened.NpcDisciplines, x => x.SelectedDiscipline == first.SelectedDiscipline && x.Rating == 3);
        Assert.Contains(reopened.NpcDisciplines, x => x.SelectedDiscipline == second.SelectedDiscipline && x.Rating == 2);
    }

    [Fact]
    public async Task Npc_sheet_rows_and_background_fields_persist_and_reach_the_pdf()
    {
        using var editor = Editor(await _service.CreateNpcAsync());
        editor.ConfigureAdvantageCatalog(await new SqliteAdvantageCatalog(_database).ListAsync());
        var sheet = editor.AdvantageCatalog!;
        Assert.Contains(sheet.AdvantageRows[0].CategorySuggestions, x => x.Value == "Beleza");
        editor.Chronicle = "Chicago by Night";
        editor.NpcTrueAge.Value = "312";
        editor.NpcApparentAge.Value = "30";
        editor.NpcHistory.Value = "Chegou ao porto em 1712.";
        editor.NpcAppearance.Value = "Casaco longo e cicatriz no queixo.";
        editor.NpcPhysical = 3;
        editor.NpcDisciplines[0].SelectedDiscipline = "Rapidez";
        editor.NpcDisciplines[0].Rating = 2;
        var advantage = sheet.AdvantageRows[0];
        advantage.Name = "Contatos no porto";
        advantage.Rating = 2;
        var flaw = sheet.FlawRows[0];
        flaw.Name = "Inimigo";
        flaw.Rating = 1;
        Assert.Equal(1, Assert.Single(editor.Flaws.Items).Rating);
        Assert.Equal(2, Assert.Single(editor.Advantages.Items).Rating);
        var extra = sheet.AdvantageRows[1];
        extra.Name = "Rebanho";
        Assert.Equal(2, editor.Advantages.Items.Count);
        extra.RemoveCommand.Execute(null);
        Assert.Single(editor.Advantages.Items);
        Assert.Equal(CharacterEditorViewModel.NpcMeritSlotsPerKind - 1, sheet.AdvantageRows.Count);
        var spare = editor.NpcDisciplines[^1];
        spare.SelectedDiscipline = "Dominação";
        spare.Rating = 1;
        Assert.Equal(1, editor.Disciplines.Items.Single(x => x.Name == "Dominação").Rating);
        spare.RemoveCommand.Execute(null);
        Assert.Equal(0, editor.Disciplines.Items.Single(x => x.Name == "Dominação").Rating);
        Assert.DoesNotContain(spare, editor.NpcDisciplines);
        Assert.True(await editor.FlushAsync());

        var saved = await _service.GetAsync(editor.Id);
        Assert.Equal("Chicago by Night", saved.Details.Chronicle);
        Assert.Equal("312", saved.Details.TrueAge);
        Assert.Equal("30", saved.Details.ApparentAge);
        Assert.Equal("Chegou ao porto em 1712.", saved.Details.History);
        using var reopened = Editor(saved);
        reopened.ConfigureAdvantageCatalog(await new SqliteAdvantageCatalog(_database).ListAsync());
        Assert.Equal("Rapidez", reopened.NpcDisciplines[0].SelectedDiscipline);
        Assert.Contains(reopened.AdvantageCatalog!.FlawRows, x => x.Name == "Inimigo" && x.Rating == 1);
        Assert.Contains(reopened.AdvantageCatalog.AdvantageRows, x => x.Name == "Contatos no porto" && x.Rating == 2);
        Assert.Equal(CharacterEditorViewModel.NpcMeritSlotsPerKind, reopened.AdvantageCatalog.AdvantageRows.Count);
        Assert.Equal(CharacterEditorViewModel.NpcMeritSlotsPerKind, reopened.AdvantageCatalog.FlawRows.Count);

        var path = Path.Combine(_directory, "npc-full.pdf");
        await new CharacterPdfExporter(new ProvidedDisciplineCatalog(), new SqliteSkillCatalog(_database)).ExportAsync(saved, path, []);
        using var document = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        var fields = document.AcroForm.Elements.GetArray("/Fields")!.Elements.Cast<PdfReference>()
            .Select(x => (PdfDictionary)x.Value).ToArray();
        string Value(string title) => fields.Single(x => x.Elements.GetString("/TU") == title).Elements.GetString("/V");
        Assert.Equal("Chicago by Night", Value("Cronica"));
        Assert.Equal("312", Value("Idade verdadeira"));
        Assert.Equal("30", Value("Idade aparente"));
        Assert.Equal("Casaco longo e cicatriz no queixo.", Value("Aparencia"));
        var titles = fields.Select(x => x.Elements.GetString("/TU")).ToArray();
        Assert.Contains("Fisicos / 5", titles);
        Assert.Contains("Rapidez / 5", titles);
        Assert.DoesNotContain("Auspicios / 5", titles);
        Assert.Contains("Disciplina 3", titles);
        Assert.Contains("Defeito: Inimigo / 5", titles);
        Assert.Contains("Vantagem / defeito 6", titles);
        Assert.Contains(titles, x => x.StartsWith("Historia", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Personal_merit_family_description_dots_and_xp_survive_every_export(bool npc)
    {
        using var editor = Editor(npc ? await _service.CreateNpcAsync() : await _service.CreateAsync());
        var catalog = await new SqliteAdvantageCatalog(_database).ListAsync();
        editor.ConfigureAdvantageCatalog(catalog);
        var family = editor.AdvantageCatalog!.Families.Single(x => x.Name == "Refúgio");
        family.Options.Single(x => x.Name == "Refúgio").Rating = 3;
        family.Options.Single(x => x.Name == "Arsenal oculto").IsSelected = true;
        family.AddAdvantageCommand.Execute(null);
        var custom = Assert.Single(family.PersonalEntries);
        custom.Name = "Biblioteca secreta";
        custom.Description = "Colecao e anotacoes da cronica.";
        custom.Rating = 5;
        family.AddFlawCommand.Execute(null);
        var flaw = family.PersonalEntries.Single(x => x.Category == TraitCategory.Flaw);
        flaw.Name = "Entrada vigiada";
        flaw.Description = "Detalhes do defeito.";
        flaw.Rating = 2;
        Assert.Equal(6, editor.ExperienceSpent);
        Assert.True(await editor.FlushAsync(), editor.ValidationSummary);
        custom.Description = "Descricao revisada depois de salvar.";
        Assert.True(editor.IsDirty);
        Assert.True(await editor.FlushAsync());
        var saved = await _service.GetAsync(editor.Id);
        var expected = new MeritInfo("Refúgio", custom.Description);
        Assert.Equal(expected, saved.Traits.Single(x => x.Key == custom.Key).Merit);
        using var reopened = Editor(saved);
        reopened.ConfigureAdvantageCatalog(catalog);
        Assert.Contains(reopened.AdvantageCatalog!.Families.Single(x => x.Name == "Refúgio").PersonalEntries,
            x => x.Key == custom.Key && x.Rating == 5 && x.Description == custom.Description);
        Assert.Equal(6, reopened.ExperienceSpent);
        Assert.False(reopened.IsDirty);
        Assert.Equal(expected, (await _service.DuplicateAsync(editor.Id)).Traits.Single(x => x.Key == custom.Key).Merit);
        var files = new JsonCharacterFiles();
        var json = Path.Combine(_directory, "personal.json");
        await files.ExportAsync(saved, json);
        var imported = await _service.ImportAsync(await files.ReadAsync(json));
        Assert.Equal(saved.Traits.OrderBy(x => x.Key), imported.Traits.OrderBy(x => x.Key));
        var maintenance = new SqliteDatabaseMaintenance(_database);
        var backup = Path.Combine(_directory, "personal.db");
        await maintenance.BackupAsync(backup);
        await _service.DeleteAsync((await _service.ListAsync()).Single(x => x.Id == editor.Id));
        await maintenance.RestoreAsync(backup);
        Assert.Equal(expected, (await _service.GetAsync(editor.Id)).Traits.Single(x => x.Key == custom.Key).Merit);
        var pdf = Path.Combine(_directory, "personal.pdf");
        await new CharacterPdfExporter(new ProvidedDisciplineCatalog(), new SqliteSkillCatalog(_database))
            .ExportAsync(saved, pdf, [new("Advantages", "Vantagens e defeitos")]);
        using var document = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
        var fields = document.AcroForm.Elements.GetArray("/Fields")!.Elements.Cast<PdfReference>()
            .Select(x => (PdfDictionary)x.Value).ToArray();
        Assert.Contains(fields, x => x.Elements.GetString("/V") == custom.Description);
        Assert.Contains(fields, x => x.Elements.GetString("/V") == "Refúgio");
        Assert.Contains(fields, x => x.Elements.GetString("/V") == flaw.Description);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Automatic_xp_shortage_survives_reopen_duplicate_json_and_pdf(bool npc)
    {
        using var editor = Editor(npc ? await _service.CreateNpcAsync() : await _service.CreateAsync());
        editor.AgeIndex = 1;
        editor.BloodPotency = 2;
        Assert.Equal(15, editor.ExperienceTotal);
        Assert.Equal(20, editor.ExperienceSpent);
        Assert.Contains("Faltam 5 XP", editor.ExperienceWarning);
        Assert.True(await editor.FlushAsync());
        var saved = await _service.GetAsync(editor.Id);
        using var reopened = Editor(saved);
        Assert.Equal(-5, reopened.ExperienceAvailable);
        Assert.Contains("Faltam 5 XP", reopened.ExperienceWarning);
        var copy = await _service.DuplicateAsync(editor.Id);
        Assert.Equal(saved.Details.Advancement, copy.Details.Advancement);
        Assert.Equal(20, copy.Details.ExperienceSpent);
        var json = Path.Combine(_directory, "unpaid.json");
        var files = new JsonCharacterFiles();
        await files.ExportAsync(saved, json);
        var imported = await _service.ImportAsync(await files.ReadAsync(json));
        Assert.Equal(-5, imported.Details.ExperienceTotal - imported.Details.ExperienceSpent);
        var pdf = Path.Combine(_directory, "unpaid.pdf");
        await new CharacterPdfExporter(new ProvidedDisciplineCatalog(), new SqliteSkillCatalog(_database))
            .ExportAsync(saved, pdf, [new("Background", "Historia e conviccoes")]);
        using var document = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
        // The NPC PDF mirrors the simple NPC sheet, which has no XP block.
        Assert.Equal(!npc, document.AcroForm.Elements.GetArray("/Fields")!.Elements.Cast<PdfReference>()
            .Any(x => ((PdfDictionary)x.Value).Elements.GetString("/V").Contains("Faltam 5 XP")));
        reopened.CampaignExperienceTotal = 5;
        Assert.Equal(0, reopened.ExperienceAvailable);
        Assert.Empty(reopened.ExperienceWarning);
        Assert.True(await reopened.FlushAsync());
        reopened.BloodPotency = 1;
        Assert.Equal(0, reopened.ExperienceSpent);
        Assert.Equal(20, reopened.ExperienceAvailable);
    }

    [Theory]
    [InlineData(0, 5, 0)]
    [InlineData(101, 5, 0)]
    [InlineData(5, 0, 0)]
    [InlineData(5, 101, 0)]
    [InlineData(5, 5, 1)]
    public void Invalid_npc_capacities_and_non_cross_willpower_are_rejected(int health, int willpower, int superficial)
    {
        Assert.Throws<CharacterValidationException>(() => Character.Create(new()
        {
            IsNpc = true, NpcHealthCapacity = health, NpcWillpowerCapacity = willpower, WillpowerSuperficial = superficial
        }));
    }

    [Fact]
    public async Task Npc_quick_sheet_attacks_blood_effects_and_large_tracks_persist_and_export()
    {
        using var editor = Editor(await _service.CreateNpcAsync());
        Assert.Single(editor.NpcAttacks);
        Assert.Equal("+1 dados", editor.NpcBloodSurge.Value);
        Assert.Equal("1 superficial", editor.NpcDamageMend.Value);
        Assert.Equal("0", editor.NpcPowerBonus.Value);
        editor.BloodPotency = 5;
        Assert.Equal("+4 dados", editor.NpcBloodSurge.Value);
        Assert.Equal("3 superficial", editor.NpcDamageMend.Value);
        Assert.Equal("+2 dados", editor.NpcPowerBonus.Value);
        editor.NpcDamageMend.Value = "3 agravado (Fortitude)";
        editor.BloodPotency = 8;
        Assert.Equal("+5 dados", editor.NpcBloodSurge.Value);
        Assert.Equal("3 agravado (Fortitude)", editor.NpcDamageMend.Value);
        for (var i = 0; i < 40; i++) editor.AddNpcHealthBoxCommand.Execute(null);
        for (var i = 0; i < 25; i++) editor.AddNpcWillpowerBoxCommand.Execute(null);
        editor.HealthAggravated = 45;
        editor.RemoveNpcHealthBoxCommand.Execute(null);
        Assert.Equal(45, editor.NpcHealthCapacity);
        editor.HealthAggravated = 30;
        editor.RemoveNpcHealthBoxCommand.Execute(null);
        Assert.Equal(44, editor.NpcHealthCapacity);
        for (var i = 0; i < 200; i++) editor.AddNpcWillpowerBoxCommand.Execute(null);
        Assert.Equal(CharacterRules.NpcMaximumBoxes, editor.NpcWillpowerCapacity);
        // Typed totals: invalid text is ignored and marked damage is the floor.
        editor.NpcHealthText = "12";
        Assert.Equal(30, editor.NpcHealthCapacity);
        editor.HealthAggravated = 0;
        editor.NpcHealthText = "12";
        Assert.Equal(12, editor.NpcHealthCapacity);
        editor.NpcHealthText = "abc";
        Assert.Equal("12", editor.NpcHealthText);
        editor.NpcWillpowerText = "7";
        Assert.Equal(7, editor.NpcWillpowerCapacity);
        editor.NpcWillpowerText = "1000";
        Assert.Equal(CharacterRules.NpcMaximumBoxes, editor.NpcWillpowerCapacity);
        editor.NpcHealthText = "44";
        editor.HealthAggravated = 30;
        editor.NpcAttacks[0].Name = "Presas";
        editor.NpcAttacks[0].DicePool = "7";
        editor.NpcAttacks[0].DamageBonus = "+1";
        editor.NpcAttacks[0].DamageType = "Agravado";
        editor.NpcAttacks[0].ActiveDisciplines = "Potencia 2";
        editor.NpcAttacks[0].Notes = "Somente apos agarrar";
        editor.AddNpcAttackCommand.Execute(null);
        editor.NpcAttacks[1].Name = "Pistola";
        editor.AddNpcAttackCommand.Execute(null);
        editor.RemoveNpcAttackCommand.Execute(editor.NpcAttacks[2]);
        Assert.True(editor.IsDirty);
        Assert.True(await editor.FlushAsync(), editor.ValidationSummary);
        var saved = await _service.GetAsync(editor.Id);
        Assert.Equal(44, saved.Details.NpcHealthCapacity);
        Assert.Equal(CharacterRules.NpcMaximumBoxes, saved.Details.NpcWillpowerCapacity);
        Assert.Equal("+5 dados", saved.Details.BloodSurge);
        Assert.Equal(new[] { "Presas", "Pistola" }, saved.Details.NpcAttacks.Select(x => x.Name));
        Assert.Equal(new NpcAttack("Presas", "7", "+1", "Agravado", "Potencia 2", "Somente apos agarrar"), saved.Details.NpcAttacks[0]);
        using var reopened = Editor(saved);
        Assert.Equal(2, reopened.NpcAttacks.Count);
        Assert.Equal("3 agravado (Fortitude)", reopened.NpcDamageMend.Value);
        Assert.False(reopened.IsDirty);
        var json = Path.Combine(_directory, "quick-npc.json");
        await new JsonCharacterFiles().ExportAsync(saved, json);
        var imported = await _service.ImportAsync(await new JsonCharacterFiles().ReadAsync(json));
        Assert.Equal(saved.Details.NpcAttacks, imported.Details.NpcAttacks);
        Assert.Equal(saved.Details.NpcAttacks, (await _service.DuplicateAsync(saved.Id)).Details.NpcAttacks);
        var path = Path.Combine(_directory, "quick-npc.pdf");
        await new CharacterPdfExporter(new ProvidedDisciplineCatalog(), new SqliteSkillCatalog(_database)).ExportAsync(saved, path, []);
        using var document = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        var fields = document.AcroForm.Elements.GetArray("/Fields")!.Elements.Cast<PdfReference>()
            .Select(x => (PdfDictionary)x.Value).ToArray();
        var titles = fields.Select(x => x.Elements.GetString("/TU")).ToArray();
        Assert.Contains(fields, x => x.Elements.GetString("/V") == "Presas");
        Assert.Contains(fields, x => x.Elements.GetString("/V") == "Pistola");
        Assert.Contains(fields, x => x.Elements.GetString("/V") == "Potencia 2");
        Assert.Contains(fields, x => x.Elements.GetString("/V") == "+5 dados");
        Assert.Contains("Fisicos / 5", titles);
        Assert.Equal(44, titles.Count(x => x.StartsWith("Vitalidade")));
    }

    [Theory]
    [InlineData(51, 10, 0)]
    [InlineData(1, 201, 0)]
    [InlineData(1, 10, 2001)]
    public void Npc_attacks_are_limited(int count, int nameLength, int notesLength)
    {
        var attacks = Enumerable.Range(0, count).Select(_ => new NpcAttack(new string('a', nameLength), Notes: new string('n', notesLength))).ToArray();
        Assert.Throws<CharacterValidationException>(() => Character.Create(new() { IsNpc = true, NpcAttacks = attacks }));
    }

    [Theory]
    [InlineData(0, "+1 dados", "1 superficial", "0")]
    [InlineData(2, "+2 dados", "2 superficial", "+1 dados")]
    [InlineData(10, "+6 dados", "5 superficial", "+5 dados")]
    public void Blood_potency_effects_follow_the_v5_table(int potency, string surge, string mend, string bonus)
    {
        Assert.Equal(surge, BloodPotencyEffects.BloodSurge(potency));
        Assert.Equal(mend, BloodPotencyEffects.DamageMend(potency));
        Assert.Equal(bonus, BloodPotencyEffects.PowerBonus(potency));
    }

    [Fact]
    public async Task Npc_can_reopen_and_export_more_than_six_filled_slots_without_truncation()
    {
        using var editor = Editor(await _service.CreateNpcAsync());
        editor.ConfigureAdvantageCatalog(await new SqliteAdvantageCatalog(_database).ListAsync());
        var family = editor.AdvantageCatalog!.Families.Single(x => x.Name == "Mortais");
        var names = editor.NpcDisciplines[0].Options.Select(x => x.Value).Take(7).ToArray();
        for (var i = 0; i < 7; i++)
        {
            if (i >= editor.NpcDisciplines.Count) editor.AddNpcDisciplineCommand.Execute(null);
            editor.NpcDisciplines[i].SelectedDiscipline = names[i];
            editor.NpcDisciplines[i].Rating = 2;
            family.AddAdvantageCommand.Execute(null);
            family.PersonalEntries[i].Name = $"Pessoa {i}";
            family.PersonalEntries[i].Rating = 2;
        }
        editor.NpcDisciplines[1].SelectedDiscipline = names[0];
        Assert.NotEmpty(editor.NpcDisciplines[1].Error);
        Assert.Equal(0, editor.Disciplines.Items.Single(x => x.Name == names[1]).Rating);
        Assert.Equal(2, editor.Disciplines.Items.Single(x => x.Name == names[0]).Rating);
        editor.NpcDisciplines[1].SelectedDiscipline = names[1];
        Assert.Equal("", editor.NpcDisciplines[1].Error);
        Assert.Equal(2, editor.Disciplines.Items.Single(x => x.Name == names[1]).Rating);
        Assert.True(await editor.FlushAsync());
        using var reopened = Editor(await _service.GetAsync(editor.Id));
        reopened.ConfigureAdvantageCatalog(await new SqliteAdvantageCatalog(_database).ListAsync());
        Assert.Equal(7, reopened.NpcDisciplines.Count);
        Assert.Equal(names.Order(), reopened.NpcDisciplines.Select(x => x.SelectedDiscipline).Order());
        Assert.Equal(7, reopened.AdvantageCatalog!.AdvantageRows.Count);
        Assert.Equal(CharacterEditorViewModel.NpcMeritSlotsPerKind, reopened.AdvantageCatalog.FlawRows.Count);
        Assert.Equal(7, reopened.AdvantageCatalog!.Families.Single(x => x.Name == "Mortais").PersonalEntries.Count);
        var path = Path.Combine(_directory, "expanded.pdf");
        await new CharacterPdfExporter(new ProvidedDisciplineCatalog(), new SqliteSkillCatalog(_database))
            .ExportAsync(await _service.GetAsync(editor.Id), path, [new("Disciplines", "Disciplinas"), new("Advantages", "Vantagens e defeitos")]);
        using var document = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        var titles = document.AcroForm.Elements.GetArray("/Fields")!.Elements.Cast<PdfReference>()
            .Select(x => ((PdfDictionary)x.Value).Elements.GetString("/TU")).ToArray();
        foreach (var name in names) Assert.Contains($"{name} / 5", titles);
        foreach (var merit in reopened.Advantages.Items)
            Assert.Contains($"Vantagem: {merit.Name} / 5", titles);
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
