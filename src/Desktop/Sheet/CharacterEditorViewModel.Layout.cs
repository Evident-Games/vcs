using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using VCS.Desktop.Layout;

namespace VCS.Desktop.Sheet;

/// <summary>Sections, tabs and sheet theme of this sheet, following the shared PC or NPC layout.</summary>
public partial class CharacterEditorViewModel
{
    /// <summary>Theme used inside this sheet; null follows the application theme.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EffectiveSheetThemeId))]
    private string? sheetThemeId;
    public string EffectiveSheetThemeId => SheetThemeId ?? Display.ThemeId;

    public DisplaySettingsViewModel Display { get; private set; } = new();
    public ObservableCollection<EditorSection> VisibleSections { get; } = [];
    public ObservableCollection<EditorTab> VisibleTabs { get; } = [];
    [ObservableProperty] private EditorTab? selectedTab;
    public bool NoVisibleSections => VisibleSections.Count == 0;
    /// <summary>Section arrangement for this sheet's kind (PC or NPC).</summary>
    public SheetLayout Layout => Display.LayoutFor(IsNpc);
    public void ConfigureDisplay(DisplaySettingsViewModel display)
    {
        Layout.Changed -= OnLayoutChanged;
        Display.PropertyChanged -= OnDisplayPropertyChanged;
        Display = display;
        Layout.Changed += OnLayoutChanged;
        Display.PropertyChanged += OnDisplayPropertyChanged;
        OnPropertyChanged(nameof(Display));
        OnPropertyChanged(nameof(Layout));
        OnPropertyChanged(nameof(EffectiveSheetThemeId));
        RefreshSections();
    }
    private void OnLayoutChanged(object? sender, EventArgs e) => RefreshSections();
    private void OnDisplayPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DisplaySettingsViewModel.ThemeId)) OnPropertyChanged(nameof(EffectiveSheetThemeId));
    }
    /// <summary>
    /// Syncs the shown sections with the layout, reusing existing section objects so their views are not rebuilt.
    /// Titles and bento sizes are bound live to the options, so only order, visibility and tabs need work here.
    /// </summary>
    private void RefreshSections()
    {
        var wanted = Layout.Sections.Where(x => x.IsVisible).OrderBy(x => x.SectionOrder).ToArray();
        var existing = VisibleSections.ToDictionary(x => x.Option);
        for (var i = VisibleSections.Count - 1; i >= 0; i--)
            if (!wanted.Contains(VisibleSections[i].Option)) VisibleSections.RemoveAt(i);
        for (var i = 0; i < wanted.Length; i++)
        {
            if (!existing.TryGetValue(wanted[i], out var section)) VisibleSections.Insert(i, new(wanted[i], this));
            else if (VisibleSections.IndexOf(section) is var from && from != i) VisibleSections.Move(from, i);
        }
        var tabs = VisibleSections.GroupBy(x => SheetLayout.TabKey(x.Option), StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Min(x => x.Option.TabOrder)).Select(g => new EditorTab(g.Key, g.ToArray())).ToArray();
        var sameTabs = tabs.Length == VisibleTabs.Count && tabs.Zip(VisibleTabs).All(x =>
            x.First.Title == x.Second.Title && x.First.Sections.SequenceEqual(x.Second.Sections));
        if (!sameTabs)
        {
            var tabKey = SelectedTab?.Sections.FirstOrDefault()?.Key;
            VisibleTabs.Clear();
            foreach (var tab in tabs) VisibleTabs.Add(tab);
            SelectedTab = VisibleTabs.FirstOrDefault(x => x.Sections.Any(s => s.Key == tabKey)) ?? VisibleTabs.FirstOrDefault();
        }
        OnPropertyChanged(nameof(NoVisibleSections));
    }
}
