using CommunityToolkit.Mvvm.ComponentModel;
using VCS.Domain.Clans;

namespace VCS.Desktop.Database;

/// <summary>Clan form; lists are edited as comma separated text. A built-in clan keeps its name.</summary>
public sealed partial class ClanDraft(ClanDefinition clan, bool isBuiltIn) : ObservableObject, ICatalogDraft<ClanDefinition>
{
    public bool CanRename => !isBuiltIn;
    [ObservableProperty] private string name = clan.Name;
    [ObservableProperty] private string description = clan.Description;
    [ObservableProperty] private string disciplines = string.Join(", ", clan.Disciplines);
    [ObservableProperty] private string aliases = string.Join(", ", clan.Aliases);
    [ObservableProperty] private string bane = clan.Bane;

    public ClanDefinition ToValue() => new(Name.Trim(), Description.Trim(), Split(Disciplines), Split(Aliases), Bane.Trim());

    private static string[] Split(string text) =>
        text.Split([',', ';', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
