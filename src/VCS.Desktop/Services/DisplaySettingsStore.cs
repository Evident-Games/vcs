using System.IO;
using System.Text.Json;
using VCS.Desktop.ViewModels;

namespace VCS.Desktop.Services;

public sealed class DisplaySettingsStore(string path)
{
    public void Load(DisplaySettingsViewModel settings)
    {
        if (!File.Exists(path)) return;
        var saved = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(path))
            ?? throw new InvalidDataException("O arquivo de preferencias de exibicao esta vazio.");
        if (saved.HiddenSections is null || saved.HiddenSections.Any(x => !settings.Sections.Any(s => s.Key == x)))
            throw new InvalidDataException("O arquivo de preferencias possui secoes desconhecidas.");
        var themeId = saved.ThemeId ?? (saved.DarkTheme is { } dark
            ? dark ? "black-red" : ThemeCatalog.DefaultId
            : ThemeCatalog.DefaultId);
        if (!ThemeCatalog.All.Any(x => x.Id == themeId))
            throw new InvalidDataException("O arquivo de preferencias possui um tema desconhecido.");
        if (saved.TabNames is not null && saved.TabNames.Any(x =>
            !settings.Sections.Any(s => s.Key == x.Key) || x.Value is null || x.Value.Length > 80))
            throw new InvalidDataException("O arquivo de preferencias possui uma organizacao de abas invalida.");
        if (saved.Layout is not null && saved.Layout.Any(x => !settings.Sections.Any(s => s.Key == x.Key) ||
            x.Value is null || x.Value.Caption is null || x.Value.Caption.Length > 80 ||
            x.Value.SectionOrder is < 1 or > 99 || x.Value.TabOrder is < 1 or > 99))
            throw new InvalidDataException("O arquivo de preferencias possui ordens ou titulos invalidos.");
        var legacyDefaultTabs = MatchesLegacyDefaults(saved.TabNames);
        settings.UseTabs = saved.UseTabs;
        settings.UseBento = saved.UseBento && !saved.UseTabs;
        settings.ShowSidebar = saved.ShowSidebar;
        settings.ThemeId = themeId;
        foreach (var section in settings.Sections) section.IsVisible = !saved.HiddenSections.Contains(section.Key);
        if (saved.TabNames is not null && !legacyDefaultTabs)
            foreach (var section in settings.Sections)
                if (saved.TabNames.TryGetValue(section.Key, out var name)) section.TabName = name;
        if (saved.Layout is not null)
            foreach (var section in settings.Sections)
                if (saved.Layout.TryGetValue(section.Key, out var layout))
                {
                    section.Caption = layout.Caption;
                    section.SectionOrder = layout.SectionOrder;
                    section.TabOrder = layout.TabOrder;
                }
        if (legacyDefaultTabs)
        {
            var orderedTabs = settings.Sections.OrderBy(x => x.SectionOrder)
                .Select(x => x.TabName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var section in settings.Sections)
                section.TabOrder = Array.FindIndex(orderedTabs, name =>
                    string.Equals(name, section.TabName, StringComparison.OrdinalIgnoreCase)) + 1;
        }
    }

    private static bool MatchesLegacyDefaults(Dictionary<string, string>? tabNames)
    {
        if (tabNames is null || tabNames.Count != 8) return false;
        var legacyDefaults = new Dictionary<string, string>
        {
            ["Identity"] = "Identidade e atributos", ["Attributes"] = "Identidade e atributos",
            ["Skills"] = "Habilidades", ["Disciplines"] = "Disciplinas",
            ["Advantages"] = "Vantagens e desvantagens", ["State"] = "Estado e sangue",
            ["Background"] = "Historia e conviccoes", ["Notes"] = "Anotacoes"
        };
        return legacyDefaults.All(x => tabNames.TryGetValue(x.Key, out var value) && value == x.Value);
    }

    public void Save(DisplaySettingsViewModel settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(new Preferences(settings.UseTabs,
                settings.Sections.Where(x => !x.IsVisible).Select(x => x.Key).ToArray(), settings.ThemeId,
                TabNames: settings.Sections.ToDictionary(x => x.Key, x => x.TabName),
                ShowSidebar: settings.ShowSidebar,
                Layout: settings.Sections.ToDictionary(x => x.Key, x => new SectionLayout(x.Caption, x.SectionOrder, x.TabOrder)),
                UseBento: settings.UseBento),
                new JsonSerializerOptions { WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private sealed record Preferences(bool UseTabs, string[] HiddenSections, string? ThemeId = null, bool? DarkTheme = null,
        Dictionary<string, string>? TabNames = null, bool ShowSidebar = true, Dictionary<string, SectionLayout>? Layout = null,
        bool UseBento = false);
    private sealed record SectionLayout(string Caption, int SectionOrder, int TabOrder);
}
