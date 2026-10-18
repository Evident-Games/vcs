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
    [ObservableProperty] private string tabName = key is "Identity" or "Attributes" ? "Identidade e atributos" : title;
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
            Sections[i].TabOrder = i + 1;
            Sections[i].PropertyChanged += OnSectionChanged;
        }
    }
    public SectionOption[] Sections { get; } =
    [
        new("Identity", "Identidade"), new("Attributes", "Atributos"), new("Skills", "Habilidades"),
        new("Disciplines", "Disciplinas"), new("Advantages", "Vantagens e desvantagens"),
        new("State", "Estado e sangue"), new("Background", "Historia e conviccoes"), new("Notes", "Anotacoes")
    ];
    [ObservableProperty] private bool useTabs;
    [ObservableProperty] private bool showSidebar = true;
    [ObservableProperty] private string themeId = ThemeCatalog.DefaultId;
    public ThemeOption[] Themes { get; } = ThemeCatalog.All.Select(x => new ThemeOption(x.Id, x.Name)).ToArray();
    public bool IsContinuous => !UseTabs;
    public event EventHandler? Changed;
    public event EventHandler? LayoutChanged;
    partial void OnUseTabsChanged(bool value)
    {
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
        if (e.PropertyName is nameof(SectionOption.IsVisible) or nameof(SectionOption.TabName) or
            nameof(SectionOption.Caption) or nameof(SectionOption.SectionOrder) or nameof(SectionOption.TabOrder))
        {
            LayoutChanged?.Invoke(this, EventArgs.Empty);
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}

public partial class ThemeOption(string id, string name) : ObservableObject
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    [ObservableProperty] private bool isSelected = id == ThemeCatalog.DefaultId;
    public void RefreshSelection() => OnPropertyChanged(nameof(IsSelected));
}

public sealed record EditorSection(string Key, string Title, CharacterEditorViewModel Editor);
public sealed record EditorTab(string Title, IReadOnlyList<EditorSection> Sections);
