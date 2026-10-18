using System.Windows;
using System.Windows.Controls;

namespace VCS.Desktop.Controls;

public partial class DotTrack : UserControl
{
    public sealed record Dot(int Number, bool Filled);
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(int), typeof(DotTrack),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTrackChanged));
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(int), typeof(DotTrack), new PropertyMetadata(5, OnTrackChanged));
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(int), typeof(DotTrack), new PropertyMetadata(0, OnTrackChanged));
    private static readonly DependencyPropertyKey DotsPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Dots), typeof(Dot[]), typeof(DotTrack), new PropertyMetadata(Array.Empty<Dot>()));
    public static readonly DependencyProperty DotsProperty = DotsPropertyKey.DependencyProperty;
    public int Value { get => (int)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public int Maximum { get => (int)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public int Minimum { get => (int)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public Dot[] Dots => (Dot[])GetValue(DotsProperty);

    public DotTrack()
    {
        InitializeComponent();
        Refresh();
    }
    private static void OnTrackChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((DotTrack)d).Refresh();
    private void Refresh()
    {
        SetValue(DotsPropertyKey, Enumerable.Range(1, Math.Clamp(Maximum, 1, 10))
            .Select(number => new Dot(number, number <= Value)).ToArray());
    }
    private void SelectDot(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int number } && number >= Minimum)
            SetCurrentValue(ValueProperty, number == Value ? Math.Max(Minimum, number - 1) : number);
    }
}
