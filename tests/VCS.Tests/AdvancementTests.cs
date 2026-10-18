using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;
using VCS.Application.Characters;
using VCS.Desktop.Services;
using VCS.Desktop.ViewModels;
using VCS.Domain.Characters;
using VCS.Infrastructure.Files;
using VCS.Infrastructure.Persistence;
using VCS.Infrastructure.Repositories;

namespace VCS.Tests;

public sealed class AdvancementTests
{
    private sealed class ManualAutosave : IAutosaveScheduler
    {
        private Func<Task>? _pending;
        public void Schedule(Func<Task> action) => _pending = action;
        public async Task FireAsync() { var action = _pending; _pending = null; if (action is not null) await action(); }
        public void Cancel() => _pending = null;
        public void Dispose() => _pending = null;
    }
    private static (CharacterDetails Details, TraitValue[] Traits) Build(VampireAge age = VampireAge.Neonate,
        SkillProfile profile = SkillProfile.Specialist)
    {
        var ratings = new[] { 4, 3, 3, 3, 2, 2, 2, 2, 1 };
        var traits = CharacterRules.DefaultAttributes.Select((x, i) => x with { Rating = ratings[i] })
            .Concat(CharacterAdvancement.SkillDistribution(profile).Select((rating, i) =>
                new TraitValue(TraitCategory.Skill, "skill" + i, i == 0 ? "Ciencia" : "Habilidade " + i, rating)))
            .Concat(new TraitValue[] { new(TraitCategory.Discipline, "presence", "Presença", 2),
                new(TraitCategory.Discipline, "celerity", "Celeridade", 1),
                new(TraitCategory.Discipline, "obfuscate", "Ofuscação", 0),
                new(TraitCategory.Advantage, "merit1", "Vantagem 1", 5),
                new(TraitCategory.Advantage, "merit2", "Vantagem 2", CharacterAdvancement.AdvantageBudget(age) - 5),
                new(TraitCategory.Flaw, "flaw", "Defeito", CharacterAdvancement.FlawBudget(age)) }).ToArray();
        return (new()
        {
            Clan = "Brujah", BloodPotency = CharacterAdvancement.StartingBlood(age),
            ExperienceTotal = CharacterAdvancement.StartingXp(age),
            Advancement = new() { Age = age, Profile = profile }
        }, traits);
    }

    [Theory]
    [InlineData(VampireAge.Child, SkillProfile.Specialist, 0, 0, 7, 2)]
    [InlineData(VampireAge.Neonate, SkillProfile.Balanced, 15, 1, 7, 2)]
    [InlineData(VampireAge.Ancilla, SkillProfile.Generalist, 35, 2, 9, 4)]
    public void Initial_budgets_are_free_and_each_skill_profile_is_independent(VampireAge age, SkillProfile profile,
        int xp, int blood, int merits, int flaws)
    {
        var (details, traits) = Build(age, profile);
        Assert.Empty(CharacterRules.Validate(details, traits));
        Assert.Empty(CharacterAdvancement.CreationIssues(details, traits));
        details = CharacterAdvancement.FinalizeCreation(details, traits);
        Assert.False(details.Advancement!.IsCreating);
        Assert.Equal(xp, details.ExperienceTotal);
        Assert.Equal(blood, details.BloodPotency);
        Assert.Equal(0, details.ExperienceSpent);
        Assert.Equal(merits, traits.Where(x => x.Category == TraitCategory.Advantage).Sum(x => x.Rating));
        Assert.Equal(flaws, traits.Where(x => x.Category == TraitCategory.Flaw).Sum(x => x.Rating));
        Assert.Empty(CharacterRules.Validate(details, traits));
    }

    [Theory]
    [InlineData(TraitCategory.Attribute, "Strength", 25)]
    [InlineData(TraitCategory.Attribute, "Resolve", 10)]
    [InlineData(TraitCategory.Skill, "skill1", 12)]
    [InlineData(TraitCategory.Discipline, "presence", 15)]
    [InlineData(TraitCategory.Discipline, "obfuscate", 7)]
    [InlineData(TraitCategory.Advantage, "merit2", 3)]
    [InlineData(null, "BloodPotency", 20)]
    public void Purchases_charge_exact_next_level_cost_and_preserve_history(TraitCategory? category, string key, int cost)
    {
        var (details, traits) = Build();
        details = CharacterAdvancement.FinalizeCreation(details, traits) with { ExperienceTotal = 100 };
        var result = CharacterAdvancement.Buy(details, traits, category, key);
        Assert.Equal(cost, result.Details.ExperienceSpent);
        Assert.Equal(cost, Assert.Single(result.Details.Advancement!.Purchases).Cost);
        Assert.Empty(CharacterRules.Validate(result.Details, result.Traits));
        Assert.NotEmpty(CharacterRules.Validate(result.Details with { ExperienceSpent = 0 }, result.Traits));
        if (category is not null) Assert.NotEmpty(CharacterRules.Validate(result.Details, traits));
        else Assert.NotEmpty(CharacterRules.Validate(result.Details with { BloodPotency = details.BloodPotency }, traits));
    }

    [Fact]
    public void Rejects_invalid_creation_insufficient_xp_maximum_and_unknown_clan()
    {
        var (details, traits) = Build();
        var changed = traits.Select(x => x.Key == "Strength" ? x with { Rating = 5 } : x).ToArray();
        Assert.Throws<InvalidOperationException>(() => CharacterAdvancement.FinalizeCreation(details, changed));
        Assert.Throws<InvalidOperationException>(() => CharacterAdvancement.Buy(details, traits, TraitCategory.Attribute, "Resolve"));
        details = CharacterAdvancement.FinalizeCreation(details, traits);
        Assert.Throws<InvalidOperationException>(() => CharacterAdvancement.Buy(details, traits, TraitCategory.Attribute, "Strength"));
        Assert.Throws<InvalidOperationException>(() => CharacterAdvancement.Buy(details with { Clan = "Personalizado" }, traits, TraitCategory.Discipline, "presence"));
        var first = CharacterAdvancement.Buy(details with { ExperienceTotal = 100 }, traits, TraitCategory.Attribute, "Strength");
        Assert.Throws<InvalidOperationException>(() => CharacterAdvancement.Buy(first.Details, first.Traits, TraitCategory.Attribute, "Strength"));
    }

    [Fact]
    public void Free_specializations_accumulate_and_subsequent_ones_cost_three_xp()
    {
        var (details, traits) = Build();
        details = CharacterAdvancement.FinalizeCreation(details, traits) with { ExperienceTotal = 100, Predator = "A cadastrar" };
        // Ciencia 4: primeiro ponto + nivel 4; ficha + predador: mais duas.
        Assert.Equal(4, CharacterAdvancement.FreeSpecializationsRemaining(details, traits));
        IReadOnlyList<TraitValue> values = traits;
        for (var i = 0; i < 5; i++)
        {
            var result = CharacterAdvancement.Buy(details, values, TraitCategory.Skill, "skill0", "Especialidade " + i);
            details = result.Details; values = result.Traits;
            Assert.Equal(i == 4 ? 3 : 0, details.ExperienceSpent);
            Assert.Empty(CharacterRules.Validate(details, values));
        }
        Assert.Equal(0, CharacterAdvancement.FreeSpecializationsRemaining(details, values));
        Assert.Throws<InvalidOperationException>(() => CharacterAdvancement.Buy(details, values, TraitCategory.Skill, "skill0", "Especialidade 0"));
        Assert.Throws<InvalidOperationException>(() => CharacterAdvancement.Buy(details, values, TraitCategory.Skill, "skill0", "Duas; Outras"));
    }

    [Theory]
    [InlineData("Academics")]
    [InlineData("Erudição")]
    [InlineData("Acadêmicos")]
    [InlineData("Ofícios")]
    [InlineData("Performance")]
    [InlineData("Ciência")]
    public void First_point_and_fourth_point_grants_are_local_and_cumulative(string skill)
    {
        Assert.Equal(0, CharacterAdvancement.LocalFreeSpecializations(new(TraitCategory.Skill, "test", skill, 0)));
        Assert.Equal(1, CharacterAdvancement.LocalFreeSpecializations(new(TraitCategory.Skill, "test", skill, 1)));
        Assert.Equal(2, CharacterAdvancement.LocalFreeSpecializations(new(TraitCategory.Skill, "test", skill, 4)));
    }

    [Fact]
    public void Reaching_four_and_buying_new_merit_and_npc_group_work()
    {
        var (details, traits) = Build();
        details = CharacterAdvancement.FinalizeCreation(details with { IsNpc = true }, traits) with { ExperienceTotal = 100 };
        var before = CharacterAdvancement.FreeSpecializationsRemaining(details, traits);
        var result = CharacterAdvancement.Buy(details, traits, TraitCategory.Skill, "skill1");
        Assert.Equal(before + 1, CharacterAdvancement.FreeSpecializationsRemaining(result.Details, result.Traits));
        result = CharacterAdvancement.Buy(result.Details, result.Traits, TraitCategory.Advantage, "newmerit", newTraitName: "Nova vantagem");
        Assert.Equal(1, result.Traits.Single(x => x.Key == "newmerit").Rating);
        result = CharacterAdvancement.Buy(result.Details, result.Traits, null, "NpcPhysical");
        Assert.Equal(1, result.Details.NpcPhysical);
        Assert.Equal(20, result.Details.ExperienceSpent);
        Assert.Empty(CharacterRules.Validate(result.Details, result.Traits));
    }

    [Fact]
    public async Task Editor_purchases_follow_sqlite_duplicate_json_backup_and_pdf()
    {
        var directory = Path.Combine(Path.GetTempPath(), "VCS-advancement-" + Guid.NewGuid().ToString("N"));
        try
        {
            var database = new DatabaseAccess(Path.Combine(directory, "test.db"));
            var maintenance = new SqliteDatabaseMaintenance(database);
            await maintenance.InitializeAsync();
            var service = new CharacterService(new SqliteCharacterRepository(database));
            var dto = await service.CreateAsync();
            var (details, traits) = Build();
            dto = await service.SaveAsync(dto with { Details = details, Traits = traits });
            var autosave = new ManualAutosave();
            using var editor = new CharacterEditorViewModel(dto, service, NullLogger<CharacterEditorViewModel>.Instance,
                autosave, new ProvidedDisciplineCatalog());
            Assert.True(editor.CanEditBuild);
            editor.CampaignExperienceTotal = 100;
            editor.Attributes.Items.Single(x => x.Key == "Resolve").Rating = 2;
            Assert.Equal(10, editor.ExperienceSpent);
            Assert.Contains(editor.ExperienceBreakdown, x => x.EndsWith(": 10 XP.", StringComparison.Ordinal));
            await autosave.FireAsync();
            Assert.False(editor.IsDirty);
            var saved = await service.GetAsync(dto.Id);
            Assert.Equal(10, saved.Details.ExperienceSpent);
            var copy = await service.DuplicateAsync(dto.Id);
            Assert.Equal(saved.Details.Advancement!.Purchases, copy.Details.Advancement!.Purchases);
            var files = new JsonCharacterFiles();
            var path = Path.Combine(directory, "sheet.json");
            await files.ExportAsync(saved, path);
            var imported = await service.ImportAsync(await files.ReadAsync(path));
            Assert.Equal(10, imported.Details.ExperienceSpent);
            Assert.True(imported.Details.Advancement!.AutomaticExperience);
            var backup = Path.Combine(directory, "backup.db");
            await maintenance.BackupAsync(backup);
            editor.Attributes.Items.Single(x => x.Key == "Resolve").Rating = 3;
            Assert.Equal(25, editor.ExperienceSpent);
            await autosave.FireAsync();
            await maintenance.RestoreAsync(backup);
            var restored = await service.GetAsync(dto.Id);
            Assert.Equal(10, restored.Details.ExperienceSpent);
            var pdf = Path.Combine(directory, "sheet.pdf");
            await new CharacterPdfExporter(new ProvidedDisciplineCatalog(), new SqliteSkillCatalog(database))
                .ExportAsync(restored, pdf, [new("Background", "Historia e conviccoes")]);
            using var document = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
            var fields = document.AcroForm.Elements.GetArray("/Fields")!.Elements.Cast<PdfReference>().Select(x => (PdfDictionary)x.Value).ToArray();
            Assert.Contains(fields, x => x.Elements.GetString("/TU") == "Total gasto de XP" && x.Elements.GetString("/V") == "10");
            Assert.Contains(fields, x => x.Elements.GetString("/V").Contains(": 10 XP."));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task Old_database_keeps_existing_xp_and_can_establish_a_baseline_without_charges()
    {
        var directory = Path.Combine(Path.GetTempPath(), "VCS-old-advancement-" + Guid.NewGuid().ToString("N"));
        try
        {
            var database = new DatabaseAccess(Path.Combine(directory, "test.db"));
            var maintenance = new SqliteDatabaseMaintenance(database);
            await maintenance.InitializeAsync();
            var repository = new SqliteCharacterRepository(database);
            var legacy = Character.Create(new() { ExperienceTotal = 80, ExperienceSpent = 20, BloodPotency = 2 });
            await repository.SaveAsync(legacy, null);
            await using (var context = database.CreateContext())
            {
                await context.GetService<IMigrator>().MigrateAsync("20261018015137_SkillSpecializations");
            }
            await maintenance.InitializeAsync();
            var service = new CharacterService(repository);
            var dto = await service.GetAsync(legacy.Id);
            Assert.Null(dto.Details.Advancement);
            Assert.Equal(20, dto.Details.ExperienceSpent);
            var tracked = CharacterAdvancement.EstablishBaseline(dto.Details, dto.Traits);
            Assert.Equal(20, tracked.Advancement!.BaselineSpent);
            Assert.Empty(tracked.Advancement.Purchases);
            var purchase = CharacterAdvancement.Buy(tracked, dto.Traits, TraitCategory.Attribute, "Strength");
            Assert.Equal(30, purchase.Details.ExperienceSpent);
            Assert.Empty(CharacterRules.Validate(purchase.Details, purchase.Traits));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task Age_changes_adjust_bonus_once_and_npc_rules_remain_optional()
    {
        var directory = Path.Combine(Path.GetTempPath(), "VCS-age-" + Guid.NewGuid().ToString("N"));
        try
        {
            var database = new DatabaseAccess(Path.Combine(directory, "test.db"));
            await new SqliteDatabaseMaintenance(database).InitializeAsync();
            var service = new CharacterService(new SqliteCharacterRepository(database));
            using var editor = new CharacterEditorViewModel(await service.CreateAsync(), service,
                NullLogger<CharacterEditorViewModel>.Instance, new ManualAutosave(), new ProvidedDisciplineCatalog());
            editor.AgeIndex = 1;
            Assert.Equal(15, editor.ExperienceTotal); Assert.Equal(1, editor.BloodPotency);
            editor.AgeIndex = 1;
            Assert.Equal(15, editor.ExperienceTotal);
            editor.CampaignExperienceTotal = 10;
            editor.AgeIndex = 2;
            Assert.Equal(45, editor.ExperienceTotal); Assert.Equal(2, editor.BloodPotency);
            editor.SkillProfileIndex = 2;
            Assert.True(await editor.FlushAsync());
            var saved = await service.GetAsync(editor.Id);
            Assert.Equal(VampireAge.Ancilla, saved.Details.Advancement!.Age);
            Assert.Equal(SkillProfile.Generalist, saved.Details.Advancement.Profile);
            Assert.True(editor.CanEditBuild);
            Assert.Empty(editor.Error);
            using var npc = new CharacterEditorViewModel(await service.CreateNpcAsync(), service,
                NullLogger<CharacterEditorViewModel>.Instance, new ManualAutosave(), new ProvidedDisciplineCatalog());
            Assert.True(npc.CanEditBuild);
            npc.NpcPhysical = 4;
            npc.CampaignExperienceTotal = 50;
            npc.Disciplines.Items.Single(x => x.Name == "Ofuscação").Rating = 1;
            Assert.Contains("cla", npc.DisciplinesExperience);
            npc.Clan = "Brujah";
            Assert.Equal(7, npc.ExperienceSpent);
            Assert.True(await npc.FlushAsync());
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
