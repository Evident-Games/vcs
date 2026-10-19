using System.Windows;
using System.Windows.Controls;
using VCS.Desktop.ViewModels;

namespace VCS.Desktop.Controls;

public partial class PowerBrowser : UserControl
{
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        nameof(Items), typeof(IEnumerable<PowerItemViewModel>), typeof(PowerBrowser), new PropertyMetadata(null));
    public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(
        nameof(Selected), typeof(PowerItemViewModel), typeof(PowerBrowser),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public IEnumerable<PowerItemViewModel>? Items
    {
        get => (IEnumerable<PowerItemViewModel>?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }
    public PowerItemViewModel? Selected
    {
        get => (PowerItemViewModel?)GetValue(SelectedProperty);
        set => SetValue(SelectedProperty, value);
    }
    public PowerBrowser() => InitializeComponent();
}
