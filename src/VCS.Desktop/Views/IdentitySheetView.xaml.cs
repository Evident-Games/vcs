using System.Windows;
using System.Windows.Controls;

namespace VCS.Desktop.Views;

public partial class IdentitySheetView : UserControl
{
    public IdentitySheetView() => InitializeComponent();

    public ToolTip? ActiveHelp { get; private set; }

    // Click help uses its own ToolTip; the hover ToolTip must keep StaysOpen=true or WPF throws.
    private void ShowHelp(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        var text = button.Tag as string ?? button.ToolTip as string;
        if (string.IsNullOrWhiteSpace(text)) return;
        CloseHelp();
        if (button.ToolTip is ToolTip hover) hover.IsOpen = false;
        ActiveHelp = new ToolTip
        {
            Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, MaxWidth = 340 },
            PlacementTarget = button,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Top,
            StaysOpen = false
        };
        button.MouseLeave -= CloseHelpOnLeave;
        button.MouseLeave += CloseHelpOnLeave;
        ActiveHelp.IsOpen = true;
        e.Handled = true;
    }

    private void CloseHelpOnLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        ((Button)sender).MouseLeave -= CloseHelpOnLeave;
        CloseHelp();
    }

    public void CloseHelp()
    {
        if (ActiveHelp is null) return;
        ActiveHelp.IsOpen = false;
        ActiveHelp = null;
    }
}
