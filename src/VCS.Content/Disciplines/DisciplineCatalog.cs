namespace VCS.Content.Disciplines;

public sealed record DisciplineDefinition(string Id, string Name);

public static class DisciplineCatalog
{
    public static IReadOnlyList<DisciplineDefinition> BuiltIn { get; } = BuiltInContent.Load<DisciplineDefinition>("disciplines.json");

    /// <summary>Built-in powers and rituals; the database editor can override them by id.</summary>
    public static IReadOnlyList<PowerDefinition> Powers { get; } = BuiltInContent.Load<PowerDefinition>("powers.json");

    /// <summary>
    /// Sheet key of a discipline named by the catalog: its built-in id, or a stable custom key for a discipline that
    /// only user content names (a custom clan or power), so the same name always gets the same key.
    /// </summary>
    public static string KeyFor(string name) =>
        BuiltIn.FirstOrDefault(x => CatalogText.Normalize(x.Name) == CatalogText.Normalize(name))?.Id ?? CustomKey(name);

    /// <summary>Custom discipline key: the first half of the name's stable hash.</summary>
    public static string CustomKey(string name) => "discipline-custom-" + CatalogText.StableId("", name)[..32].ToLowerInvariant();
}
