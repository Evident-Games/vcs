using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace VCS.Desktop.Controls;

public static class ScrollChaining
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(ScrollChaining), new PropertyMetadata(false, OnEnabledChanged));
    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
    private static void OnEnabledChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not ScrollViewer viewer) return;
        if ((bool)e.NewValue) viewer.PreviewMouseWheel += OnWheel;
        else viewer.PreviewMouseWheel -= OnWheel;
    }

    private static void OnWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer viewer || e.Handled || e.Delta == 0) return;
        var origin = e.OriginalSource as DependencyObject;
        if (FindViewer(origin) != viewer) return;
        if (CanScroll(viewer, e.Delta)) return;
        var parent = FindViewer(VisualTreeHelper.GetParent(viewer));
        while (parent is not null && !CanScroll(parent, e.Delta))
            parent = FindViewer(VisualTreeHelper.GetParent(parent));
        if (parent is null) return;
        var distance = SystemParameters.WheelScrollLines < 0
            ? parent.ViewportHeight : SystemParameters.WheelScrollLines * 16;
        if (parent.CanContentScroll && SystemParameters.WheelScrollLines >= 0)
            distance = SystemParameters.WheelScrollLines;
        parent.ScrollToVerticalOffset(parent.VerticalOffset - e.Delta / 120d * distance);
        e.Handled = true;
    }

    private static bool CanScroll(ScrollViewer viewer, int delta) =>
        delta > 0 ? viewer.VerticalOffset > 0 : viewer.VerticalOffset < viewer.ScrollableHeight;
    private static ScrollViewer? FindViewer(DependencyObject? element)
    {
        while (element is not null)
        {
            if (element is ScrollViewer viewer) return viewer;
            element = element is Visual ? VisualTreeHelper.GetParent(element) : LogicalTreeHelper.GetParent(element);
        }
        return null;
    }
}
