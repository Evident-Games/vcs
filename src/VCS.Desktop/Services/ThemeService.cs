using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace VCS.Desktop.Services;

/// <summary>Each theme file has an application block (VcsApp*) and a sheet block (VcsSheet*).</summary>
public enum ThemePart { Application, Sheet }

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
        System.Windows.Application.Current.Resources.MergedDictionaries.OfType<ThemePalette>().Single().Fill(replacement);
        SetSelectionColors(System.Windows.Application.Current.Resources, replacement);
        var accent = ((SolidColorBrush)replacement["VcsAccent"]).Color;
        ApplicationAccentColorManager.Apply(accent, appearance, false, false);
        foreach (Window window in System.Windows.Application.Current.Windows)
        {
            window.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "VcsBackground");
            window.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "VcsOuterText");
        }
        _appliedTheme = themeId;
    }

    /// <summary>Loads one block of a theme file under the shared Vcs* keys the styles use.</summary>
    public static ResourceDictionary Load(ThemeDefinition theme, ThemePart part = ThemePart.Application)
    {
        var file = new ResourceDictionary { Source = new Uri(theme.ResourcePath, UriKind.Relative) };
        var prefix = part == ThemePart.Application ? "VcsApp" : "VcsSheet";
        var palette = new ResourceDictionary();
        foreach (var key in file.Keys.OfType<string>().Where(x => x.StartsWith(prefix, StringComparison.Ordinal)))
            palette["Vcs" + key[prefix.Length..]] = file[key];
        return palette;
    }

    /// <summary>Loads the sheet block for one sheet; system selection colors are scoped with it.</summary>
    public static ResourceDictionary LoadSheet(ThemeDefinition theme)
    {
        var palette = Load(theme, ThemePart.Sheet);
        SetSelectionColors(palette, palette);
        return palette;
    }

    private static void SetSelectionColors(ResourceDictionary target, ResourceDictionary palette)
    {
        target[SystemColors.HighlightBrushKey] = palette["VcsSelection"];
        target[SystemColors.InactiveSelectionHighlightBrushKey] = palette["VcsSelection"];
        target[SystemColors.HighlightTextBrushKey] = palette["VcsSelectionText"];
        target[SystemColors.InactiveSelectionHighlightTextBrushKey] = palette["VcsSelectionText"];
    }

    public static bool IsDark(Color background) =>
        0.299 * background.R + 0.587 * background.G + 0.114 * background.B < 128;
}