using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VCS.Desktop.Sheet;

namespace VCS.Desktop.Shell;

/// <summary>Open sheet tabs, home view and the open sheets.</summary>
public partial class WorkspaceView : UserControl
{
    public WorkspaceView() => InitializeComponent();

    private void OnSheetPaneMouseDown(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is CharacterEditorViewModel editor) ((MainViewModel)DataContext).ActivateEditorCommand.Execute(editor);
    }
}
