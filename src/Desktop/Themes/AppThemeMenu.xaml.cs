using System.Windows.Controls;

namespace Game.Desktop.Themes;

/// <summary>Application theme menu, reused wherever themes can be picked.</summary>
public partial class AppThemeMenu : MenuItem
{
    public AppThemeMenu()
    {
        InitializeComponent();
        // A subclass does not pick up the implicit MenuItem style by itself.
        SetResourceReference(StyleProperty, typeof(MenuItem));
    }
}
