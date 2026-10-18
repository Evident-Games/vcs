using VCS.Desktop.Services;
using VCS.Desktop.ViewModels;

namespace VCS.Tests;

public sealed class DisplaySettingsTests
{
    [Theory]
    [InlineData("black-white")]
    [InlineData("white-black")]
    [InlineData("white-red")]
    [InlineData("black-red")]
    [InlineData("yellow-red")]
    public void Display_preferences_roundtrip_without_affecting_character_data(string themeId)
    {
        var directory = Path.Combine(Path.GetTempPath(), "VCS-settings-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "settings.json");
        try
        {
            var store = new DisplaySettingsStore(path);
            var display = new DisplaySettingsViewModel();
            store.Load(display);
            Assert.False(display.UseTabs);
            Assert.Equal("white-red", display.ThemeId);
            Assert.Equal("white-red", Assert.Single(display.Themes, x => x.IsSelected).Id);
            Assert.All(display.Sections, x => Assert.True(x.IsVisible));
            display.UseTabs = true;
            display.ShowSidebar = false;
            display.SelectThemeCommand.Execute(themeId);
            display.Sections.Single(x => x.Key == "Identity").TabName = "Minha ficha";
            display.Sections.Single(x => x.Key == "State").TabName = "Minha ficha";
            display.Sections.Single(x => x.Key == "State").Caption = "Estado de sangue";
            display.Sections.Single(x => x.Key == "State").SectionOrder = 1;
            display.Sections.Single(x => x.Key == "State").TabOrder = 2;
            display.Sections.Single(x => x.Key == "Attributes").IsVisible = false;
            display.Sections.Single(x => x.Key == "Notes").IsVisible = false;
            store.Save(display);
            var reloaded = new DisplaySettingsViewModel();
            store.Load(reloaded);
            Assert.True(reloaded.UseTabs);
            Assert.False(reloaded.ShowSidebar);
            Assert.Equal("Estado de sangue", reloaded.Sections.Single(x => x.Key == "State").Caption);
            Assert.Equal(1, reloaded.Sections.Single(x => x.Key == "State").SectionOrder);
            Assert.Equal(2, reloaded.Sections.Single(x => x.Key == "State").TabOrder);
            Assert.Equal(themeId, reloaded.ThemeId);
            Assert.Equal("Minha ficha", reloaded.Sections.Single(x => x.Key == "Identity").TabName);
            Assert.Equal("Minha ficha", reloaded.Sections.Single(x => x.Key == "State").TabName);
            Assert.Equal(themeId, Assert.Single(reloaded.Themes, x => x.IsSelected).Id);
            Assert.DoesNotContain("DarkTheme", File.ReadAllText(path));
            Assert.Equal(new[] { "Attributes", "Notes" }, reloaded.Sections.Where(x => !x.IsVisible).Select(x => x.Key));
            Assert.Single(Directory.GetFiles(directory));
        }

        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData(true, "black-red")]
    [InlineData(false, "white-red")]
    public void Previous_dark_theme_preference_is_migrated(bool dark, string expectedId)
    {
        var path = Path.Combine(Path.GetTempPath(), "VCS-settings-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, $$"""{"UseTabs":true,"DarkTheme":{{dark.ToString().ToLowerInvariant()}},"HiddenSections":["Notes"]}""");
            var display = new DisplaySettingsViewModel();
            new DisplaySettingsStore(path).Load(display);
            Assert.Equal(expectedId, display.ThemeId);
            Assert.True(display.UseTabs);
            Assert.False(display.Sections.Single(x => x.Key == "Notes").IsVisible);
            Assert.Equal(display.Sections.Single(x => x.Key == "Identity").TabName,
                display.Sections.Single(x => x.Key == "Attributes").TabName);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Unknown_theme_is_rejected_before_changing_preferences()
    {
        var path = Path.Combine(Path.GetTempPath(), "VCS-settings-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, """{"UseTabs":true,"ThemeId":"unknown","HiddenSections":[]}""");
            var display = new DisplaySettingsViewModel();
            Assert.Throws<InvalidDataException>(() => new DisplaySettingsStore(path).Load(display));
            Assert.False(display.UseTabs);
            Assert.Equal(ThemeCatalog.DefaultId, display.ThemeId);
            Assert.Throws<ArgumentException>(() => display.ThemeId = "unknown");
            Assert.Equal(ThemeCatalog.DefaultId, display.ThemeId);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("""{"Unknown":"Aba"}""")]
    [InlineData("""{"Identity":null}""")]
    [InlineData("""{"Identity":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}""")]
    public void Invalid_tab_configuration_is_reported_without_mutating_settings(string tabNames)
    {
        var path = Path.Combine(Path.GetTempPath(), "VCS-settings-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, $$"""{"UseTabs":true,"HiddenSections":[],"TabNames":{{tabNames}}}""");
            var display = new DisplaySettingsViewModel();
            Assert.Throws<InvalidDataException>(() => new DisplaySettingsStore(path).Load(display));
            Assert.False(display.UseTabs);
            Assert.Equal("Identidade e atributos", display.Sections[0].TabName);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("""{"State":{"Caption":"Sangue","SectionOrder":0,"TabOrder":1}}""")]
    [InlineData("""{"State":{"Caption":"Sangue","SectionOrder":1,"TabOrder":100}}""")]
    [InlineData("""{"Unknown":{"Caption":"Sangue","SectionOrder":1,"TabOrder":1}}""")]
    public void Invalid_layout_orders_are_reported_without_mutating_preferences(string layout)
    {
        var path = Path.Combine(Path.GetTempPath(), "VCS-settings-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, $$"""{"UseTabs":true,"HiddenSections":[],"Layout":{{layout}}}""");
            var display = new DisplaySettingsViewModel();
            Assert.Throws<InvalidDataException>(() => new DisplaySettingsStore(path).Load(display));
            Assert.False(display.UseTabs);
            Assert.True(display.ShowSidebar);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Corrupt_settings_are_reported_not_silently_replaced()
    {
        var path = Path.Combine(Path.GetTempPath(), "VCS-settings-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, """{"UseTabs":true,"DarkTheme":true,"HiddenSections":["Unknown"]}""");
            Assert.Throws<InvalidDataException>(() => new DisplaySettingsStore(path).Load(new()));
            Assert.Contains("Unknown", File.ReadAllText(path));
        }
        finally { File.Delete(path); }
    }
}
