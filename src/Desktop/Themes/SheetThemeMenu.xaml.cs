using System.Windows.Controls;

namespace Game.Desktop.Themes;

/// <summary>Theme menu for the active sheet.</summary>
public partial class SheetThemeMenu : MenuItem
{
    public SheetThemeMenu()
    {
        InitializeComponent();
        // A subclass does not pick up the implicit MenuItem style by itself.
        SetResourceReference(StyleProperty, typeof(MenuItem));
    }
}
