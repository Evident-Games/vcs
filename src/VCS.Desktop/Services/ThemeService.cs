using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace VCS.Desktop.Services;

public sealed class ThemeService
{
    private string? _appliedTheme;
    public void Apply(string themeId)
    {
        var theme = ThemeCatalog.Get(themeId);
        if (_appliedTheme == themeId) return;
        _appliedTheme = null;
        var replacement = Load(theme);
        var appearance = IsDark(((SolidColorBrush)replacement["VcsBackground"]).Color) ? ApplicationTheme.Dark : ApplicationTheme.Light;
        ApplicationThemeManager.Apply(appearance);
        foreach (var dictionary in System.Windows.Application.Current.Resources.MergedDictionaries.OfType<Wpf.Ui.Markup.ThemesDictionary>())
            dictionary.Theme = appearance;
        var dictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;
        var palette = dictionaries.Single(x => x.Keys.Cast<object>().Contains("VcsBackground"));
        foreach (var key in replacement.Keys)
            palette[key] = replacement[key];
        var appResources = System.Windows.Application.Current.Resources;
        appResources[SystemColors.HighlightBrushKey] = replacement["VcsSelection"];
        appResources[SystemColors.InactiveSelectionHighlightBrushKey] = replacement["VcsSelection"];
        appResources[SystemColors.HighlightTextBrushKey] = replacement["VcsText"];
        appResources[SystemColors.InactiveSelectionHighlightTextBrushKey] = replacement["VcsText"];
        var accent = ((SolidColorBrush)replacement["VcsAccent"]).Color;
        ApplicationAccentColorManager.Apply(accent, appearance, false, false);
        foreach (Window window in System.Windows.Application.Current.Windows)
        {
            window.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "VcsBackground");
            window.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "VcsOuterText");
        }
        _appliedTheme = themeId;
    }

    /// <summary>Loads a palette; themes without VcsOuterText reuse VcsText for text on the window background.</summary>
    public static ResourceDictionary Load(ThemeDefinition theme)
    {
        var palette = new ResourceDictionary { Source = new Uri(theme.ResourcePath, UriKind.Relative) };
        if (!palette.Contains("VcsOuterText")) palette["VcsOuterText"] = palette["VcsText"];
        return palette;
    }

    public static bool IsDark(Color background) =>
        0.299 * background.R + 0.587 * background.G + 0.114 * background.B < 128;
}