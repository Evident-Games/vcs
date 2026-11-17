using System.Windows;
using System.Windows.Controls;

namespace VCS.Desktop.Controls;

public partial class NumericField : UserControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(int), typeof(NumericField),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(int), typeof(NumericField), new PropertyMetadata(0));
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(int), typeof(NumericField), new PropertyMetadata(10));
    public int Value { get => (int)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public int Minimum { get => (int)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public int Maximum { get => (int)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public NumericField() => InitializeComponent();
    private void Decrease(object sender, RoutedEventArgs e)
    {
        if (Value > Minimum) SetCurrentValue(ValueProperty, Value - 1);
    }
    private void Increase(object sender, RoutedEventArgs e)
    {
        if (Value < Maximum) SetCurrentValue(ValueProperty, Value + 1);
    }
}
