using System.Collections;
using System.Windows;

namespace VCS.Desktop.Themes;

/// <summary>Application palette: starts with the default theme's application block under the shared Vcs* keys.</summary>
public sealed class ThemePalette : ResourceDictionary
{
    public ThemePalette()
    {
        foreach (DictionaryEntry entry in ThemeService.Load(ThemeCatalog.Get(ThemeCatalog.DefaultId)))
            this[entry.Key] = entry.Value;
    }
}
