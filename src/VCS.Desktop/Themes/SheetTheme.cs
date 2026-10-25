using System.Windows;

namespace VCS.Desktop.Themes;

/// <summary>Scopes a theme palette to one element, so a sheet can differ from the application theme.</summary>
public static class SheetTheme
{
    public static readonly DependencyProperty IdProperty = DependencyProperty.RegisterAttached("Id", typeof(string),
        typeof(SheetTheme), new PropertyMetadata(null, OnIdChanged));
    private static readonly DependencyProperty PaletteProperty = DependencyProperty.RegisterAttached("Palette",
        typeof(ResourceDictionary), typeof(SheetTheme));

    public static string? GetId(DependencyObject element) => (string?)element.GetValue(IdProperty);
    public static void SetId(DependencyObject element, string? value) => element.SetValue(IdProperty, value);

    private static void OnIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var element = (FrameworkElement)d;
        if (element.GetValue(PaletteProperty) is ResourceDictionary previous)
            element.Resources.MergedDictionaries.Remove(previous);
        var palette = e.NewValue is string id ? ThemeService.LoadSheet(ThemeCatalog.Get(id)) : null;
        element.SetValue(PaletteProperty, palette);
        if (palette is not null) element.Resources.MergedDictionaries.Add(palette);
    }
}
