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
        editor.FloatingX = Math.Clamp(editor.FloatingX + e.HorizontalChange, 0, Math.Max(0, canvas.ActualWidth - editor.FloatingWidth));
        editor.FloatingY = Math.Clamp(editor.FloatingY + e.VerticalChange, 0, Math.Max(0, canvas.ActualHeight - editor.FloatingHeight));
        e.Handled = true;
    }

    private void OnFloatingResizeDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not MainViewModel { FloatingWindows: true } model ||
            sender is not FrameworkElement { DataContext: CharacterEditorViewModel editor } handle) return;
        DependencyObject? parent = handle;
        while (parent is not null && parent is not Canvas) parent = VisualTreeHelper.GetParent(parent);
        if (parent is not Canvas canvas) return;
        model.ActivateEditorCommand.Execute(editor);
        var maxWidth = Math.Max(1, canvas.ActualWidth - editor.FloatingX);
        var maxHeight = Math.Max(1, canvas.ActualHeight - editor.FloatingY);
        editor.FloatingWidth = Math.Clamp(editor.FloatingWidth + e.HorizontalChange, Math.Min(300, maxWidth), maxWidth);
        editor.FloatingHeight = Math.Clamp(editor.FloatingHeight + e.VerticalChange, Math.Min(240, maxHeight), maxHeight);
        e.Handled = true;
    }

    private void OnWindowViewMenu(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { ContextMenu: { } menu } button) return;
        menu.PlacementTarget = button;
        menu.DataContext = button.DataContext;
        menu.IsOpen = true;
    }

    private void OnWindowViewSelected(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { DataContext: CharacterEditorViewModel editor, Tag: string mode } &&
            mode is "Default" or "Continuous" or "Tabbed" or "Bento") editor.WindowViewMode = mode;
    }

    private void OnSheetPaneMouseDown(object sender, MouseButtonEventArgs e) => ActivateSheetPane(sender);

    private void OnSheetPaneGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => ActivateSheetPane(sender);

    private void ActivateSheetPane(object sender)
    {
        if (DataContext is not MainViewModel model || (sender as FrameworkElement)?.DataContext is not CharacterEditorViewModel editor) return;
        model.Display.HudCollapsed = true;
        model.ActivateEditorCommand.Execute(editor);
    }
}
