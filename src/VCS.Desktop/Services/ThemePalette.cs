using System.Collections;
using System.Windows;

namespace VCS.Desktop.Services;

/// <summary>Application palette: the application block of a theme under the shared Vcs* keys.</summary>
public sealed class ThemePalette : ResourceDictionary
{
    public ThemePalette() => Fill(ThemeCatalog.DefaultId);

    public void Fill(string themeId) => Fill(ThemeService.Load(ThemeCatalog.Get(themeId)));

    public void Fill(ResourceDictionary palette)
    {
        foreach (DictionaryEntry entry in palette) this[entry.Key] = entry.Value;
    }
}
