using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;
using VCS.Application.Characters;
using VCS.Desktop.Sheet;
using VCS.Domain.Advancement;
using VCS.Domain.Characters;
using VCS.Domain.Clans;
using VCS.Domain.Predators;
using VCS.Infrastructure.Catalogs;
using VCS.Infrastructure.Characters;
using VCS.Infrastructure.Persistence;

namespace VCS.Tests;

public sealed class PredatorTests
{
    public static IEnumerable<object[]> Choices()
    {
        foreach (var definition in PredatorCatalog.All)
            foreach (var discipline in definition.Disciplines)
                for (var i = 0; i < definition.Specializations.Length; i++)
                    yield return [definition.Id, discipline, i];
    }
    private static (CharacterDetails Details, TraitValue[] Traits) Build(string firstSkill = "Medicina", string clan = "Brujah")
    {
        var attributes = new[] { 4, 3, 3, 3, 2, 2, 2, 2, 1 };
        var specialistSkills = CharacterAdvancement.SkillDistribution(SkillProfile.Specialist);
        var skills = new[] { firstSkill, "Intimidação" }.Concat(PredatorCatalog.All.SelectMany(x => x.Specializations).Select(x => x.Skill))
            .Distinct().Select((name, i) => new TraitValue(TraitCategory.Skill, "skill-" + i, name,
                i < specialistSkills.Length ? specialistSkills[i] : 0));
        var disciplines = ClanCatalog.Find(clan)!.Disciplines.Take(2).Select((name, i) =>
            new TraitValue(TraitCategory.Discipline, "disc-" + i, name, i == 0 ? 2 : 1));
        return (new()
        {
            Clan = clan, BloodPotency = 1, ExperienceTotal = 15,
            Advancement = new() { Age = VampireAge.Neonate }
        }, [.. CharacterRules.DefaultAttributes.Select((x, i) => x with { Rating = attributes[i] }), .. skills, .. disciplines,
            new(TraitCategory.Advantage, "base-merit", "Vantagem basica", 5),
            new(TraitCategory.Advantage, "base-merit-2", "Outra vantagem basica", 2),
            new(TraitCategory.Flaw, "base-flaw", "Defeito basico", 2)]);
    }
    private static PredatorChoice Choice(PredatorDefinition definition, string? discipline = null, int specialization = 0) =>
        new(definition.Id, discipline ?? definition.Disciplines[0], specialization, "Escolha pessoal",
            definition.AllocationOptions.Select((_, i) => i == 0 ? definition.AllocationPoints : 0).ToArray(),
            definition.FlawOptions.Select((_, i) => i == 0 ? definition.FlawPoints : 0).ToArray());

    [Theory]
    [MemberData(nameof(Choices))]
    public void Every_predator_discipline_and_specialization_is_free_reversible_and_valid(string id, string discipline, int specialization)
    {
        var definition = PredatorCatalog.Find(id)!;
        var (details, traits) = Build(definition.Specializations[specialization].Skill, "Tremere");
        Assert.Empty(CharacterAdvancement.CreationIssues(details, traits));
        var choice = Choice(definition, discipline, specialization);
        var result = PredatorRules.Apply(details, traits, choice);
        Assert.Empty(CharacterRules.Validate(result.Details, result.Traits));
        Assert.Empty(CharacterAdvancement.CreationIssues(result.Details, result.Traits));
        Assert.Equal(details.Humanity + definition.HumanityDelta, result.Details.Humanity);
        Assert.Equal(details.BloodPotency + definition.BloodDelta, result.Details.BloodPotency);
        Assert.Equal(0, result.Details.ExperienceSpent);
        Assert.Equal(CharacterAdvancement.FreeSpecializationsRemaining(details, traits),
            CharacterAdvancement.FreeSpecializationsRemaining(result.Details, result.Traits));
        var repeated = PredatorRules.Apply(result.Details, result.Traits, choice);
        Assert.Equal(result.Details, repeated.Details);
        Assert.Equal(result.Traits.OrderBy(x => x.Key), repeated.Traits.OrderBy(x => x.Key));
        var removed = PredatorRules.Remove(repeated.Details, repeated.Traits);
        Assert.Equal(details, removed.Details);
        Assert.Equal(traits.Where(x => x.Rating > 0).OrderBy(x => x.Key),
            removed.Traits.Where(x => x.Rating > 0).OrderBy(x => x.Key));
        var finalized = CharacterAdvancement.FinalizeCreation(result.Details, result.Traits);
        Assert.Empty(CharacterRules.Validate(finalized, result.Traits));
        Assert.Throws<InvalidOperationException>(() => PredatorRules.Remove(finalized, result.Traits));
        Assert.Throws<InvalidOperationException>(() => PredatorRules.Apply(finalized, result.Traits, choice));
        var advanced = CharacterAdvancement.Buy(finalized with { ExperienceTotal = 100 }, result.Traits,
            TraitCategory.Discipline, result.Details.PredatorSelection!.DisciplineKey);
        Assert.Empty(CharacterRules.Validate(advanced.Details, advanced.Traits));
        Assert.NotEmpty(CharacterRules.Validate(advanced.Details with { PredatorSelection = null }, advanced.Traits));
    }

    [Fact]
    public void Switching_preserves_base_traits_and_other_specializations()
    {
        var (details, traits) = Build();
        var medicine = traits.Single(x => x.Name == "Medicina");
        traits = traits.Select(x => x == medicine ? x with { Specialization = "Cirurgia" } : x).ToArray();
        var sandman = PredatorRules.Apply(details, traits, Choice(PredatorCatalog.Find("sandman")!));
        var alley = PredatorRules.Apply(sandman.Details, sandman.Traits, Choice(PredatorCatalog.Find("alleycat")!));
        Assert.DoesNotContain(alley.Traits, x => x.Key.StartsWith("predator:sandman:"));
        Assert.Equal("Cirurgia", alley.Traits.Single(x => x.Key == medicine.Key).Specialization);
        Assert.Equal(details.Humanity - 1, alley.Details.Humanity);
        var reverted = PredatorRules.Remove(alley.Details, alley.Traits);
        Assert.Equal(details, reverted.Details);
        Assert.Equal(traits.Where(x => x.Rating > 0).OrderBy(x => x.Key), reverted.Traits.Where(x => x.Rating > 0).OrderBy(x => x.Key));
        var preexisting = traits.Select(x => x.Key == medicine.Key ? x with { Specialization = "Anestésicos" } : x).ToArray();
        var same = PredatorRules.Apply(details, preexisting, Choice(PredatorCatalog.Find("sandman")!));
        Assert.False(same.Details.PredatorSelection!.SpecializationAdded);
        Assert.Equal("Anestésicos", PredatorRules.Remove(same.Details, same.Traits).Traits.Single(x => x.Key == medicine.Key).Specialization);
        Assert.Empty(CharacterRules.Validate(same.Details, same.Traits));
    }

    [Theory]
    [InlineData("bagger", "Ventrue", 1)]
    [InlineData("farmer", "ventrue", 0)]
    [InlineData("farmer", "Gangrel", 3)]
    [InlineData("farmer", "Gangrel", 10)]
    public void Restrictions_reject_forbidden_choices(string id, string clan, int blood)
    {
        var definition = PredatorCatalog.Find(id)!;
        var (details, traits) = Build(definition.Specializations[0].Skill, clan);
        Assert.Throws<InvalidOperationException>(() => PredatorRules.Apply(details with { BloodPotency = blood }, traits, Choice(definition)));
    }

    [Theory]
    [InlineData("scene-queen", 1)]
    [InlineData("blood-leech", 1)]
    [InlineData("osiris", 1)]
    [InlineData("osiris", 2)]
    [InlineData("osiris", 3)]
    [InlineData("osiris", 4)]
    public void Alternative_and_mythic_flaws_follow_the_selected_allocation(string id, int index)
    {
        var definition = PredatorCatalog.Find(id)!;
        var (details, traits) = Build(definition.Specializations[0].Skill, "Tremere");
        var allocation = new int[definition.FlawOptions.Length];
        allocation[index] = id == "osiris" && index < 4 ? 1 : definition.FlawPoints;
        allocation[0] = definition.FlawPoints - allocation[index];
        var result = PredatorRules.Apply(details, traits, Choice(definition) with { FlawAllocation = allocation });
        Assert.Empty(CharacterRules.Validate(result.Details, result.Traits));
        Assert.Contains(result.Details.PredatorSelection!.Grants,
            x => x.Name == definition.FlawOptions[index].Name && x.Rating == allocation[index]);
        Assert.Empty(CharacterAdvancement.CreationIssues(result.Details, result.Traits));
    }

    [Fact]
    public void Rejects_invalid_allocations_free_text_clan_and_tampered_grants()
    {
        var (details, traits) = Build("Ocultismo");
        var osiris = PredatorCatalog.Find("osiris")!;
        Assert.DoesNotContain("Feitiçaria de Sangue", PredatorCatalog.DisciplinesFor(osiris, "Banu Haqim"));
        Assert.Throws<InvalidOperationException>(() => PredatorRules.Apply(details, traits, Choice(osiris)));
        var valid = Choice(osiris, "Presença") with { AdvantageAllocation = [1, 2], FlawAllocation = [0, 1, 1, 0, 0] };
        var result = PredatorRules.Apply(details, traits, valid);
        Assert.Empty(CharacterRules.Validate(result.Details, result.Traits));
        var partial = PredatorRules.Apply(details, traits, valid with { AdvantageAllocation = [1, 1], FlawAllocation = [0, 0, 0, 0, 0] });
        Assert.Equal((1, 2), PredatorRules.Unallocated(partial.Details.PredatorSelection!));
        Assert.Contains(CharacterAdvancement.CreationIssues(partial.Details, partial.Traits),
            x => x.Contains("distribua 1 ponto(s) de vantagem e 2 ponto(s) de defeito"));
        Assert.Throws<InvalidOperationException>(() => PredatorRules.Apply(details, traits, valid with { AdvantageAllocation = [2, 2] }));
        Assert.Throws<InvalidOperationException>(() => PredatorRules.Apply(details, traits, valid with { FlawAllocation = [0, 0, 0, 0, 1] }));
        Assert.Throws<InvalidOperationException>(() => PredatorRules.Apply(details, traits, valid with { SpecializationText = "" }));
        Assert.Throws<InvalidOperationException>(() => PredatorRules.Apply(details, traits, valid with { SpecializationText = "Uma; duas" }));
        var tremere = PredatorRules.Apply(details with { Clan = "Tremere" }, traits, valid with { Discipline = "Feitiçaria de Sangue" });
        Assert.NotEmpty(CharacterRules.Validate(tremere.Details with { Clan = "Ventrue" }, tremere.Traits));
        var changed = result.Traits.Select(x => PredatorRules.IsGrant(x) ? x with { Rating = x.Rating + 1 } : x).ToArray();
        Assert.NotEmpty(CharacterRules.Validate(result.Details, changed));
        Assert.Throws<InvalidOperationException>(() => PredatorRules.Remove(result.Details, changed));
        var zeroSkill = traits.Select(x => x.Name == "Ocultismo" ? x with { Rating = 0 } : x).ToArray();
        Assert.Throws<InvalidOperationException>(() => PredatorRules.Apply(details, zeroSkill, valid));
        Assert.Throws<InvalidOperationException>(() => PredatorRules.Apply(details with { Humanity = 10 }, traits,
            Choice(PredatorCatalog.Find("farmer")!)));
    }

    [Fact]
    public void Predator_applies_without_discipline_or_specialization_and_asks_for_them()
    {
        var definition = PredatorCatalog.Find("blood-leech")!;
        var (details, traits) = Build("Briga");
        var choice = Choice(definition) with { Discipline = "", SpecializationIndex = -1 };
        var result = PredatorRules.Apply(details, traits, choice);
        Assert.Empty(CharacterRules.Validate(result.Details, result.Traits));
        Assert.Equal("", result.Details.PredatorSelection!.DisciplineKey);
        Assert.Equal("", result.Details.PredatorSelection.SkillKey);
        var issues = CharacterAdvancement.CreationIssues(result.Details, result.Traits);
        Assert.Contains(issues, x => x.Contains("disciplina que recebe +1"));
        Assert.Contains(issues, x => x.Contains("especializacao gratuita"));
        Assert.Equal(traits.Where(x => x.Category == TraitCategory.Discipline),
            result.Traits.Where(x => x.Category == TraitCategory.Discipline));
        var removed = PredatorRules.Remove(result.Details, result.Traits);
        Assert.Equal(details, removed.Details);
        var unrated = traits.Select(x => x.Category == TraitCategory.Skill && x.Name == "Briga" ? x with { Rating = 0 } : x).ToArray();
        var error = Assert.Throws<InvalidOperationException>(() => PredatorRules.Apply(details, unrated, Choice(definition) with { SpecializationIndex = 0 }));
        Assert.Contains("pelo menos um ponto em Briga", error.Message);
    }

    [Fact]
    public async Task Predator_tags_add_the_free_dot_and_specialization_and_rows_reallocate_flaws()
    {
        var directory = Path.Combine(Path.GetTempPath(), "VCS-predator-tags-" + Guid.NewGuid().ToString("N"));
        try
        {
            var database = new DatabaseAccess(Path.Combine(directory, "test.db"));
            await new SqliteDatabaseMaintenance(database).InitializeAsync();
            var service = new CharacterService(new SqliteCharacterRepository(database));
            using var autosave = new ManualAutosave();
            using var editor = new CharacterEditorViewModel(await service.CreateAsync(), service, NullLogger<CharacterEditorViewModel>.Instance,
                autosave, new ProvidedDisciplineCatalog());
            editor.ConfigureSkillCatalog(await new SqliteSkillCatalog(database).ListAsync());
            editor.ConfigureAdvantageCatalog(await new SqliteAdvantageCatalog(database).ListAsync());
            editor.ConfigurePredatorCatalog(await new SqlitePredatorCatalog(database).ListAsync());
            Assert.Null(editor.SelectedPredatorType);
            editor.Clan = "Brujah";
            var brawl = editor.Skills.Items.Single(x => x.Name == "Briga");
            brawl.Rating = 1;
            editor.SelectedPredatorType = editor.PredatorTypes.Single(x => x.Id == "blood-leech");
            Assert.True(editor.HasAppliedPredator, editor.Error);

            var celerity = editor.Disciplines.Items.Single(x => x.Name == "Celeridade");
            Assert.True(celerity.IsPredatorChoice);
            Assert.DoesNotContain(editor.Disciplines.Items, x => x.IsPredatorChoice && x.Name is not ("Celeridade" or "Proteanismo"));
            var rating = celerity.Rating;
            editor.TogglePredatorDisciplineCommand.Execute(celerity);
            Assert.Equal(rating + 1, celerity.Rating);
            Assert.True(celerity.HasPredatorBonus);
            editor.TogglePredatorDisciplineCommand.Execute(celerity);
            Assert.Equal(rating, celerity.Rating);
            Assert.False(celerity.HasPredatorBonus);
            editor.TogglePredatorDisciplineCommand.Execute(celerity);

            Assert.True(brawl.IsPredatorChoice);
            editor.TogglePredatorSpecializationCommand.Execute(brawl);
            Assert.True(brawl.HasPredatorBonus, editor.Error);
            Assert.Equal("Membros", brawl.PredatorSpecialization);
            Assert.Contains("Membros", brawl.Specialization);

            // Unallocated predator points show every option; they hide once spent and come back when freed.
            var rows = editor.AdvantageCatalog!.FlawRows;
            var pending = rows.Where(x => x.IsPending).ToList();
            Assert.Equal(2, pending.Count);
            Assert.All(pending, x => Assert.Contains("2 pontos livres", x.Subtitle));
            var name = pending[1].Name;
            pending[1].Rating = 2;
            Assert.Equal("", editor.Error);
            Assert.DoesNotContain(rows, x => x.IsPending);
            var chosen = rows.Single(x => x.IsPredator && x.CanReallocate);
            Assert.Equal(name, chosen.Name);
            Assert.Equal(2, chosen.Rating);
            chosen.Rating = 0;
            Assert.Equal(2, rows.Count(x => x.IsPending));
            rows.First(x => x.IsPending).Rating = 2;
            Assert.Equal("", editor.Error);
            Assert.Equal(celerity.Rating, rating + 1);
            Assert.True(brawl.HasPredatorBonus);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Pc_and_npc_persist_autosave_duplicate_json_backup_pdf_and_age_bonus(bool npc)
    {
        var directory = Path.Combine(Path.GetTempPath(), "VCS-predator-" + Guid.NewGuid().ToString("N"));
        try
        {
            var database = new DatabaseAccess(Path.Combine(directory, "test.db"));
            var maintenance = new SqliteDatabaseMaintenance(database);
            await maintenance.InitializeAsync();
            var service = new CharacterService(new SqliteCharacterRepository(database));
            var dto = npc ? await service.CreateNpcAsync() : await service.CreateAsync();
            using var autosave = new ManualAutosave();
            using var editor = new CharacterEditorViewModel(dto, service, NullLogger<CharacterEditorViewModel>.Instance,
                autosave, new ProvidedDisciplineCatalog());
            editor.ConfigureSkillCatalog(await new SqliteSkillCatalog(database).ListAsync());
            editor.ConfigureAdvantageCatalog(await new SqliteAdvantageCatalog(database).ListAsync());
            editor.ConfigurePredatorCatalog(await new SqlitePredatorCatalog(database).ListAsync());
            Assert.Equal(13, editor.PredatorTypes.Count);
            editor.Clan = "Brujah";
            editor.Skills.Items.Single(x => x.Name == "Briga").Rating = 1;
            editor.SelectedPredatorType = editor.PredatorTypes.Single(x => x.Id == "blood-leech");
            Assert.Equal(!npc, editor.HasAppliedPredator);
            foreach (var flaw in editor.PredatorFlaws) flaw.Points = 0;
            editor.PredatorFlaws[1].Points = 2;
            editor.SelectedPredatorSpecialization = editor.PredatorSpecializations.First(x => x.Skill == "Briga");
            editor.ApplyPredatorCommand.Execute(null);
            Assert.Equal("", editor.Error);
            Assert.Equal(1, editor.BloodPotency);
            editor.AgeIndex = 2;
            Assert.Equal(3, editor.BloodPotency);
            editor.Stains = 2;
            editor.ApplyStainDamageCommand.Execute(null);
            Assert.Equal(1, editor.WillpowerAggravated);
            editor.FailRemorseCommand.Execute(null);
            Assert.Equal(5, editor.Humanity);
            Assert.Equal(0, editor.Stains);
            await autosave.FireAsync();
            Assert.False(editor.IsDirty, editor.Error + editor.ValidationSummary);
            var saved = await service.GetAsync(dto.Id);
            Assert.NotNull(saved.Details.PredatorSelection);
            Assert.Equal(3, saved.Details.BloodPotency);
            var copy = await service.DuplicateAsync(dto.Id);
            Assert.Equal(saved.Details, copy.Details with { Name = saved.Details.Name });
            var files = new JsonCharacterFiles();
            var json = Path.Combine(directory, "sheet.json");
            await files.ExportAsync(saved, json);
            var imported = await service.ImportAsync(await files.ReadAsync(json));
            Assert.Equal(saved.Details, imported.Details);
            var backup = Path.Combine(directory, "backup.db");
            await maintenance.BackupAsync(backup);
            editor.RemovePredatorCommand.Execute(null);
            Assert.Equal(2, editor.BloodPotency);
            await autosave.FireAsync();
            Assert.False(editor.IsDirty, editor.Error);
            await maintenance.RestoreAsync(backup);
            var restored = await service.GetAsync(dto.Id);
            Assert.Equal(saved.Details, restored.Details);
            var pdf = Path.Combine(directory, "sheet.pdf");
            await new CharacterPdfExporter(new ProvidedDisciplineCatalog(), new SqliteSkillCatalog(database))
                .ExportAsync(restored, pdf, [new("Identity", "Identidade"), new("State", "Estado")]);
            using var document = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
            var fields = document.AcroForm.Elements.GetArray("/Fields")!.Elements.Cast<PdfReference>().Select(x => (PdfDictionary)x.Value).ToArray();
            Assert.Contains(fields, x => x.Elements.GetString("/V") == "Sanguessuga");
            Assert.Contains(fields, x => x.Elements.GetString("/V") == "Briga (Membros)");
            // Humanity is not part of the simple NPC sheet, so only the player PDF carries it.
            Assert.Equal(!npc, fields.Any(x => x.Elements.GetString("/TU") == "Maculas"));
            Assert.Equal(!npc, fields.Any(x => x.Elements.GetString("/TU") == "Efeitos da Humanidade atual"));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task Migration_preserves_old_predator_text_without_retroactive_effects()
    {
        var directory = Path.Combine(Path.GetTempPath(), "VCS-old-predator-" + Guid.NewGuid().ToString("N"));
        try
        {
            var database = new DatabaseAccess(Path.Combine(directory, "test.db"));
            var maintenance = new SqliteDatabaseMaintenance(database);
            await maintenance.InitializeAsync();
            var service = new CharacterService(new SqliteCharacterRepository(database));
            var dto = await service.CreateAsync();
            dto = await service.SaveAsync(dto with { Details = dto.Details with { Predator = "Sanguessuga", Humanity = 8 } });
            await using (var db = database.CreateContext())
                await db.GetService<IMigrator>().MigrateAsync("20261018023515_LoresheetsAndCampaignXp");
            await maintenance.InitializeAsync();
            var saved = await service.GetAsync(dto.Id);
            Assert.Equal("Sanguessuga", saved.Details.Predator);
            Assert.Equal(8, saved.Details.Humanity);
            Assert.Equal(0, saved.Details.BloodPotency);
            Assert.Equal(0, saved.Details.Stains);
            Assert.Null(saved.Details.PredatorSelection);
            Assert.Equal(13, (await new SqlitePredatorCatalog(database).ListAsync()).Count);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
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
