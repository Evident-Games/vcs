using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Desktop.Sheet;
using VCS.Desktop.Themes;

namespace VCS.Desktop.Layout;

public partial class DisplaySettingsViewModel : ObservableObject
{
    public DisplaySettingsViewModel()
    {
        Pc.Changed += OnLayoutChanged;
        Npc.Changed += OnLayoutChanged;
    }

    public SheetLayout Pc { get; } = SheetLayout.CreatePc();
    public SheetLayout Npc { get; } = SheetLayout.CreateNpc();
    public SheetLayout LayoutFor(bool npc) => npc ? Npc : Pc;
    /// <summary>PC sections, the layout most callers mean.</summary>
    public SectionOption[] Sections => Pc.Sections;
    public System.Collections.ObjectModel.ObservableCollection<SectionOption> OrderedSections => Pc.OrderedSections;
    public void MoveSection(SectionOption section, int index) =>
        (Npc.Sections.Contains(section) ? Npc : Pc).MoveSection(section, index);

    /// <summary>Layout shown in the layout editor; opens on the active sheet's kind.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditedLayout), nameof(IsEditingPc))]
    private bool isEditingNpc;
    public bool IsEditingPc
    {
        get => !IsEditingNpc;
        set { if (value) IsEditingNpc = false; }
    }
    public SheetLayout EditedLayout => LayoutFor(IsEditingNpc);

    private void OnLayoutChanged(object? sender, EventArgs e)
    {
        LayoutChanged?.Invoke(this, EventArgs.Empty);
        Changed?.Invoke(this, EventArgs.Empty);
    }
    [ObservableProperty] private bool useTabs;
    [ObservableProperty] private bool useBento;
    [ObservableProperty] private bool showSidebar = true;
    [ObservableProperty] private string themeId = ThemeCatalog.DefaultId;
    public ThemeOption[] Themes { get; } = ThemeCatalog.All.Select(x => new ThemeOption(x.Id, x.Name, x.Group)).ToArray();
    public IEnumerable<ThemeOption> DefaultThemes => Themes.Where(x => x.Group == ThemeGroup.Default);
    public IEnumerable<ThemeOption> ClanThemes => Themes.Where(x => x.Group == ThemeGroup.Clan);
    public IEnumerable<ThemeOption> SpecialThemes => Themes.Where(x => x.Group == ThemeGroup.Special);

    // Sheet themes: each character may override the application theme inside its own sheet.
    private Guid? _activeSheet;
    public Dictionary<Guid, string> SheetThemeIds { get; } = [];
    public ThemeOption[] SheetThemes { get; } =
        ThemeCatalog.All.Select(x => new ThemeOption(x.Id, x.Name, x.Group) { IsSelected = false }).ToArray();
    public IEnumerable<ThemeOption> DefaultSheetThemes => SheetThemes.Where(x => x.Group == ThemeGroup.Default);
    public IEnumerable<ThemeOption> ClanSheetThemes => SheetThemes.Where(x => x.Group == ThemeGroup.Clan);
    public IEnumerable<ThemeOption> SpecialSheetThemes => SheetThemes.Where(x => x.Group == ThemeGroup.Special);
    public bool SheetFollowsApp => _activeSheet is not { } id || !SheetThemeIds.ContainsKey(id);
    public string? SheetThemeFor(Guid id) => SheetThemeIds.GetValueOrDefault(id);

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

    public bool IsContinuous
    {
        get => !UseTabs && !UseBento;
        set
        {
            if (value) { UseTabs = false; UseBento = false; }
            OnPropertyChanged();
        }
    }
    public event EventHandler? Changed;
    public event EventHandler? LayoutChanged;
    partial void OnUseTabsChanged(bool value)
    {
        if (value) UseBento = false;
        OnPropertyChanged(nameof(IsContinuous));
        Changed?.Invoke(this, EventArgs.Empty);
    }
    partial void OnUseBentoChanged(bool value)
    {
        if (value) UseTabs = false;
        OnPropertyChanged(nameof(IsContinuous));
        Changed?.Invoke(this, EventArgs.Empty);
    }
    partial void OnThemeIdChanging(string value) => ThemeCatalog.Get(value);
    partial void OnShowSidebarChanged(bool value) => Changed?.Invoke(this, EventArgs.Empty);
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
}

public partial class ThemeOption(string id, string name, ThemeGroup group) : ObservableObject
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public ThemeGroup Group { get; } = group;
    [ObservableProperty] private bool isSelected = id == ThemeCatalog.DefaultId;
    public void RefreshSelection() => OnPropertyChanged(nameof(IsSelected));
}

/// <summary>A section shown in one sheet; the option carries its live title, tab and bento size.</summary>
public sealed record EditorSection(SectionOption Option, CharacterEditorViewModel Editor)
{
    public string Key => Option.Key;
    public string Title => Option.DisplayTitle;
}
public sealed record EditorTab(string Title, IReadOnlyList<EditorSection> Sections);
