using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace VCS.Desktop.Themes;

/// <summary>Each theme file has an application block (VcsApp*) and a sheet block (VcsSheet*).</summary>
public enum ThemePart { Application, Sheet }

public sealed class ThemeService
{
    private string? _appliedTheme;
    private ApplicationTheme? _appearance;
    private Color? _accent;
    public void Apply(string themeId)
    {
        var theme = ThemeCatalog.Get(themeId);
        if (_appliedTheme == themeId) return;
        _appliedTheme = null;
        var replacement = Load(theme);
        var appearance = IsDark(((SolidColorBrush)replacement["VcsBackground"]).Color) ? ApplicationTheme.Dark : ApplicationTheme.Light;
        // Wpf.Ui reloads all of its dictionaries on apply, so only do it when light/dark actually changes.
        if (_appearance != appearance)
        {
            ApplicationThemeManager.Apply(appearance);
            foreach (var dictionary in System.Windows.Application.Current.Resources.MergedDictionaries.OfType<Wpf.Ui.Markup.ThemesDictionary>())
                dictionary.Theme = appearance;
            _appearance = appearance;
            _accent = null;
        }
        // Update keys in place: Wpf.Ui's TitleBar ignores a swapped dictionary.
        SetSelectionColors(replacement);
        var palette = System.Windows.Application.Current.Resources.MergedDictionaries.OfType<ThemePalette>().Single();
        foreach (var key in replacement.Keys) palette[key] = replacement[key];
        var accent = ((SolidColorBrush)replacement["VcsAccent"]).Color;
        if (_accent != accent) ApplicationAccentColorManager.Apply(accent, appearance, false, false);
        _accent = accent;
        foreach (Window window in System.Windows.Application.Current.Windows)
        {
            window.SetResourceReference(Control.BackgroundProperty, "VcsBackground");
            window.SetResourceReference(Control.ForegroundProperty, "VcsOuterText");
        }
        _appliedTheme = themeId;
    }

    /// <summary>
    /// Loads one block of a theme file under the shared Vcs* keys the styles use. The application block may
    /// list only what differs: missing colors come from the sheet block, header and footer text from outer text.
    /// </summary>
    public static ResourceDictionary Load(ThemeDefinition theme, ThemePart part = ThemePart.Application) =>
        Load(new ResourceDictionary { Source = new Uri(theme.ResourcePath, UriKind.Relative) }, part);

    public static ResourceDictionary Load(ResourceDictionary file, ThemePart part)
    {
        var palette = new ResourceDictionary();
        void Copy(string prefix)
        {
            foreach (var key in file.Keys.OfType<string>().Where(x => x.StartsWith(prefix, StringComparison.Ordinal)))
                palette["Vcs" + key[prefix.Length..]] = file[key];
        }
        Copy("VcsSheet");
        if (part == ThemePart.Sheet) return palette;
        Copy("VcsApp");
        foreach (var key in new[] { "VcsHeaderText", "VcsFooterText" })
            if (!palette.Contains(key)) palette[key] = palette["VcsOuterText"];
        return palette;
    }

    /// <summary>Loads the sheet block for one sheet; system selection colors are scoped with it.</summary>
    public static ResourceDictionary LoadSheet(ThemeDefinition theme)
    {
        var palette = Load(theme, ThemePart.Sheet);
        SetSelectionColors(palette);
        return palette;
    }

    private static void SetSelectionColors(ResourceDictionary palette)
    {
        palette[SystemColors.HighlightBrushKey] = palette["VcsSelection"];
        palette[SystemColors.InactiveSelectionHighlightBrushKey] = palette["VcsSelection"];
        palette[SystemColors.HighlightTextBrushKey] = palette["VcsSelectionText"];
        palette[SystemColors.InactiveSelectionHighlightTextBrushKey] = palette["VcsSelectionText"];
    }

    public static bool IsDark(Color background) =>
        0.299 * background.R + 0.587 * background.G + 0.114 * background.B < 128;
}