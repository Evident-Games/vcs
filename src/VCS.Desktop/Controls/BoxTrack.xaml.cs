using System.Windows;
using System.Windows.Controls;
using VCS.Rules.V5.Validation;

namespace VCS.Desktop.Controls;

public partial class BoxTrack : UserControl
{
    public sealed record Box(int Number, string Mark, string Label, Thickness Margin);
    public static readonly DependencyProperty CapacityProperty = DependencyProperty.Register(nameof(Capacity), typeof(int),
        typeof(BoxTrack), new PropertyMetadata(5, Changed));
    public static readonly DependencyProperty SuperficialProperty = DependencyProperty.Register(nameof(Superficial), typeof(int),
        typeof(BoxTrack), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, Changed));
    public static readonly DependencyProperty AggravatedProperty = DependencyProperty.Register(nameof(Aggravated), typeof(int),
        typeof(BoxTrack), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, Changed));
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(int),
        typeof(BoxTrack), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, Changed));
    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(nameof(Mode), typeof(string),
        typeof(BoxTrack), new PropertyMetadata("Damage", Changed));
    public static readonly DependencyProperty BoxSizeProperty = DependencyProperty.Register(nameof(BoxSize), typeof(double),
        typeof(BoxTrack), new PropertyMetadata(28d));
    public static readonly DependencyProperty GroupSizeProperty = DependencyProperty.Register(nameof(GroupSize), typeof(int),
        typeof(BoxTrack), new PropertyMetadata(0, Changed));
    private static readonly DependencyPropertyKey BoxesPropertyKey = DependencyProperty.RegisterReadOnly(nameof(Boxes),
        typeof(Box[]), typeof(BoxTrack), new PropertyMetadata(Array.Empty<Box>()));
    public static readonly DependencyProperty BoxesProperty = BoxesPropertyKey.DependencyProperty;
    public int Capacity { get => (int)GetValue(CapacityProperty); set => SetValue(CapacityProperty, value); }
    public int Superficial { get => (int)GetValue(SuperficialProperty); set => SetValue(SuperficialProperty, value); }
    public int Aggravated { get => (int)GetValue(AggravatedProperty); set => SetValue(AggravatedProperty, value); }
    public int Value { get => (int)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public string Mode { get => (string)GetValue(ModeProperty); set => SetValue(ModeProperty, value); }
    public double BoxSize { get => (double)GetValue(BoxSizeProperty); set => SetValue(BoxSizeProperty, value); }
    public int GroupSize { get => (int)GetValue(GroupSizeProperty); set => SetValue(GroupSizeProperty, value); }
    public Box[] Boxes => (Box[])GetValue(BoxesProperty);
    public BoxTrack() { InitializeComponent(); Refresh(); }
    private static void Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((BoxTrack)d).Refresh();
    private void Refresh() => SetValue(BoxesPropertyKey, Enumerable.Range(1, Math.Clamp(Capacity, 1, CharacterRules.NpcMaximumBoxes))
        .Select(i => new Box(i, Mode == "Count" ? (i <= Value ? "X" : "") :
            i <= Aggravated ? "X" : i <= Aggravated + Superficial ? "\u25a0" : "",
            $"Caixa {i}: " + (Mode == "Count" ? (i <= Value ? "marcada" : "vazia") :
                i <= Aggravated ? "X" : i <= Aggravated + Superficial ? "cheia" : "vazia"),
            new Thickness(2, 2, GroupSize > 0 && i % GroupSize == 0 ? 8 : 2, 2))).ToArray());
    private void SelectBox(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int number }) return;
        if (Mode == "Count") SetCurrentValue(ValueProperty, number == Value ? number - 1 : number);
        else if (number <= Aggravated) SetCurrentValue(AggravatedProperty, Aggravated - 1);
        else if (number <= Aggravated + Superficial)
        {
            SetCurrentValue(SuperficialProperty, Superficial - 1);
            SetCurrentValue(AggravatedProperty, Aggravated + 1);
        }
        else if (Mode == "Cross") SetCurrentValue(AggravatedProperty, Aggravated + 1);
        else SetCurrentValue(SuperficialProperty, Superficial + 1);
    }
}
