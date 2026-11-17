using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VCS.Desktop.Shell;

namespace VCS.Desktop.Layout;

/// <summary>Layout editor with list, tab and mini bento previews; drag and drop rearranges sections.</summary>
public partial class LayoutEditorView : UserControl
{
    public LayoutEditorView() => InitializeComponent();

    private Point _dragStart;
    private SectionOption? _dragCandidate;

    private void OnSectionHandleDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
        _dragCandidate = (sender as FrameworkElement)?.DataContext as SectionOption;
    }

    private void OnSectionHandleMove(object sender, MouseEventArgs e)
    {
        if (_dragCandidate is null || e.LeftButton != MouseButtonState.Pressed) return;
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

    private SheetLayout EditedLayout => ((MainViewModel)DataContext).Display.EditedLayout;

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
        if (sender is TextBox { DataContext: LayoutTab tab } box && box.Text.Trim() != tab.Name)
            EditedLayout.RenameTab(tab.Name, box.Text);
    }

    private void OnTabNameKey(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        ((UIElement)sender).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        e.Handled = true;
    }

    private void OnSectionHandleKey(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if ((Keyboard.Modifiers & ModifierKeys.Alt) == 0 ||
            key is not (Key.Up or Key.Down) ||
            (sender as FrameworkElement)?.DataContext is not SectionOption section) return;
        EditedLayout.MoveSection(section, EditedLayout.OrderedSections.IndexOf(section) + (key == Key.Up ? -1 : 1));
        e.Handled = true;
        Dispatcher.BeginInvoke(() => FindHandle(section)?.Focus(), System.Windows.Threading.DispatcherPriority.Input);
    }

    private Border? FindRow(SectionOption section) =>
        SectionOrganizer.ItemContainerGenerator.ContainerFromItem(section) is DependencyObject container
            ? FindDescendant<Border>(container) : null;

    private Button? FindHandle(SectionOption section) =>
        SectionOrganizer.ItemContainerGenerator.ContainerFromItem(section) is DependencyObject container
            ? FindDescendant<Button>(container) : null;

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) return match;
            if (FindDescendant<T>(child) is { } nested) return nested;
        }
        return null;
    }
}
