using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VCS.Desktop.Sheet;

namespace VCS.Desktop.Shell;

/// <summary>Open sheet tabs, home view and the open sheets.</summary>
public partial class WorkspaceView : UserControl
{
    private Point _tabDragStart;
    public WorkspaceView() => InitializeComponent();

    private void OnTabMouseDown(object sender, MouseButtonEventArgs e) => _tabDragStart = e.GetPosition(this);

    private void OnTabMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || sender is not FrameworkElement { DataContext: CharacterEditorViewModel editor }) return;
        var position = e.GetPosition(this);
        if (Math.Abs(position.X - _tabDragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(position.Y - _tabDragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        DragDrop.DoDragDrop((DependencyObject)sender, editor, DragDropEffects.Move);
    }

    private void OnTabDrop(object sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel model || sender is not FrameworkElement { DataContext: CharacterEditorViewModel target } ||
            e.Data.GetData(typeof(CharacterEditorViewModel)) is not CharacterEditorViewModel source) return;
        var from = model.OpenEditors.IndexOf(source);
        var to = model.OpenEditors.IndexOf(target);
        if (from >= 0 && to >= 0 && from != to) model.OpenEditors.Move(from, to);
        e.Handled = true;
    }

    private void OnWorkspaceDrop(object sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel model || sender is not FrameworkElement workspace ||
            e.Data.GetData(typeof(CharacterEditorViewModel)) is not CharacterEditorViewModel editor ||
            model.OpenEditors.Count < 2) return;
        var from = model.OpenEditors.IndexOf(editor);
        if (from < 0) return;
        var to = e.GetPosition(workspace).X < workspace.ActualWidth / 2 ? 0 : model.OpenEditors.Count - 1;
        if (from != to) model.OpenEditors.Move(from, to);
        model.SplitWindows = true;
        model.ActivateEditorCommand.Execute(editor);
        e.Handled = true;
    }

    private void OnSheetPaneMouseDown(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is CharacterEditorViewModel editor) ((MainViewModel)DataContext).ActivateEditorCommand.Execute(editor);
    }
}
