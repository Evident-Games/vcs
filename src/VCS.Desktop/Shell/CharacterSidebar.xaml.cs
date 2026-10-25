using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace VCS.Desktop.Shell;

/// <summary>Character list beside the sheets.</summary>
public partial class CharacterSidebar : UserControl
{
    public CharacterSidebar() => InitializeComponent();

    private void OnNewSheetMenu(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.Placement = PlacementMode.Bottom;
        button.ContextMenu.IsOpen = true;
    }
}
