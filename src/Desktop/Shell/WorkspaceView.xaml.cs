using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
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
        if (DataContext is MainViewModel { FloatingWindows: true } && sender is DockPanel) return;
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
        model.FloatingWindows = false;
        model.SplitWindows = true;
        model.ActivateEditorCommand.Execute(editor);
        e.Handled = true;
    }

    private void OnFloatingDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not MainViewModel { FloatingWindows: true } model ||
            sender is not FrameworkElement { DataContext: CharacterEditorViewModel editor } handle) return;
        DependencyObject? parent = handle;
        while (parent is not null && parent is not Canvas) parent = VisualTreeHelper.GetParent(parent);
        if (parent is not Canvas canvas) return;
        model.ActivateEditorCommand.Execute(editor);
        editor.FloatingX = Math.Clamp(editor.FloatingX + e.HorizontalChange, 0, Math.Max(0, canvas.ActualWidth - 480));
        editor.FloatingY = Math.Clamp(editor.FloatingY + e.VerticalChange, 0, Math.Max(0, canvas.ActualHeight - 480));
        e.Handled = true;
    }

    private void OnSheetPaneMouseDown(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is CharacterEditorViewModel editor) ((MainViewModel)DataContext).ActivateEditorCommand.Execute(editor);
    }
}
