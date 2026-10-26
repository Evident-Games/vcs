using VCS.Content;
using VCS.Content.Advantages;
using VCS.Content.Clans;
using VCS.Content.Disciplines;
using VCS.Content.Predators;
using VCS.Content.Skills;
using VCS.Sheet;

namespace VCS.Tests;

/// <summary>The content JSON files that ship with the app: complete, with unique prefixed ids and references that exist.</summary>
public sealed class ContentFilesTests
{
    private static readonly string[] DisciplineNames = [.. DisciplineCatalog.BuiltIn.Select(x => x.Name)];

    private static bool IsDiscipline(string name) => DisciplineNames.Any(x => ClanCatalog.SameName(x, name));

    [Fact]
    public void Ids_are_unique_and_start_with_their_kind()
    {
        (string Id, string Kind)[] ids =
        [
            .. SkillCatalog.BuiltIn.Select(x => (x.Id, "skill-")),
            .. AdvantageCatalog.BuiltIn.Select(x => (x.Id, x.Category == TraitCategory.Flaw ? "flaw-" : "advantage-")),
            .. AdvantageCatalog.Loresheets.Select(x => (x.Id, "loresheet-")),
            .. PredatorCatalog.All.Select(x => (x.Id, "predator-")),
            .. ClanCatalog.BuiltIn.Select(x => (x.Id, "clan-")),
            .. DisciplineCatalog.BuiltIn.Select(x => (x.Id, "discipline-")),
            .. DisciplineCatalog.Powers.Select(x => (x.Id, x.IsRitual ? "ritual-" : "power-"))
        ];
        Assert.All(ids, x => Assert.Matches("^" + x.Kind + "[a-z0-9]+(-[a-z0-9]+)*$", x.Id));
        Assert.Equal(ids.Length, ids.Select(x => x.Id).Distinct().Count());
    }

    [Fact]
    public void Entries_are_complete()
    {
        Assert.All(SkillCatalog.BuiltIn, x => Assert.False(string.IsNullOrWhiteSpace(x.Name + x.Group + x.Description) || x.Name.Length == 0));
        Assert.All(AdvantageCatalog.BuiltIn.Concat(AdvantageCatalog.Loresheets), x =>
        {
            Assert.NotEmpty(x.Name);
            Assert.InRange(x.Minimum, 1, 5);
            Assert.InRange(x.Maximum, x.Minimum, 5);
        });
        Assert.All(DisciplineCatalog.Powers, x =>
        {
            Assert.InRange(x.Level, 1, 5);
            Assert.InRange(x.Name.Length, 1, 80);
            Assert.False(string.IsNullOrWhiteSpace(x.Cost) || string.IsNullOrWhiteSpace(x.DicePool) || string.IsNullOrWhiteSpace(x.Summary));
        });
        Assert.All(ClanCatalog.BuiltIn, x => Assert.NotEmpty(x.Bane));
    }

    [Fact]
    public void References_point_to_existing_content()
    {
        Assert.All(ClanCatalog.BuiltIn.SelectMany(x => x.Disciplines), x => Assert.True(IsDiscipline(x), x));
        Assert.All(DisciplineCatalog.Powers, x =>
        {
            Assert.True(x.IsRitual || IsDiscipline(x.Discipline), x.Discipline);
            if (x.AmalgamDiscipline is { } amalgam) Assert.True(IsDiscipline(amalgam), amalgam);
        });
        Assert.All(PredatorCatalog.All.SelectMany(x => x.Disciplines), x => Assert.True(IsDiscipline(x), x));
        Assert.All(PredatorCatalog.All.SelectMany(x => x.Specializations),
            x => Assert.Contains(SkillCatalog.BuiltIn, skill => ClanCatalog.SameName(skill.Name, x.Skill)));
    }

    [Fact]
    public void Only_the_loader_reads_the_files()
    {
        Assert.Throws<InvalidDataException>(() => BuiltInContent.Load<SkillDefinition>("missing.json"));
    }
}
