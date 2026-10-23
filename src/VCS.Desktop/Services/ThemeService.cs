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
        appResources[SystemColors.HighlightTextBrushKey] = replacement["VcsSelectionText"];
        appResources[SystemColors.InactiveSelectionHighlightTextBrushKey] = replacement["VcsSelectionText"];
        var accent = ((SolidColorBrush)replacement["VcsAccent"]).Color;
        ApplicationAccentColorManager.Apply(accent, appearance, false, false);
        foreach (Window window in System.Windows.Application.Current.Windows)
        {
            window.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "VcsBackground");
            window.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "VcsHeaderText");
        }
        _appliedTheme = themeId;
    }

    /// <summary>Loads a palette; themes without VcsOuterText reuse VcsText for text on the window background.</summary>
    public static ResourceDictionary Load(ThemeDefinition theme)
    {
        var palette = new ResourceDictionary { Source = new Uri(theme.ResourcePath, UriKind.Relative) };
        // New semantic color roles fall back to the legacy palette keys. This keeps saved themes
        // working while letting each surface be customized independently going forward.
        SetFallback(palette, "VcsOuterText", "VcsText");
        SetFallback(palette, "VcsHeaderText", "VcsOuterText");
        SetFallback(palette, "VcsFooterText", "VcsOuterText");
        SetFallback(palette, "VcsCardText", "VcsText");
        SetFallback(palette, "VcsSelectionText", "VcsText");
        SetFallback(palette, "VcsInputText", "VcsText");
        SetFallback(palette, "VcsInputTitle", "VcsText");
        SetFallback(palette, "VcsInputDescription", "VcsMuted");
        SetFallback(palette, "VcsAccentText", "VcsAccent");
        SetFallback(palette, "VcsAccentHotbar", "VcsAccent");
        SetFallback(palette, "VcsAccentBorder", "VcsAccent");
        SetFallback(palette, "VcsScroll", "VcsMuted");
        return palette;
    }

    private static void SetFallback(ResourceDictionary palette, string key, string legacyKey)
    {
        if (!palette.Contains(key)) palette[key] = palette[legacyKey];
    }

    public static bool IsDark(Color background) =>
        0.299 * background.R + 0.587 * background.G + 0.114 * background.B < 128;
}