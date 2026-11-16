using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace VCS.Desktop.Shell;

/// <summary>Native floating HUD. Only its visible surfaces take pointer input.</summary>
public partial class CharacterSidebar : UserControl
{
    public static readonly DependencyProperty IsCollapsedProperty = DependencyProperty.Register(nameof(IsCollapsed),
        typeof(bool), typeof(CharacterSidebar), new PropertyMetadata(false, OnCollapsedChanged));

    public bool IsCollapsed { get => (bool)GetValue(IsCollapsedProperty); set => SetValue(IsCollapsedProperty, value); }

    public CharacterSidebar()
    {
        InitializeComponent();
        SetBinding(IsCollapsedProperty, new Binding("Display.HudCollapsed") { Mode = BindingMode.TwoWay });
        Loaded += OnLoaded;
    }

    private static void OnCollapsedChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        var sidebar = (CharacterSidebar)element;
        if (sidebar.IsLoaded) sidebar.AnimateDrawer(true);
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => AnimateDrawer(false);

    private void AnimateDrawer(bool animate)
    {
        var duration = new Duration(TimeSpan.FromMilliseconds(animate && SystemParameters.ClientAreaAnimation ? 220 : 0));
        var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };
        DrawerHost.IsHitTestVisible = !IsCollapsed;
        DrawerHost.IsEnabled = !IsCollapsed;
        DrawerHost.BeginAnimation(WidthProperty, new DoubleAnimation(IsCollapsed ? 0 : 352, duration) { EasingFunction = easing });
        Drawer.BeginAnimation(OpacityProperty, new DoubleAnimation(IsCollapsed ? 0 : 1, duration) { EasingFunction = easing });
        DrawerTranslation.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty,
            new DoubleAnimation(IsCollapsed ? 18 : 0, duration) { EasingFunction = easing });
    }

    private void OnNewSheetMenu(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { ContextMenu: { } menu } button) return;
        menu.PlacementTarget = button;
        menu.Placement = PlacementMode.Left;
        menu.IsOpen = true;
    }

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
