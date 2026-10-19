using System.Windows;
using System.Windows.Controls;

namespace VCS.Desktop.Controls;

public partial class DamageTrack : UserControl
{
    public static readonly DependencyProperty CapacityProperty = DependencyProperty.Register(
        nameof(Capacity), typeof(int), typeof(DamageTrack), new PropertyMetadata(1, OnDamageChanged));
    public static readonly DependencyProperty SuperficialProperty = DependencyProperty.Register(
        nameof(Superficial), typeof(int), typeof(DamageTrack),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDamageChanged));
    public static readonly DependencyProperty AggravatedProperty = DependencyProperty.Register(
        nameof(Aggravated), typeof(int), typeof(DamageTrack),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDamageChanged));
    private static readonly DependencyPropertyKey RemainingPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Remaining), typeof(int), typeof(DamageTrack), new PropertyMetadata(1));
    public static readonly DependencyProperty RemainingProperty = RemainingPropertyKey.DependencyProperty;
    public int Capacity { get => (int)GetValue(CapacityProperty); set => SetValue(CapacityProperty, value); }
    public int Superficial { get => (int)GetValue(SuperficialProperty); set => SetValue(SuperficialProperty, value); }
    public int Aggravated { get => (int)GetValue(AggravatedProperty); set => SetValue(AggravatedProperty, value); }
    public int Remaining => (int)GetValue(RemainingProperty);
    public DamageTrack() => InitializeComponent();
    private static void OnDamageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var track = (DamageTrack)d;
        track.SetValue(RemainingPropertyKey, Math.Max(0, track.Capacity - track.Superficial - track.Aggravated));
    }
}
