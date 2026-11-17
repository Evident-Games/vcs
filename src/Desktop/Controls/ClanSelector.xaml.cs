using System.Windows;
using System.Windows.Controls;

namespace VCS.Desktop.Controls;

public partial class ClanSelector : UserControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(ClanSelector),
        new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty CatalogProperty = DependencyProperty.Register(
        nameof(Catalog), typeof(IEnumerable<string>), typeof(ClanSelector), new PropertyMetadata(null));
    public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public IEnumerable<string>? Catalog
    {
        get => (IEnumerable<string>?)GetValue(CatalogProperty);
        set => SetValue(CatalogProperty, value);
    }
    public ClanSelector() => InitializeComponent();
}
