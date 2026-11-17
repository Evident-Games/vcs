using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;

namespace VCS.Desktop.Shell;

/// <summary>Native floating HUD. Only its visible surfaces take pointer input.</summary>
public partial class Sidebar : UserControl
{
    public static readonly DependencyProperty IsCollapsedProperty = DependencyProperty.Register(nameof(IsCollapsed),
        typeof(bool), typeof(Sidebar), new PropertyMetadata(false, OnCollapsedChanged));

    public bool IsCollapsed { get => (bool)GetValue(IsCollapsedProperty); set => SetValue(IsCollapsedProperty, value); }

    public Sidebar()
    {
        InitializeComponent();
        SetBinding(IsCollapsedProperty, new Binding("Display.HudCollapsed") { Mode = BindingMode.TwoWay });
        Loaded += OnLoaded;
        IsVisibleChanged += (_, _) => { if (!IsVisible) ProfilePopup.IsOpen = false; };
    }

    private static void OnCollapsedChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        var sidebar = (Sidebar)element;
        if (sidebar.IsLoaded) sidebar.UpdateDrawer();
    }

    private const double MarginSize = 16;
    private const double Gap = 12;
    private bool dragging;
    private double dragX, dragY, panelDragX, panelDragY;
    private string previewEdge = "Right";
    private Layout.DisplaySettingsViewModel? display;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (display != null) display.PropertyChanged -= OnDisplayChanged;
        display = (DataContext as MainViewModel)?.Display;
        if (display != null) display.PropertyChanged += OnDisplayChanged;
        Unloaded -= OnUnloaded;
        Unloaded += OnUnloaded;
        UpdateDrawer();
        Dispatcher.BeginInvoke(new Action(UpdateLayoutPosition));
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (display != null) display.PropertyChanged -= OnDisplayChanged;
        display = null;
    }

    private void OnDisplayChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Layout.DisplaySettingsViewModel.HudEdge) or nameof(Layout.DisplaySettingsViewModel.HudPosition)
            or nameof(Layout.DisplaySettingsViewModel.HudCompact) or nameof(Layout.DisplaySettingsViewModel.HudCollapsed)
            or nameof(Layout.DisplaySettingsViewModel.HudSize))
            Dispatcher.BeginInvoke(new Action(UpdateLayoutPosition));
    }

    private void UpdateDrawer()
    {
        // Keep the collapsed surface out of hit testing and keyboard navigation.
        DrawerHost.Visibility = IsCollapsed ? Visibility.Collapsed : Visibility.Visible;
        DrawerHost.IsHitTestVisible = !IsCollapsed;
        DrawerHost.IsEnabled = !IsCollapsed;
        UpdateLayoutPosition();
    }

    private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e) => UpdateLayoutPosition();

    private void UpdateLayoutPosition()
    {
        if (!IsLoaded || display == null || dragging || HudCanvas.ActualWidth <= 0 || HudCanvas.ActualHeight <= 0) return;
        var w = HudCanvas.ActualWidth;
        var h = HudCanvas.ActualHeight;
        var edge = display.HudEdge;
        var horizontal = edge is "Top" or "Bottom";
        DockBar.Orientation = horizontal ? Orientation.Horizontal : Orientation.Vertical;
        DockPill.Margin = horizontal ? new Thickness(14, 0, 0, 0) : new Thickness(0, 14, 0, 0);
        DockPill.Padding = horizontal ? new Thickness(10, 5, 10, 5) : new Thickness(5, 10, 5, 10);
        DockButtons.Orientation = horizontal ? Orientation.Horizontal : Orientation.Vertical;
        DockBar.Measure(new Size(w, h));
        var bw = DockBar.DesiredSize.Width;
        var bh = DockBar.DesiredSize.Height;
        var scale = display.HudScale;
        var panelWidth = Math.Min(340 * scale, Math.Max(0, w - bw - Gap - 2 * MarginSize));
        DrawerHost.Width = panelWidth / scale;
        Drawer.Width = panelWidth / scale;
        DrawerShadow.Width = panelWidth / scale;
        var panelHeight = Math.Max(0, (display.HudCompact ? 0.62 : 1) * (h - 2 * MarginSize));
        if (horizontal) panelHeight = Math.Min(panelHeight, Math.Max(0, h - bh - Gap - 2 * MarginSize));
        DrawerHost.Height = panelHeight / scale;
        var position = Math.Clamp(display.HudPosition, 0, 1);
        var availableX = Math.Max(0, w - 2 * MarginSize - bw);
        var availableY = Math.Max(0, h - 2 * MarginSize - bh);
        double x, y, px, py;
        if (horizontal)
        {
            x = MarginSize + position * availableX;
            y = edge == "Top" ? MarginSize : h - MarginSize - bh;
            px = Math.Clamp(x, MarginSize, Math.Max(MarginSize, w - MarginSize - panelWidth));
            py = edge == "Top" ? y + bh + Gap : y - Gap - panelHeight;
        }
        else
        {
            y = MarginSize + position * availableY;
            x = edge == "Left" ? MarginSize : w - MarginSize - bw;
            px = edge == "Left" ? x + bw + Gap : x - Gap - panelWidth;
            // Align the closest panel end with the bar, then clamp to the visible workspace.
            py = y + bh / 2 <= h / 2 ? y : y + bh - panelHeight;
            py = Math.Clamp(py, MarginSize, Math.Max(MarginSize, h - MarginSize - panelHeight));
        }
        Canvas.SetLeft(DockBar, x); Canvas.SetTop(DockBar, y);
        Canvas.SetLeft(DrawerHost, px); Canvas.SetTop(DrawerHost, py);
        var placement = edge switch { "Left" => PlacementMode.Right, "Top" => PlacementMode.Bottom,
            "Bottom" => PlacementMode.Top, _ => PlacementMode.Left };
        foreach (var tip in new[] { CharactersTip, ChroniclesTip })
        {
            tip.Placement = placement;
            tip.HorizontalOffset = placement == PlacementMode.Left ? -12 : placement == PlacementMode.Right ? 12 : 0;
            tip.VerticalOffset = placement == PlacementMode.Top ? -8 : placement == PlacementMode.Bottom ? 8 : 0;
        }
    }

    private void OnToggleHeight(object sender, RoutedEventArgs e)
    {
        if (display != null) display.HudCompact = !display.HudCompact;
    }

    private void OnDragStarted(object sender, DragStartedEventArgs e)
    {
        dragging = true;
        dragX = Canvas.GetLeft(DockBar); dragY = Canvas.GetTop(DockBar);
        panelDragX = Canvas.GetLeft(DrawerHost) - dragX;
        panelDragY = Canvas.GetTop(DrawerHost) - dragY;
        previewEdge = display?.HudEdge ?? "Right";
        SnapPreview.Visibility = Visibility.Visible;
    }

    private void OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        dragX = Math.Clamp(dragX + e.HorizontalChange, 0, Math.Max(0, HudCanvas.ActualWidth - DockBar.DesiredSize.Width));
        dragY = Math.Clamp(dragY + e.VerticalChange, 0, Math.Max(0, HudCanvas.ActualHeight - DockBar.DesiredSize.Height));
        Canvas.SetLeft(DockBar, dragX); Canvas.SetTop(DockBar, dragY);
        Canvas.SetLeft(DrawerHost, dragX + panelDragX); Canvas.SetTop(DrawerHost, dragY + panelDragY);
        var w = HudCanvas.ActualWidth; var h = HudCanvas.ActualHeight;
        var distances = new[] { ("Left", dragX), ("Right", w - dragX - DockBar.DesiredSize.Width),
            ("Top", dragY), ("Bottom", h - dragY - DockBar.DesiredSize.Height) };
        previewEdge = distances.OrderBy(x => x.Item2).First().Item1;
        var horizontal = previewEdge is "Top" or "Bottom";
        SnapPreview.Width = horizontal ? w : 5; SnapPreview.Height = horizontal ? 5 : h;
        Canvas.SetLeft(SnapPreview, previewEdge == "Right" ? w - 5 : 0);
        Canvas.SetTop(SnapPreview, previewEdge == "Bottom" ? h - 5 : 0);
    }

    private void OnDragCompleted(object sender, DragCompletedEventArgs e)
    {
        dragging = false;
        SnapPreview.Visibility = Visibility.Collapsed;
        DrawerHost.Visibility = IsCollapsed ? Visibility.Collapsed : Visibility.Visible;
        if (display != null && !e.Canceled)
        {
            var horizontal = previewEdge is "Top" or "Bottom";
            var size = horizontal ? HudCanvas.ActualWidth : HudCanvas.ActualHeight;
            var bar = horizontal ? DockBar.DesiredSize.Width : DockBar.DesiredSize.Height;
            var offset = horizontal ? dragX : dragY;
            display.HudEdge = previewEdge;
            display.HudPosition = Math.Clamp((offset - MarginSize) / Math.Max(1, size - 2 * MarginSize - bar), 0, 1);
        }
        UpdateLayoutPosition();
    }

    private void OnNewSheetMenu(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { ContextMenu: { } menu } button) return;
        menu.PlacementTarget = button;
        menu.Placement = PlacementMode.Left;
        menu.IsOpen = true;
    }

    private void OnOpenProfile(object sender, RoutedEventArgs e) => ProfilePopup.IsOpen = !ProfilePopup.IsOpen;

    private void OnChooseAvatar(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog
        {
            Title = "Avatar do jogador local",
            Filter = "Imagens|*.png;*.jpg;*.jpeg;*.bmp",
            CheckFileExists = true,
            Multiselect = false
        };
        if (picker.ShowDialog(Window.GetWindow(this)) == true && DataContext is MainViewModel model)
            model.Display.HudAvatarPath = picker.FileName;
    }
}
