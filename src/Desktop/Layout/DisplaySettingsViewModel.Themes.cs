using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Desktop.Themes;

namespace VCS.Desktop.Layout;

/// <summary>Application theme and per-sheet theme overrides.</summary>
public partial class DisplaySettingsViewModel
{
    [ObservableProperty] private string themeId = ThemeCatalog.DefaultId;
    public ThemeOption[] Themes { get; } = ThemeCatalog.All.Select(x => new ThemeOption(x.Id, x.Name, x.Group)).ToArray();
    public IEnumerable<ThemeOption> DefaultThemes => Themes.Where(x => x.Group == ThemeGroup.Default);
    public IEnumerable<ThemeOption> ClanThemes => Themes.Where(x => x.Group == ThemeGroup.Clan);
    public IEnumerable<ThemeOption> SpecialThemes => Themes.Where(x => x.Group == ThemeGroup.Special);

    partial void OnThemeIdChanging(string value) => ThemeCatalog.Get(value);

    partial void OnThemeIdChanged(string value)
    {
        foreach (var theme in Themes) theme.IsSelected = theme.Id == value;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void SelectTheme(string id)
    {
        ThemeId = id;
        foreach (var theme in Themes) theme.RefreshSelection();
    }

    // Each character may override the application theme inside its own sheet.
    private Guid? _activeSheet;
    public Dictionary<Guid, string> SheetThemeIds { get; } = [];
    public ThemeOption[] SheetThemes { get; } =
        ThemeCatalog.All.Select(x => new ThemeOption(x.Id, x.Name, x.Group) { IsSelected = false }).ToArray();
    public IEnumerable<ThemeOption> DefaultSheetThemes => SheetThemes.Where(x => x.Group == ThemeGroup.Default);
    public IEnumerable<ThemeOption> ClanSheetThemes => SheetThemes.Where(x => x.Group == ThemeGroup.Clan);
    public IEnumerable<ThemeOption> SpecialSheetThemes => SheetThemes.Where(x => x.Group == ThemeGroup.Special);
    public bool SheetFollowsApp => _activeSheet is not { } id || !SheetThemeIds.ContainsKey(id);
    public string? SheetThemeFor(Guid id) => SheetThemeIds.GetValueOrDefault(id);

    /// <summary>Marks which sheet the "Tema da ficha" menu refers to.</summary>
    public void ShowSheet(Guid? id)
    {
        _activeSheet = id;
        RefreshSheetSelection();
    }

    public void SetSheetTheme(Guid id, string? themeId)
    {
        if (themeId is null) SheetThemeIds.Remove(id);
        else SheetThemeIds[id] = ThemeCatalog.Get(themeId).Id;
        RefreshSheetSelection();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshSheetSelection()
    {
        var current = _activeSheet is { } id ? SheetThemeFor(id) : null;
        foreach (var theme in SheetThemes)
        {
            theme.IsSelected = theme.Id == current;
            theme.RefreshSelection();
        }
        OnPropertyChanged(nameof(SheetFollowsApp));
    }
}
