using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;
using VCS.Application.Characters;
using VCS.Desktop.Catalogs;
using VCS.Desktop.Sheet;
using VCS.Domain.Advancement;
using VCS.Domain.Advantages;
using VCS.Domain.Characters;
using VCS.Infrastructure.Catalogs;
using VCS.Infrastructure.Catalogs.Seed;
using VCS.Infrastructure.Characters;
using VCS.Infrastructure.Persistence;

namespace VCS.Tests;

public sealed class LoresheetTests
{
    private static TraitValue Benefit(int points = 5, string key = "lore-test", bool lineage = false) =>
        new(TraitCategory.Loresheet, key, "Beneficio " + key, points,
            Loresheet: new("Colecao " + key, lineage, "Descricao do beneficio.", LoresheetPayment.AdvantagePoints));
    private static CharacterDetails Creating(VampireAge age = VampireAge.Ancilla) =>
        new() { Clan = "Brujah", BloodPotency = CharacterAdvancement.StartingBlood(age),
            ExperienceTotal = CharacterAdvancement.StartingXp(age), Advancement = new() { Age = age } };

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Each_benefit_is_independent_and_initial_points_or_xp_are_distinct(int points)
    {
        var details = Creating();
        var benefit = Benefit(points);
        var fromPoints = LoresheetRules.Select(details, CharacterRules.DefaultAttributes, benefit, LoresheetPayment.AdvantagePoints);
        Assert.Equal(points, LoresheetRules.AdvantagePoints(fromPoints.Traits));
        Assert.Equal(0, fromPoints.Details.ExperienceSpent);
        Assert.Single(fromPoints.Traits, x => x.Category == TraitCategory.Loresheet);
        Assert.Empty(CharacterRules.Validate(fromPoints.Details, fromPoints.Traits));
        var fromXp = LoresheetRules.Select(details, CharacterRules.DefaultAttributes, benefit, LoresheetPayment.InitialXp);
        Assert.Equal(points * 3, fromXp.Details.ExperienceSpent);
        Assert.Equal(0, LoresheetRules.AdvantagePoints(fromXp.Traits));
        Assert.Equal(0, fromXp.Details.CampaignExperienceSpent);
        Assert.Empty(CharacterRules.Validate(fromXp.Details, fromXp.Traits));
        var removed = LoresheetRules.RemoveInitial(fromXp.Details, fromXp.Traits, benefit.Key);
        Assert.Equal(0, removed.Details.ExperienceSpent);
        Assert.DoesNotContain(removed.Traits, x => x.Category == TraitCategory.Loresheet);
    }

    [Fact]
    public void Automatic_creation_pays_extra_loresheets_with_xp_and_budgets_are_minimums()
    {
        var details = Creating() with { Advancement = new() { Age = VampireAge.Ancilla, AutomaticExperience = true } };
        var general = LoresheetRules.Select(details, CharacterRules.DefaultAttributes, Benefit(2), LoresheetPayment.AdvantagePoints);
        var lineage = LoresheetRules.Select(general.Details, general.Traits, Benefit(1, "lineage", true), LoresheetPayment.AdvantagePoints);
        var extra = LoresheetRules.Select(lineage.Details, lineage.Traits, Benefit(1, "extra"), LoresheetPayment.AdvantagePoints);
        Assert.Equal(LoresheetPayment.InitialXp, extra.Traits.Single(x => x.Key == "extra").Loresheet!.Payment);
        Assert.Equal(3, CharacterAdvancement.AdvantagePoints(extra.Traits));
        var issues = CharacterAdvancement.CreationIssues(extra.Details, extra.Traits);
        Assert.Contains("Vantagens: faltam 6 de 9 pontos.", issues);
        Assert.Contains("Desvantagens: faltam 4 de 4 pontos.", issues);
        var neonate = details with { Advancement = details.Advancement! with { Age = VampireAge.Neonate } };
        var full = CharacterRules.DefaultAttributes.Concat([new TraitValue(TraitCategory.Advantage, "a", "Recursos", 5),
            new TraitValue(TraitCategory.Advantage, "b", "Contatos", 4), new TraitValue(TraitCategory.Flaw, "c", "Presa", 2)]).ToArray();
        Assert.Equal(9, CharacterAdvancement.AdvantagePoints(full));
        Assert.DoesNotContain(CharacterAdvancement.CreationIssues(neonate, full), x => x.StartsWith("Vantagens") || x.StartsWith("Desvantagens"));
        var exact = full.Select(x => x.Key == "b" ? x with { Rating = 2 } : x).ToArray();
        Assert.Equal(6, AutomaticExperience.Calculate(neonate, full).Spent - AutomaticExperience.Calculate(neonate, exact).Spent);
    }
    [Fact]
    public void Creation_limits_are_one_general_and_one_lineage_not_one_per_collection()
    {
        var initial = LoresheetRules.Select(Creating(), CharacterRules.DefaultAttributes, Benefit(2), LoresheetPayment.InitialXp);
        Assert.Throws<InvalidOperationException>(() =>
            LoresheetRules.Select(initial.Details, initial.Traits, Benefit(1, "another-collection"), LoresheetPayment.AdvantagePoints));
        var lineage = LoresheetRules.Select(initial.Details, initial.Traits, Benefit(1, "lineage", true), LoresheetPayment.AdvantagePoints);
        Assert.Equal(2, lineage.Traits.Count(x => x.Category == TraitCategory.Loresheet));
        Assert.Empty(CharacterRules.Validate(lineage.Details, lineage.Traits));
        Assert.Throws<InvalidOperationException>(() =>
            LoresheetRules.Select(lineage.Details, lineage.Traits, Benefit(1, "second-lineage", true), LoresheetPayment.InitialXp));
        var bypass = lineage.Traits.Append(Benefit(1, "bypass")).ToArray();
        Assert.NotEmpty(CharacterRules.Validate(lineage.Details, bypass));
    }

    [Fact]
    public void Creation_requires_available_points_or_real_age_xp_and_cannot_use_campaign()
    {
        var child = Creating(VampireAge.Child) with { ExperienceTotal = 100, CampaignExperienceTotal = 100 };
        Assert.Throws<InvalidOperationException>(() => LoresheetRules.Select(child, CharacterRules.DefaultAttributes, Benefit(1), LoresheetPayment.InitialXp));
        Assert.Throws<InvalidOperationException>(() => LoresheetRules.Select(child, CharacterRules.DefaultAttributes, Benefit(1), LoresheetPayment.CampaignXp));
        TraitValue[] full = [.. CharacterRules.DefaultAttributes, new(TraitCategory.Advantage, "a", "Vantagem A", 5),
            new(TraitCategory.Advantage, "b", "Vantagem B", 4)];
        Assert.Throws<InvalidOperationException>(() => LoresheetRules.Select(Creating(), full, Benefit(1), LoresheetPayment.AdvantagePoints));
        var selected = LoresheetRules.Select(Creating(), CharacterRules.DefaultAttributes, Benefit(5), LoresheetPayment.InitialXp);
        Assert.NotEmpty(CharacterRules.Validate(selected.Details with { Advancement = selected.Details.Advancement! with { Age = VampireAge.Child } }, selected.Traits));
        Assert.NotEmpty(CharacterRules.Validate(selected.Details with { Advancement = selected.Details.Advancement! with { Age = (VampireAge)99 } }, selected.Traits));
    }

    [Theory]
    [InlineData(VampireAge.Neonate)]
    [InlineData(VampireAge.Ancilla)]
    public void After_creation_age_xp_cannot_buy_loresheets_but_campaign_can(VampireAge age)
    {
        var details = CharacterAdvancement.EstablishBaseline(Creating(age), CharacterRules.DefaultAttributes);
        Assert.Throws<InvalidOperationException>(() => LoresheetRules.Select(details, CharacterRules.DefaultAttributes, Benefit(), LoresheetPayment.CampaignXp));
        var mislabeled = details with { CampaignExperienceTotal = details.ExperienceTotal };
        Assert.NotEmpty(CharacterRules.Validate(mislabeled, CharacterRules.DefaultAttributes));
        Assert.Throws<InvalidOperationException>(() => LoresheetRules.Select(mislabeled, CharacterRules.DefaultAttributes, Benefit(1), LoresheetPayment.CampaignXp));
        details = details with { ExperienceTotal = details.ExperienceTotal + 30, CampaignExperienceTotal = 30 };
        var first = LoresheetRules.Select(details, CharacterRules.DefaultAttributes, Benefit(), LoresheetPayment.CampaignXp);
        Assert.Equal(15, first.Details.ExperienceSpent);
        Assert.Equal(15, first.Details.CampaignExperienceSpent);
        Assert.Equal(15, LoresheetRules.CampaignAvailable(first.Details));
        Assert.Empty(CharacterRules.Validate(first.Details, first.Traits));
        var second = LoresheetRules.Select(first.Details, first.Traits, Benefit(5, "other"), LoresheetPayment.CampaignXp);
        Assert.Equal(30, second.Details.CampaignExperienceSpent);
        Assert.Empty(CharacterRules.Validate(second.Details, second.Traits));
        Assert.Throws<InvalidOperationException>(() => LoresheetRules.Select(second.Details, second.Traits, Benefit(1, "third"), LoresheetPayment.CampaignXp));
        Assert.Throws<InvalidOperationException>(() => LoresheetRules.Select(second.Details, second.Traits, Benefit(1, "third"), LoresheetPayment.InitialXp));
        Assert.Throws<InvalidOperationException>(() => LoresheetRules.RemoveInitial(second.Details, second.Traits, "other"));
        Assert.Throws<InvalidOperationException>(() => CharacterAdvancement.Buy(second.Details, second.Traits, TraitCategory.Loresheet, "other"));
    }

    [Fact]
    public void Ordinary_purchases_use_non_campaign_first_and_preserve_exact_source_after_new_awards()
    {
        var traits = CharacterRules.DefaultAttributes;
        var details = CharacterAdvancement.EstablishBaseline(Creating(VampireAge.Neonate), traits)
            with { ExperienceTotal = 35, CampaignExperienceTotal = 20 };
        var first = CharacterAdvancement.Buy(details, traits, TraitCategory.Attribute, "Strength");
        Assert.Equal(0, first.Details.CampaignExperienceSpent);
        var second = CharacterAdvancement.Buy(first.Details, first.Traits, TraitCategory.Attribute, "Strength");
        Assert.Equal(10, second.Details.CampaignExperienceSpent);
        Assert.Equal(10, LoresheetRules.CampaignAvailable(second.Details));
        Assert.Throws<InvalidOperationException>(() => LoresheetRules.Select(second.Details, second.Traits, Benefit(), LoresheetPayment.CampaignXp));
        var awarded = second.Details with { ExperienceTotal = 135, CampaignExperienceTotal = 120 };
        Assert.Empty(CharacterRules.Validate(awarded, second.Traits));
        var third = LoresheetRules.Select(awarded, second.Traits, Benefit(), LoresheetPayment.CampaignXp);
        Assert.Equal(40, third.Details.ExperienceSpent);
        Assert.Equal(25, third.Details.CampaignExperienceSpent);
        Assert.Empty(CharacterRules.Validate(third.Details, third.Traits));
        var tampered = third.Details with { CampaignExperienceSpent = 0 };
        Assert.NotEmpty(CharacterRules.Validate(tampered, third.Traits));
    }

    [Fact]
    public void Creation_finalizes_with_loresheet_points_or_paid_initial_xp()
    {
        foreach (var payWithXp in new[] { false, true })
        {
            var ratings = new[] { 4, 3, 3, 3, 2, 2, 2, 2, 1 };
            TraitValue[] traits = [.. CharacterRules.DefaultAttributes.Select((x, i) => x with { Rating = ratings[i] }),
                .. CharacterAdvancement.SkillDistribution(SkillProfile.Specialist).Select((x, i) => new TraitValue(TraitCategory.Skill, "s" + i, "Habilidade " + i, x)),
                new(TraitCategory.Discipline, "presence", "Presença", 2), new(TraitCategory.Discipline, "celerity", "Celeridade", 1),
                new(TraitCategory.Advantage, "merit-a", "Vantagem A", 5),
                new(TraitCategory.Advantage, "merit-b", "Vantagem B", payWithXp ? 4 : 1),
                new(TraitCategory.Flaw, "flaw", "Defeito", 4)];
            var result = LoresheetRules.Select(Creating(), traits, Benefit(3), payWithXp ? LoresheetPayment.InitialXp : LoresheetPayment.AdvantagePoints);
            Assert.Empty(CharacterAdvancement.CreationIssues(result.Details, result.Traits));
            var finished = CharacterAdvancement.FinalizeCreation(result.Details, result.Traits);
            Assert.Equal(payWithXp ? 9 : 0, finished.ExperienceSpent);
            Assert.Empty(CharacterRules.Validate(finished, result.Traits));
            Assert.Throws<InvalidOperationException>(() => LoresheetRules.Select(finished, result.Traits, Benefit(1, "new"), LoresheetPayment.CampaignXp));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Catalog_selection_and_funding_follow_editor_sqlite_duplicate_json_dump_and_pdf(bool npc)
    {
        var directory = Path.Combine(Path.GetTempPath(), "VCS-lore-" + Guid.NewGuid().ToString("N"));
        try
        {
            var database = new DatabaseAccess(Path.Combine(directory, "test.db"));
            var maintenance = new SqliteDatabaseMaintenance(database);
            await maintenance.InitializeAsync();
            var service = new CharacterService(new SqliteCharacterRepository(database));
            var catalog = await new SqliteAdvantageCatalog(database).ListAsync();
            var lores = catalog.Where(x => x.Category == TraitCategory.Loresheet).ToArray();
            Assert.Equal(20, lores.Length);
            Assert.Equal(new[] { "Carna", "Heresia Cainita", "Os Bahari", "Theo Bell" }, lores.Select(x => x.LoresheetCollection).Distinct().Order());
            Assert.All(lores, x => Assert.False(x.IsLineage));
            foreach (var group in lores.GroupBy(x => x.LoresheetCollection))
                Assert.Equal(new[] { 1, 2, 3, 4, 5 }, group.Select(x => x.Minimum).Order());
            Assert.Equal(ProvidedLoresheets.All.Select(x => x.Description).Order(), lores.Select(x => x.Description).Order());
            var dto = npc ? await service.CreateNpcAsync() : await service.CreateAsync();
            using var editor = new CharacterEditorViewModel(dto, service, NullLogger<CharacterEditorViewModel>.Instance,
                new ManualAutosave(), new ProvidedDisciplineCatalog());
            editor.ConfigureAdvantageCatalog(catalog);
            Assert.Equal(63, editor.AdvantageCatalog!.Families.Sum(x => x.Options.Count));
            Assert.Equal(20, editor.AdvantageCatalog.Families.Single(x => x.Name == AdvantageCatalogViewModel.LoresheetFamily).Options.Count);
            Assert.Equal(20, editor.LoresheetBenefits.Count);
            editor.AgeIndex = 2;
            editor.SelectedLoresheetCollection = "Os Bahari";
            Assert.Equal(5, editor.LoresheetBenefits.Count);
            editor.LoresheetSearch = "amaldiçoada";
            Assert.Single(editor.LoresheetBenefits);
            editor.PayLoresheetWithInitialXp = true;
            editor.AddLoresheetCommand.Execute(null);
            Assert.Equal(15, editor.ExperienceSpent);
            Assert.Single(editor.Loresheets.Items);
            Assert.True(await editor.FlushAsync());
            var initial = await service.GetAsync(editor.Id);
            dto = initial;
            using var evolving = new CharacterEditorViewModel(dto, service, NullLogger<CharacterEditorViewModel>.Instance,
                new ManualAutosave(), new ProvidedDisciplineCatalog());
            evolving.ConfigureAdvantageCatalog(catalog);
            evolving.SelectedLoresheetCollection = "Theo Bell";
            evolving.SelectedLoresheetBenefit = evolving.LoresheetBenefits.Single(x => x.Minimum == 5);
            evolving.PayLoresheetWithInitialXp = true;
            evolving.AddLoresheetCommand.Execute(null);
            Assert.Empty(evolving.Error);
            Assert.Equal(2, evolving.Loresheets.Items.Count);
            evolving.CampaignExperienceTotal = 30;
            Assert.Equal(65, evolving.ExperienceTotal);
            Assert.Equal(30, evolving.ExperienceSpent);
            Assert.Equal(0, evolving.CampaignExperienceSpent);
            Assert.Equal(30, evolving.CampaignExperienceAvailable);
            Assert.True(await evolving.FlushAsync());
            var saved = await service.GetAsync(dto.Id);
            var copy = await service.DuplicateAsync(saved.Id);
            Assert.Equal(saved.Details.Advancement, copy.Details.Advancement);
            Assert.Equal(saved.Traits.OrderBy(x => x.Key), copy.Traits.OrderBy(x => x.Key));
            var json = Path.Combine(directory, "sheet.json");
            var files = new JsonCharacterFiles();
            await files.ExportAsync(saved, json);
            var imported = await service.ImportAsync(await files.ReadAsync(json));
            Assert.Equal(saved.Details, imported.Details);
            Assert.Equal(saved.Traits.OrderBy(x => x.Key), imported.Traits.OrderBy(x => x.Key));
            var backup = Path.Combine(directory, "backup.db");
            await maintenance.BackupAsync(backup);
            evolving.SelectedLoresheetBenefit = evolving.LoresheetBenefits.Single(x => x.Minimum == 1);
            evolving.AddLoresheetCommand.Execute(null);
            Assert.True(await evolving.FlushAsync());
            await maintenance.RestoreAsync(backup);
            var restored = await service.GetAsync(dto.Id);
            Assert.Equal(saved.Details, restored.Details);
            Assert.Equal(2, restored.Traits.Count(x => x.Category == TraitCategory.Loresheet));
            var pdf = Path.Combine(directory, "sheet.pdf");
            await new CharacterPdfExporter(new ProvidedDisciplineCatalog(), new SqliteSkillCatalog(database))
                .ExportAsync(restored, pdf, [new("Advantages", "Vantagens")]);
            using var document = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
            var fields = document.AcroForm.Elements.GetArray("/Fields")!.Elements.Cast<PdfReference>().Select(x => (PdfDictionary)x.Value).ToArray();
            Assert.Contains(fields, x => x.Elements.GetString("/V") == "Amaldiçoada Original");
            Assert.Contains(fields, x => x.Elements.GetString("/V") == "Neutralidade de Seita");
            Assert.Contains(fields, x => x.Elements.GetString("/V") == "15 XP inicial de idade");
            Assert.Equal(2, fields.Count(x => x.Elements.GetString("/V") == "15 XP inicial de idade"));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task Existing_database_retains_xp_and_history_without_classifying_age_xp_as_campaign()
    {
        var directory = Path.Combine(Path.GetTempPath(), "VCS-old-lore-" + Guid.NewGuid().ToString("N"));
        try
        {
            var database = new DatabaseAccess(Path.Combine(directory, "test.db"));
            var maintenance = new SqliteDatabaseMaintenance(database);
            await maintenance.InitializeAsync();
            var repository = new SqliteCharacterRepository(database);
            var details = CharacterAdvancement.EstablishBaseline(Creating(), CharacterRules.DefaultAttributes);
            var purchase = CharacterAdvancement.Buy(details, CharacterRules.DefaultAttributes, TraitCategory.Attribute, "Strength");
            var character = Character.Create(purchase.Details, purchase.Traits);
            await repository.SaveAsync(character, null);
            await using (var db = database.CreateContext())
                await db.GetService<IMigrator>().MigrateAsync("20261018021329_CharacterAdvancement");
            await maintenance.InitializeAsync();
            var reopened = (await repository.GetAsync(character.Id))!;
            Assert.Equal(35, reopened.Details.ExperienceTotal);
            Assert.Equal(10, reopened.Details.ExperienceSpent);
            Assert.Equal(0, reopened.Details.CampaignExperienceTotal);
            Assert.Equal(0, reopened.Details.CampaignExperienceSpent);
            Assert.Empty(CharacterRules.Validate(reopened.Details, reopened.Traits.Select(x => x.ToValue()).ToArray()));
            Assert.Throws<InvalidOperationException>(() => LoresheetRules.Select(reopened.Details,
                reopened.Traits.Select(x => x.ToValue()).ToArray(), Benefit(), LoresheetPayment.CampaignXp));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    private sealed class ManualAutosave : IAutosaveScheduler
    {
        public void Schedule(Func<Task> action) { }
        public void Cancel() { }
        public void Dispose() { }
    }
}
