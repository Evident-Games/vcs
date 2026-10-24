using VCS.Desktop.Layout;
using VCS.Desktop.Themes;


namespace VCS.Tests;

public sealed class DisplaySettingsTests
{
    [Fact]
    public void Moving_sections_reorders_list_and_tabs_follow_first_section()
    {
        var display = new DisplaySettingsViewModel();
        var changes = 0;
        display.LayoutChanged += (_, _) => changes++;
        var state = display.Sections.Single(x => x.Key == "State");
        display.MoveSection(state, 0);
        Assert.Same(state, display.OrderedSections[0]);
        Assert.Equal(Enumerable.Range(1, display.Sections.Length), display.OrderedSections.Select(x => x.SectionOrder));
        Assert.Equal(1, state.TabOrder);
        Assert.Equal(2, display.Sections.Single(x => x.Key == "Identity").TabOrder);
        Assert.Equal(1, changes);
        display.MoveSection(state, 99);
        Assert.Same(state, display.OrderedSections[^1]);
        display.MoveSection(state, display.OrderedSections.Count - 1);
        Assert.Equal(2, changes);
        display.Sections.Single(x => x.Key == "Notes").SectionOrder = 0;
        Assert.Equal("Notes", display.OrderedSections[0].Key);
    }

    [Fact]
    public void Npc_layout_and_bento_sizes_roundtrip_separately_from_pc()
    {
        var directory = Path.Combine(Path.GetTempPath(), "VCS-settings-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "settings.json");
        try
        {
            var display = new DisplaySettingsViewModel();
            Assert.Equal(7, display.Npc.Sections.Length);
            var combat = display.Npc.Find("NpcCombat")!;
            display.Npc.MoveSection(combat, 0);
            combat.CycleSpanCommand.Execute(null);
            combat.CycleSpanCommand.Execute(null);
            combat.CycleHeightCommand.Execute(null);
            display.Npc.MoveToTab(display.Npc.Find("NpcHistory")!, "Lore");
            display.Npc.Find("NpcSkills")!.ToggleVisibleCommand.Execute(null);
            display.Pc.Find("Skills")!.Span = 1;
            new DisplaySettingsStore(path).Save(display);

            var reloaded = new DisplaySettingsViewModel();
            new DisplaySettingsStore(path).Load(reloaded);
            Assert.Equal("NpcCombat", reloaded.Npc.OrderedSections[0].Key);
            Assert.Equal((4, 3), (reloaded.Npc.Find("NpcCombat")!.Span, reloaded.Npc.Find("NpcCombat")!.Height));
            Assert.Equal("Lore", reloaded.Npc.Find("NpcHistory")!.TabName);
            Assert.Contains(reloaded.Npc.Tabs, x => x.Name == "Lore");
            Assert.False(reloaded.Npc.Find("NpcSkills")!.IsVisible);
            Assert.Equal(1, reloaded.Pc.Find("Skills")!.Span);
            Assert.All(reloaded.Pc.Sections, x => Assert.True(x.IsVisible));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void Sheet_modes_are_mutually_exclusive_and_bento_roundtrips()
    {
        var directory = Path.Combine(Path.GetTempPath(), "VCS-settings-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "settings.json");
        try
        {
            var display = new DisplaySettingsViewModel();
            Assert.True(display.IsContinuous);
            display.UseTabs = true;
            display.UseBento = true;
            Assert.False(display.UseTabs);
            Assert.False(display.IsContinuous);
            display.UseTabs = true;
            Assert.False(display.UseBento);
            display.UseBento = true;
            var store = new DisplaySettingsStore(path);
            store.Save(display);
            var reloaded = new DisplaySettingsViewModel();
            store.Load(reloaded);
            Assert.True(reloaded.UseBento);
            Assert.False(reloaded.UseTabs);
            reloaded.IsContinuous = true;
            Assert.False(reloaded.UseBento);
            Assert.False(reloaded.UseTabs);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

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
            Assert.Equal("Identidade", display.Sections.Single(x => x.Key == "Identity").TabName);
            Assert.Equal("Atributos", display.Sections.Single(x => x.Key == "Attributes").TabName);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Previous_default_tab_grouping_is_replaced_by_subject_tabs()
    {
        var path = Path.Combine(Path.GetTempPath(), "VCS-settings-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, """
                {"UseTabs":true,"HiddenSections":[],"TabNames":{
                  "Identity":"Identidade e atributos","Attributes":"Identidade e atributos",
                  "Skills":"Habilidades","Disciplines":"Disciplinas",
                  "Advantages":"Vantagens e desvantagens","State":"Estado e sangue",
                  "Background":"Historia e conviccoes","Notes":"Anotacoes"
                }}
                """);
            var display = new DisplaySettingsViewModel();
            new DisplaySettingsStore(path).Load(display);
            Assert.Equal("Identidade", display.Sections.Single(x => x.Key == "Identity").TabName);
            Assert.Equal("Atributos", display.Sections.Single(x => x.Key == "Attributes").TabName);
            Assert.Equal("História", display.Sections.Single(x => x.Key == "Background").TabName);
            Assert.Equal("História", display.Sections.Single(x => x.Key == "Notes").TabName);
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
            Assert.Equal("Identidade", display.Sections[0].TabName);
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
