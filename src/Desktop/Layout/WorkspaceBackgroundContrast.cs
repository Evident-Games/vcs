using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VCS.Desktop.Layout;

/// <summary>Estimates image brightness for controls drawn directly over the workspace.</summary>
public static class WorkspaceBackgroundContrast
{
    public static bool IsDark(BitmapSource bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        BitmapSource source = bitmap.Format == PixelFormats.Bgra32 || bitmap.Format == PixelFormats.Pbgra32
            ? bitmap : new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
        var sample = new byte[4];
        double luminance = 0;
        const int grid = 5;
        for (var y = 0; y < grid; y++)
        for (var x = 0; x < grid; x++)
        {
            var px = Math.Min(source.PixelWidth - 1, (int)((x + 0.5) * source.PixelWidth / grid));
            var py = Math.Min(source.PixelHeight - 1, (int)((y + 0.5) * source.PixelHeight / grid));
            source.CopyPixels(new Int32Rect(px, py, 1, 1), sample, 4, 0);
            luminance += 0.0722 * sample[0] + 0.7152 * sample[1] + 0.2126 * sample[2];
        }
        return luminance / (grid * grid) < 140;
    }
}
