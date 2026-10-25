using CommunityToolkit.Mvvm.ComponentModel;

namespace VCS.Desktop.Themes;

/// <summary>A theme entry in a theme menu.</summary>
public partial class ThemeOption(string id, string name, ThemeGroup group) : ObservableObject
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public ThemeGroup Group { get; } = group;
    [ObservableProperty] private bool isSelected = id == ThemeCatalog.DefaultId;
    public void RefreshSelection() => OnPropertyChanged(nameof(IsSelected));
}
