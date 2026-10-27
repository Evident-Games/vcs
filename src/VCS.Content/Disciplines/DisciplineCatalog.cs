namespace VCS.Content.Disciplines;

public sealed record DisciplineDefinition(string Id, string Name);

public static class DisciplineCatalog
{
    public const string BloodSorcery = "discipline-blood-sorcery";
    public const string Oblivion = "discipline-oblivion";

    public static IReadOnlyList<DisciplineDefinition> BuiltIn { get; } = BuiltInContent.Load<DisciplineDefinition>("disciplines.json");

    /// <summary>Built-in powers and rituals; the database editor can override them by id.</summary>
    public static IReadOnlyList<PowerDefinition> Powers { get; } = BuiltInContent.Load<PowerDefinition>("powers.json");

    // ponytail: process-wide like ClanCatalog; set at startup and after each edit from the database editor.
    private static IReadOnlyList<DisciplineDefinition>? _configured;

    /// <summary>Disciplines in use: the built-in ones with user edits (same id), then the user's own.</summary>
    public static IReadOnlyList<DisciplineDefinition> All => _configured ?? BuiltIn;

    public static void Configure(IEnumerable<DisciplineDefinition> custom) => _configured = Merge(custom);

    public static IReadOnlyList<DisciplineDefinition> Merge(IEnumerable<DisciplineDefinition> custom)
    {
        var edits = custom.ToArray();
        return Array.AsReadOnly([.. BuiltIn.Select(x => edits.FirstOrDefault(edit => edit.Id == x.Id) ?? x),
            .. edits.Where(edit => !IsBuiltIn(edit.Id))]);
    }

    public static bool IsBuiltIn(string id) => BuiltIn.Any(x => x.Id == id);

    /// <summary>Name shown for a discipline id; an id the catalog does not know is shown as is.</summary>
    public static string NameOf(string id) => All.FirstOrDefault(x => x.Id == id)?.Name ?? id;

    public static DisciplineDefinition? Find(string name) => Find(All, name);

    public static DisciplineDefinition? Find(IEnumerable<DisciplineDefinition> disciplines, string name) =>
        disciplines.FirstOrDefault(x => CatalogText.Normalize(x.Name) == CatalogText.Normalize(name));

    /// <summary>
    /// Sheet key of a discipline named on a sheet: its catalog id, or a stable custom key for a name the catalog does
    /// not have, so the same name always gets the same key.
    /// </summary>
    public static string KeyFor(string name) => Find(name)?.Id ?? CustomKey(name);

    /// <summary>Custom discipline key: the first half of the name's stable hash.</summary>
    public static string CustomKey(string name) => "discipline-custom-" + CatalogText.StableId("", name)[..32].ToLowerInvariant();
}
