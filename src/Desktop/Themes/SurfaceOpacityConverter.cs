using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace VCS.Desktop.Themes;

/// <summary>Applies transparency to the surface brush without fading its contents.</summary>
public sealed class SurfaceOpacityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 0 || values[0] is not Brush brush) return Brushes.Transparent;
        var opacity = values.Length > 1 && values[1] is double number && double.IsFinite(number)
            ? Math.Clamp(number, 0.25, 1) : 1;
        var result = brush.CloneCurrentValue();
        result.Opacity *= opacity;
        result.Freeze();
        return result;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        targetTypes.Select(_ => DependencyProperty.UnsetValue).ToArray();
}
