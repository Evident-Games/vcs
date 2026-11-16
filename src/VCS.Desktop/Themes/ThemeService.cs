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
        AddHudPalette(palette);
        return palette;
    }

    private static void AddHudPalette(ResourceDictionary palette)
    {
        var accent = ((SolidColorBrush)palette["VcsAccent"]).Color;
        var background = ((SolidColorBrush)palette["VcsBackground"]).Color;
        static Color Shade(Color color, double amount) => Color.FromRgb((byte)(color.R * amount), (byte)(color.G * amount), (byte)(color.B * amount));
        void Default(string key, object value) { if (!palette.Contains(key)) palette[key] = value; }
        // Optional VcsAppHud* overrides are loaded above; older custom themes need no new keys.
        var surface = IsDark(background) ? Color.FromArgb(246, background.R, background.G, background.B) : Color.FromArgb(246, 28, 25, 26);
        Default("VcsHudSurface", new SolidColorBrush(surface));
        Default("VcsHudDock", new SolidColorBrush(Color.FromArgb(235, 24, 21, 22)));
        Default("VcsHudCard", new SolidColorBrush(Color.FromRgb(35, 31, 33)));
        Default("VcsHudText", new SolidColorBrush(Color.FromRgb(242, 242, 242)));
        Default("VcsHudMuted", new SolidColorBrush(Color.FromRgb(158, 155, 155)));
        Default("VcsHudBorder", new SolidColorBrush(Color.FromArgb(70, 200, 190, 194)));
        Default("VcsHudAccent", new SolidColorBrush(accent));
        Default("VcsHudOpen", new SolidColorBrush(Color.FromRgb(108, 179, 135)));
        Default("VcsHudClosed", new SolidColorBrush(Color.FromRgb(100, 96, 99)));
        var hudAccent = ((SolidColorBrush)palette["VcsHudAccent"]).Color;
        Default("VcsHudAccentColor", hudAccent);
        Default("VcsHudSection", new LinearGradientBrush(new GradientStopCollection
        {
            new(Shade(hudAccent, 0.12), 0), new(Shade(hudAccent, 0.4), 0.5), new(Shade(hudAccent, 0.12), 1)
        }, new Point(0, 0.5), new Point(1, 0.5)));
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
