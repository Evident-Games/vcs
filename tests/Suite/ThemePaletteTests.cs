using System.Windows;
using System.Windows.Media;
using VCS.Desktop.Themes;

namespace VCS.Tests;

public sealed class ThemePaletteTests
{
    private static SolidColorBrush Brush(byte value) => new(Color.FromRgb(value, value, value));

    [Theory]
    [InlineData(15, true)]
    [InlineData(240, false)]
    public void Workspace_contrast_uses_background_brightness(byte level, bool expectedDark)
    {
        var bitmap = System.Windows.Media.Imaging.BitmapSource.Create(1, 1, 96, 96,
            PixelFormats.Bgra32, null, new byte[] { level, level, level, 255 }, 4);
        Assert.Equal(expectedDark, VCS.Desktop.Layout.WorkspaceBackgroundContrast.IsDark(bitmap));
    }

    [Fact]
    public void Surface_opacity_clones_the_brush_without_changing_the_theme()
    {
        var original = new SolidColorBrush(Colors.DarkRed);
        var converter = new SurfaceOpacityConverter();
        var result = Assert.IsType<SolidColorBrush>(converter.Convert(
            [original, 0.5], typeof(Brush), null!, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(0.5, result.Opacity);
        Assert.Equal(1, original.Opacity);
        Assert.Equal(original.Color, result.Color);
    }

    [Fact]
    public void Application_block_lists_only_differences_and_inherits_the_rest_from_the_sheet_block()
    {
        var file = new ResourceDictionary
        {
            ["ThemeSheetBackground"] = Brush(1), ["ThemeSheetCard"] = Brush(2), ["ThemeSheetOuterText"] = Brush(3),
            ["ThemeSheetInputText"] = Brush(4),
            ["ThemeAppBackground"] = Brush(10), ["ThemeAppOuterText"] = Brush(11), ["ThemeAppHeaderText"] = Brush(12)
        };

        var app = ThemeService.Load(file, ThemePart.Application);
        var sheet = ThemeService.Load(file, ThemePart.Sheet);

        Color Of(ResourceDictionary palette, string key) => ((SolidColorBrush)palette[key]).Color;
        Assert.Equal(Color.FromRgb(10, 10, 10), Of(app, "ThemeBackground"));
        Assert.Equal(Color.FromRgb(2, 2, 2), Of(app, "ThemeCard"));
        Assert.Equal(Color.FromRgb(4, 4, 4), Of(app, "ThemeInputText"));
        Assert.Equal(Color.FromRgb(12, 12, 12), Of(app, "ThemeHeaderText"));
        Assert.Equal(Color.FromRgb(11, 11, 11), Of(app, "ThemeFooterText"));
        Assert.Equal(Color.FromRgb(1, 1, 1), Of(sheet, "ThemeBackground"));
        Assert.False(sheet.Contains("ThemeHeaderText"));
    }
}
