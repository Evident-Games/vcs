using VCS.Application.Catalogs;
using VCS.Domain.Advantages;
using VCS.Domain.Characters;
using VCS.Domain.Clans;
using VCS.Domain.Skills;

namespace VCS.Desktop.Database;

/// <summary>Editar › Banco de dados: one tab per editable catalog.</summary>
public sealed class DatabaseEditorViewModel(
    IEditableCatalog<SkillDefinition> skills, IEditableCatalog<AdvantageDefinition> advantages,
    IEditableCatalog<PowerDefinition> powers, IEditableCatalog<ClanDefinition> clans)
{
    public IReadOnlyList<CatalogTabViewModel> Tabs { get; } =
    [
        new CatalogTab<SkillDefinition, SkillDraft>("Habilidades", skills, x => (x.Name, x.Group),
            x => new(x?.Value ?? new("", "", "Físicas", ""))),
        new CatalogTab<AdvantageDefinition, AdvantageDraft>("Vantagens e defeitos", advantages,
            x => (x.Name, $"{x.Group} · {CategoryLabel(x.Category)}"),
            x => new(x?.Value ?? new AdvantageDefinition { Category = TraitCategory.Advantage, Minimum = 1, Maximum = 1, Group = "Pessoais" })),
        new CatalogTab<PowerDefinition, PowerDraft>("Disciplinas e poderes", powers, x => (x.Name, $"{x.Discipline} · nivel {x.Level}"),
            x => new(x?.Value ?? new("", "", "", 1, "", "", ""))),
        new CatalogTab<ClanDefinition, ClanDraft>("Clas", clans, x => (x.Name, string.Join(" · ", x.Disciplines)),
            x => new(x?.Value ?? new("", "", [], []), x?.IsBuiltIn == true))
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
