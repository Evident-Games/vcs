using VCS.Application.Catalogs;
using VCS.Content.Advantages;
using VCS.Content.Clans;
using VCS.Content.Disciplines;
using VCS.Content.Skills;
using VCS.Sheet;

namespace VCS.Desktop.Database;

/// <summary>Editar › Banco de dados: one tab per editable catalog.</summary>
public sealed class DatabaseEditorViewModel(
    IEditableCatalog<SkillDefinition> skills, IEditableCatalog<AdvantageDefinition> advantages,
    IEditableCatalog<PowerDefinition> powers, IEditableCatalog<ClanDefinition> clans, IEditableCatalog<DisciplineDefinition> disciplines)
{
    public IReadOnlyList<CatalogTabViewModel> Tabs { get; } =
    [
        new CatalogTab<SkillDefinition, SkillDraft>("Habilidades", skills, x => (x.Name, x.Group),
            x => new(x?.Value ?? new("", "", "Físicas", ""))),
        new CatalogTab<AdvantageDefinition, AdvantageDraft>("Vantagens e defeitos", advantages,
            x => (x.Name, $"{x.Group} · {CategoryLabel(x.Category)}"),
            x => new(x?.Value ?? new AdvantageDefinition { Category = TraitCategory.Advantage, Minimum = 1, Maximum = 1, Group = "Pessoais" })),
        new CatalogTab<DisciplineDefinition, DisciplineDraft>("Disciplinas", disciplines, x => (x.Name, ""),
            x => new(x?.Value ?? new("", ""), x?.IsBuiltIn == true)),
        new CatalogTab<PowerDefinition, PowerDraft>("Poderes e rituais", powers,
            x => (x.Name, $"{x.Discipline}{(x.IsRitual ? " · ritual" : "")} · nivel {x.Level}"),
            x => new(x?.Value ?? new("", DisciplineCatalog.All[0].Id, "", 1, "", "", ""))),
        new CatalogTab<ClanDefinition, ClanDraft>("Clas", clans, x => (x.Name, string.Join(" · ", x.Disciplines.Select(DisciplineCatalog.NameOf))),
            x => new(x?.Value ?? new("", "", "", [], []), x?.IsBuiltIn == true))
    ];

    public async Task LoadAsync()
    {
        foreach (var tab in Tabs) await tab.LoadAsync(tab.SelectedRow?.Id);
    }

    private static string CategoryLabel(TraitCategory category) => category switch
    {
        TraitCategory.Flaw => "defeito",
        TraitCategory.Loresheet => "loresheet",
        _ => "vantagem"
    };
}
