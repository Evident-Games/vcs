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
        var appearance = theme.IsDark ? ApplicationTheme.Dark : ApplicationTheme.Light;
        ApplicationThemeManager.Apply(appearance);
        foreach (var dictionary in System.Windows.Application.Current.Resources.MergedDictionaries.OfType<Wpf.Ui.Markup.ThemesDictionary>())
            dictionary.Theme = appearance;
        var dictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;
        var palette = dictionaries.Single(x => x.Keys.Cast<object>().Contains("VcsBackground"));
        var replacement = new System.Windows.ResourceDictionary
        {
            Source = new Uri($"/VCS.Desktop;component/Themes/{theme.ResourceName}.xaml", UriKind.Relative)
        };
        foreach (var key in replacement.Keys)
            palette[key] = replacement[key];
        var accent = ((System.Windows.Media.SolidColorBrush)replacement["VcsAccent"]).Color;
        ApplicationAccentColorManager.Apply(accent, appearance, false, false);
        foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
        {
            window.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "VcsBackground");
            window.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "VcsText");
        }
        _appliedTheme = themeId;
    }
}
