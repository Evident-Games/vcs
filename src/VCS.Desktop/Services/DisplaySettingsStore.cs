using System.IO;
using System.Text.Json;
using VCS.Desktop.ViewModels;

namespace VCS.Desktop.Services;

public sealed class DisplaySettingsStore(string path)
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
    private static readonly Dictionary<string, string> LegacyDefaultTabs = new()
    {
        ["Identity"] = "Identidade e atributos", ["Attributes"] = "Identidade e atributos",
        ["Skills"] = "Habilidades", ["Disciplines"] = "Disciplinas",
        ["Advantages"] = "Vantagens e desvantagens", ["State"] = "Estado e sangue",
        ["Background"] = "Historia e conviccoes", ["Notes"] = "Anotacoes"
    };

    public void Load(DisplaySettingsViewModel settings)
    {
        if (!File.Exists(path)) return;
        var saved = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(path))
            ?? throw new InvalidDataException("O arquivo de preferencias de exibicao esta vazio.");
        if (saved.HiddenSections is null)
            throw new InvalidDataException("O arquivo de preferencias possui secoes desconhecidas.");
        var themeId = saved.ThemeId ?? (saved.DarkTheme is { } dark
            ? dark ? "black-red" : ThemeCatalog.DefaultId
            : ThemeCatalog.DefaultId);
        if (!ThemeCatalog.All.Any(x => x.Id == themeId))
            throw new InvalidDataException("O arquivo de preferencias possui um tema desconhecido.");
        if (saved.SheetThemes is not null && saved.SheetThemes.Values.Any(id => !ThemeCatalog.All.Any(x => x.Id == id)))
            throw new InvalidDataException("O arquivo de preferencias possui um tema de ficha desconhecido.");
        Validate(settings.Pc, saved.HiddenSections, saved.TabNames, saved.Layout);
        if (saved.Npc is { } npc) Validate(settings.Npc, npc.HiddenSections ?? [], null, npc.Layout);

        var legacyDefaultTabs = MatchesLegacyDefaults(saved.TabNames);
        settings.UseTabs = saved.UseTabs;
        settings.UseBento = saved.UseBento && !saved.UseTabs;
        settings.ShowSidebar = saved.ShowSidebar;
        settings.ThemeId = themeId;
        foreach (var (sheet, sheetTheme) in saved.SheetThemes ?? []) settings.SheetThemeIds[sheet] = sheetTheme;
        Apply(settings.Pc, saved.HiddenSections, legacyDefaultTabs ? null : saved.TabNames, saved.Layout);
        if (saved.Npc is { } savedNpc) Apply(settings.Npc, savedNpc.HiddenSections ?? [], null, savedNpc.Layout);
        if (legacyDefaultTabs)
        {
            var orderedTabs = settings.Sections.OrderBy(x => x.SectionOrder)
                .Select(x => x.TabName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var section in settings.Sections)
                section.TabOrder = Array.FindIndex(orderedTabs, name =>
                    string.Equals(name, section.TabName, StringComparison.OrdinalIgnoreCase)) + 1;
        }
    }

    private static void Validate(SheetLayout layout, string[] hidden, Dictionary<string, string>? tabNames,
        Dictionary<string, SectionLayout>? sections)
    {
        if (hidden.Any(x => layout.Find(x) is null))
            throw new InvalidDataException("O arquivo de preferencias possui secoes desconhecidas.");
        if (tabNames is not null && tabNames.Any(x => layout.Find(x.Key) is null || x.Value is null || x.Value.Length > 80))
            throw new InvalidDataException("O arquivo de preferencias possui uma organizacao de abas invalida.");
        if (sections is not null && sections.Any(x => layout.Find(x.Key) is null || x.Value is null ||
                x.Value.Caption is null || x.Value.Caption.Length > 80 || x.Value.TabName is { Length: > 80 } ||
                x.Value.SectionOrder is < 1 or > 99 || x.Value.TabOrder is < 1 or > 99 ||
                x.Value.Span is < 0 or > SheetLayout.BentoColumns || x.Value.Height is < 0 or > 3))
            throw new InvalidDataException("O arquivo de preferencias possui ordens ou titulos invalidos.");
    }

    private static void Apply(SheetLayout layout, string[] hidden, Dictionary<string, string>? tabNames,
        Dictionary<string, SectionLayout>? sections)
    {
        foreach (var section in layout.Sections)
        {
            section.IsVisible = !hidden.Contains(section.Key);
            if (tabNames is not null && tabNames.TryGetValue(section.Key, out var name)) section.TabName = name;
            if (sections is null || !sections.TryGetValue(section.Key, out var stored)) continue;
            section.Caption = stored.Caption;
            section.SectionOrder = stored.SectionOrder;
            section.TabOrder = stored.TabOrder;
            if (stored.TabName is { } tab) section.TabName = tab;
            if (stored.Span > 0) section.Span = stored.Span;
            if (stored.Height > 0) section.Height = stored.Height;
        }
    }

    private static bool MatchesLegacyDefaults(Dictionary<string, string>? tabNames)
    {
        if (tabNames is null || tabNames.Count != LegacyDefaultTabs.Count) return false;
        return LegacyDefaultTabs.All(x => tabNames.TryGetValue(x.Key, out var value) && value == x.Value);
    }

    public void Save(DisplaySettingsViewModel settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(new Preferences(settings.UseTabs,
                Hidden(settings.Pc), settings.ThemeId,
                TabNames: settings.Sections.ToDictionary(x => x.Key, x => x.TabName),
                ShowSidebar: settings.ShowSidebar,
                Layout: Sections(settings.Pc),
                UseBento: settings.UseBento, SheetThemes: settings.SheetThemeIds,
                Npc: new LayoutPreferences(Hidden(settings.Npc), Sections(settings.Npc))), WriteOptions));
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static string[] Hidden(SheetLayout layout) => layout.Sections.Where(x => !x.IsVisible).Select(x => x.Key).ToArray();

    private static Dictionary<string, SectionLayout> Sections(SheetLayout layout) => layout.Sections.ToDictionary(x => x.Key,
        x => new SectionLayout(x.Caption, x.SectionOrder, x.TabOrder, x.TabName, x.Span, x.Height));

    private sealed record Preferences(bool UseTabs, string[] HiddenSections, string? ThemeId = null, bool? DarkTheme = null,
        Dictionary<string, string>? TabNames = null, bool ShowSidebar = true, Dictionary<string, SectionLayout>? Layout = null,
        bool UseBento = false, Dictionary<Guid, string>? SheetThemes = null, LayoutPreferences? Npc = null);
    private sealed record LayoutPreferences(string[]? HiddenSections, Dictionary<string, SectionLayout>? Layout);
    /// <summary>Span and Height are 0 in files saved before bento sizes existed, meaning "keep the default".</summary>
    private sealed record SectionLayout(string Caption, int SectionOrder, int TabOrder, string? TabName = null,
        int Span = 0, int Height = 0);
}
