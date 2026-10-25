using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VCS.Desktop.Layout;

/// <summary>One section of a sheet layout: visibility, order, tab and bento size.</summary>
public partial class SectionOption(string key, string title, string defaultTab, int defaultSpan = 1, int defaultHeight = 1)
    : ObservableObject
{
    public string Key { get; } = key;
    public string Title { get; } = title;
    [ObservableProperty] private bool isVisible = true;
    [ObservableProperty] private string tabName = defaultTab;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    private string caption = title;
    [ObservableProperty] private int sectionOrder;
    [ObservableProperty] private int tabOrder;
    /// <summary>Bento width in columns, 1 to 4.</summary>
    [ObservableProperty] private int span = Math.Clamp(defaultSpan, 1, SheetLayout.BentoColumns);
    /// <summary>Bento height: 1 small, 2 medium, 3 large.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowWeight), nameof(HeightLabel))]
    private int height = Math.Clamp(defaultHeight, 1, 3);

    public string DisplayTitle => string.IsNullOrWhiteSpace(Caption) ? Title : Caption.Trim();
    public double RowWeight => Height switch { 2 => 1.6, 3 => 2.2, _ => 1 };
    public string HeightLabel => Height switch { 2 => "M", 3 => "G", _ => "P" };

    [RelayCommand] private void CycleSpan() => Span = Span % SheetLayout.BentoColumns + 1;
    [RelayCommand] private void CycleHeight() => Height = Height % 3 + 1;
    [RelayCommand] private void ToggleVisible() => IsVisible = !IsVisible;
}
