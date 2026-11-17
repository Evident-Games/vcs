using CommunityToolkit.Mvvm.ComponentModel;
using VCS.Content.Clans;
using VCS.Content.Disciplines;

namespace VCS.Desktop.Database;

/// <summary>Clan form; disciplines are ticked in the list of disciplines in use, aliases are comma separated text.</summary>
public sealed partial class ClanDraft(ClanDefinition clan, bool isBuiltIn) : ObservableObject, ICatalogDraft<ClanDefinition>
{
    public bool CanRename => !isBuiltIn;
    [ObservableProperty] private string name = clan.Name;
    [ObservableProperty] private string description = clan.Description;
    [ObservableProperty] private string aliases = string.Join(", ", clan.Aliases);
    [ObservableProperty] private string bane = clan.Bane;

    public IReadOnlyList<DisciplineChoice> Disciplines { get; } =
        [.. DisciplineCatalog.All.Select(x => new DisciplineChoice(x) { IsSelected = clan.Disciplines.Contains(x.Id) })];

    public ClanDefinition ToValue() => new(clan.Id, Name.Trim(), Description.Trim(),
        [.. Disciplines.Where(x => x.IsSelected).Select(x => x.Definition.Id)], Split(Aliases), Bane.Trim());

    private static string[] Split(string text) =>
        text.Split([',', ';', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}

public sealed partial class DisciplineChoice(DisciplineDefinition definition) : ObservableObject
{
    public DisciplineDefinition Definition { get; } = definition;
    [ObservableProperty] private bool isSelected;
}
