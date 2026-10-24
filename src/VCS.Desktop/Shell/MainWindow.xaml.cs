using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using VCS.Desktop.Layout;
using VCS.Desktop.Sheet;
using Wpf.Ui.Controls;

namespace VCS.Desktop.Shell;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;
    private bool _approved;
    private bool _checkingClose;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
        Closing += OnClosing;
    }

    private void OnNewSheetMenu(object sender, RoutedEventArgs e)
    {
        var button = (System.Windows.Controls.Button)sender;
        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.Placement = PlacementMode.Bottom;
        button.ContextMenu.IsOpen = true;
    }

    private void OnSheetPaneMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is CharacterEditorViewModel editor) _viewModel.ActivateEditorCommand.Execute(editor);
    }

    private Point _dragStart;
    private SectionOption? _dragCandidate;

    private void OnSectionHandleDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
        _dragCandidate = (sender as FrameworkElement)?.DataContext as SectionOption;
    }

    private void OnSectionHandleMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_dragCandidate is null || e.LeftButton != System.Windows.Input.MouseButtonState.Pressed) return;
        var delta = e.GetPosition(this) - _dragStart;
        if (Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance &&
            Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance) return;
        var section = _dragCandidate;
        _dragCandidate = null;
        var row = FindRow(section);
        if (row is not null) row.Opacity = 0.45;
        try { DragDrop.DoDragDrop((DependencyObject)sender, new DataObject(typeof(SectionOption), section), DragDropEffects.Move); }
        finally
        {
            row = FindRow(section);
            if (row is not null) row.Opacity = 1;
        }
    }

    // Reorders live while hovering so the other rows are pushed aside before the drop.
    private void OnSectionDragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (e.Data.GetData(typeof(SectionOption)) is not SectionOption dragged ||
            (sender as FrameworkElement)?.DataContext is not SectionOption target)
        {
            e.Effects = DragDropEffects.None;
            return;
        }
        e.Effects = DragDropEffects.Move;
        if (ReferenceEquals(dragged, target)) return;
        EditedLayout.MoveSection(dragged, EditedLayout.OrderedSections.IndexOf(target));
        var row = FindRow(dragged);
        if (row is not null) row.Opacity = 0.45;
    }

    private void OnSectionDrop(object sender, DragEventArgs e) => e.Handled = true;

    private SheetLayout EditedLayout => _viewModel.Display.EditedLayout;

    private void OnTabDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(SectionOption)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    // Dropping on a tab column moves the section into that tab; the "Nova aba" target creates one.
    private void OnTabDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (e.Data.GetData(typeof(SectionOption)) is not SectionOption section) return;
        var tab = (sender as FrameworkElement)?.DataContext is LayoutTab target ? target.Name : EditedLayout.NewTabName();
        EditedLayout.MoveToTab(section, tab);
    }

    private void OnTabRenamed(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox { DataContext: LayoutTab tab } box && box.Text.Trim() != tab.Name)
            EditedLayout.RenameTab(tab.Name, box.Text);
    }

    private void OnTabNameKey(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Enter) return;
        ((UIElement)sender).MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next));
        e.Handled = true;
    }

    private void OnSectionHandleKey(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
        if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Alt) == 0 ||
            key is not (System.Windows.Input.Key.Up or System.Windows.Input.Key.Down) ||
            (sender as FrameworkElement)?.DataContext is not SectionOption section) return;
        EditedLayout.MoveSection(section, EditedLayout.OrderedSections.IndexOf(section) + (key == System.Windows.Input.Key.Up ? -1 : 1));
        e.Handled = true;
        Dispatcher.BeginInvoke(() => FindHandle(section)?.Focus(), System.Windows.Threading.DispatcherPriority.Input);
    }

    private System.Windows.Controls.Border? FindRow(SectionOption section) =>
        SectionOrganizer.ItemContainerGenerator.ContainerFromItem(section) is DependencyObject container
            ? FindDescendant<System.Windows.Controls.Border>(container) : null;

    private System.Windows.Controls.Button? FindHandle(SectionOption section) =>
        SectionOrganizer.ItemContainerGenerator.ContainerFromItem(section) is DependencyObject container
            ? FindDescendant<System.Windows.Controls.Button>(container) : null;

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (child is T match) return match;
            if (FindDescendant<T>(child) is { } nested) return nested;
        }
        return null;
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_approved) return;
        e.Cancel = true;
        if (_checkingClose) return;
        _checkingClose = true;
        try
        {
            if (await _viewModel.TryCloseAsync())
            {
                _approved = true;
                // Defer until the original Closing event has returned.
                _ = Dispatcher.BeginInvoke(Close);
            }
        }
        finally { _checkingClose = false; }
    }
}
