using System.Windows;
using System.Windows.Media;
using VCS.Desktop.Themes;

namespace VCS.Tests;

public sealed class ThemePaletteTests
{
    private static SolidColorBrush Brush(byte value) => new(Color.FromRgb(value, value, value));

    [Fact]
    public void Application_block_lists_only_differences_and_inherits_the_rest_from_the_sheet_block()
    {
        var file = new ResourceDictionary
        {
            ["VcsSheetBackground"] = Brush(1), ["VcsSheetCard"] = Brush(2), ["VcsSheetOuterText"] = Brush(3),
            ["VcsSheetInputText"] = Brush(4),
            ["VcsAppBackground"] = Brush(10), ["VcsAppOuterText"] = Brush(11), ["VcsAppHeaderText"] = Brush(12)
        };

        var app = ThemeService.Load(file, ThemePart.Application);
        var sheet = ThemeService.Load(file, ThemePart.Sheet);

        Color Of(ResourceDictionary palette, string key) => ((SolidColorBrush)palette[key]).Color;
        Assert.Equal(Color.FromRgb(10, 10, 10), Of(app, "VcsBackground"));
        Assert.Equal(Color.FromRgb(2, 2, 2), Of(app, "VcsCard"));
        Assert.Equal(Color.FromRgb(4, 4, 4), Of(app, "VcsInputText"));
        Assert.Equal(Color.FromRgb(12, 12, 12), Of(app, "VcsHeaderText"));
        Assert.Equal(Color.FromRgb(11, 11, 11), Of(app, "VcsFooterText"));
        Assert.Equal(Color.FromRgb(1, 1, 1), Of(sheet, "VcsBackground"));
        Assert.False(sheet.Contains("VcsHeaderText"));
    }
}
