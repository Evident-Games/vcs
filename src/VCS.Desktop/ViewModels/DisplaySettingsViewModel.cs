using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using VCS.Desktop.Services;

namespace VCS.Desktop.ViewModels;

public partial class SectionOption(string key, string title) : ObservableObject
{
    public string Key { get; } = key;
    public string Title { get; } = title;
    [ObservableProperty] private bool isVisible = true;
    [ObservableProperty] private string tabName = key switch
    {
        "Identity" => "Identidade",
        "Attributes" => "Atributos",
        "Skills" => "Habilidades",
        "Disciplines" => "Disciplinas",
        "Advantages" => "Vantagens",
        "State" => "Estado",
        "Background" or "Notes" => "História",
        _ => title
    };
    [ObservableProperty] private string caption = title;
    [ObservableProperty] private int sectionOrder;
    [ObservableProperty] private int tabOrder;
}

public partial class DisplaySettingsViewModel : ObservableObject
{
    public DisplaySettingsViewModel()
    {
        for (var i = 0; i < Sections.Length; i++)
        {
            Sections[i].SectionOrder = i + 1;
            Sections[i].TabOrder = Array.FindIndex(Sections, section => section.TabName == Sections[i].TabName) + 1;
            Sections[i].PropertyChanged += OnSectionChanged;
        }
        OrderedSections = new(Sections);
    }
    /// <summary>Sections in display order; the organizer list binds here so drag and drop can reorder it.</summary>
    public System.Collections.ObjectModel.ObservableCollection<SectionOption> OrderedSections { get; }
    private bool _reordering;

    /// <summary>Moves a section to a new position; tab order follows the first section of each tab group.</summary>
    public void MoveSection(SectionOption section, int index)
    {
        var current = OrderedSections.IndexOf(section);
        index = Math.Clamp(index, 0, OrderedSections.Count - 1);
        if (current < 0 || current == index) return;
        OrderedSections.Move(current, index);
        _reordering = true;
        try
        {
            var tabs = OrderedSections.Select(TabKey).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            for (var i = 0; i < OrderedSections.Count; i++)
            {
                OrderedSections[i].SectionOrder = i + 1;
                OrderedSections[i].TabOrder = tabs.FindIndex(x =>
                    string.Equals(x, TabKey(OrderedSections[i]), StringComparison.OrdinalIgnoreCase)) + 1;
            }
        }
        finally { _reordering = false; }
        LayoutChanged?.Invoke(this, EventArgs.Empty);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static string TabKey(SectionOption section)
    {
        var name = section.TabName.Trim();
        return name.Length > 0 ? name : string.IsNullOrWhiteSpace(section.Caption) ? section.Title : section.Caption.Trim();
    }

    private void SyncOrderedSections()
    {
        var sorted = Sections.OrderBy(x => x.SectionOrder).ThenBy(x => Array.IndexOf(Sections, x)).ToArray();
        for (var i = 0; i < sorted.Length; i++)
        {
            var from = OrderedSections.IndexOf(sorted[i]);
            if (from != i) OrderedSections.Move(from, i);
        }
    }
    public SectionOption[] Sections { get; } =
    [
        new("Identity", "Identidade"), new("Attributes", "Atributos"), new("Skills", "Habilidades"),
        new("Disciplines", "Disciplinas"), new("Advantages", "Vantagens e desvantagens"),
        new("State", "Estado e sangue"), new("Background", "Historia e conviccoes"), new("Notes", "Anotacoes")
    ];
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
        LayoutChanged?.Invoke(this, EventArgs.Empty);
        Changed?.Invoke(this, EventArgs.Empty);
    }
    partial void OnUseBentoChanged(bool value)
    {
        if (value) UseTabs = false;
        OnPropertyChanged(nameof(IsContinuous));
        LayoutChanged?.Invoke(this, EventArgs.Empty);
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
    private void OnSectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_reordering) return;
        if (e.PropertyName == nameof(SectionOption.SectionOrder)) SyncOrderedSections();
        if (e.PropertyName is nameof(SectionOption.IsVisible) or nameof(SectionOption.TabName) or
            nameof(SectionOption.Caption) or nameof(SectionOption.SectionOrder) or nameof(SectionOption.TabOrder))
        {
            LayoutChanged?.Invoke(this, EventArgs.Empty);
            Changed?.Invoke(this, EventArgs.Empty);
        }
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

public sealed record EditorSection(string Key, string Title, CharacterEditorViewModel Editor);
public sealed record EditorTab(string Title, IReadOnlyList<EditorSection> Sections);
