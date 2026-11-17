using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VCS.Desktop.Controls;

/// <summary>Shrinks a TextBox font so its text fits the visible width; wraps once the minimum size is reached.</summary>
public static class FitText
{
    public const double MinimumFontSize = 8;

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(FitText), new PropertyMetadata(false, OnEnabledChanged));
    private static readonly DependencyProperty BaseFontSizeProperty = DependencyProperty.RegisterAttached(
        "BaseFontSize", typeof(double), typeof(FitText), new PropertyMetadata(double.NaN));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not TextBox box) return;
        if ((bool)e.NewValue)
        {
            box.TextAlignment = TextAlignment.Center;
            box.VerticalContentAlignment = VerticalAlignment.Center;
            box.TextChanged += OnChanged;
            box.SizeChanged += OnChanged;
            box.Loaded += OnChanged;
            Fit(box);
        }
        else
        {
            box.TextChanged -= OnChanged;
            box.SizeChanged -= OnChanged;
            box.Loaded -= OnChanged;
        }
    }

    private static void OnChanged(object sender, RoutedEventArgs e) => Fit((TextBox)sender);

    private static void Fit(TextBox box)
    {
        var baseSize = (double)box.GetValue(BaseFontSizeProperty);
        if (double.IsNaN(baseSize))
        {
            baseSize = box.FontSize;
            box.SetValue(BaseFontSizeProperty, baseSize);
        }
        var available = box.ActualWidth - box.Padding.Left - box.Padding.Right
            - box.BorderThickness.Left - box.BorderThickness.Right - 6;
        if (available <= 0 || string.IsNullOrEmpty(box.Text))
        {
            Apply(box, baseSize, false);
            return;
        }
        var text = new FormattedText(box.Text, System.Globalization.CultureInfo.CurrentCulture,
            box.FlowDirection, new Typeface(box.FontFamily, box.FontStyle, box.FontWeight, box.FontStretch),
            baseSize, Brushes.Black, VisualTreeHelper.GetDpi(box).PixelsPerDip);
        var size = Math.Clamp(Math.Floor(baseSize * available / text.WidthIncludingTrailingWhitespace * 2) / 2,
            MinimumFontSize, baseSize);
        Apply(box, size, text.WidthIncludingTrailingWhitespace * size / baseSize > available);
    }

    private static void Apply(TextBox box, double size, bool wrap)
    {
        if (box.FontSize != size) box.FontSize = size;
        var wrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
        if (box.TextWrapping != wrapping) box.TextWrapping = wrapping;
    }
}
