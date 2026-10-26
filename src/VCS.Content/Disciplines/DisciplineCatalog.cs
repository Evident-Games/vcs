namespace VCS.Content.Disciplines;

public sealed record DisciplineDefinition(string Id, string Name);

public static class DisciplineCatalog
{
    public static IReadOnlyList<DisciplineDefinition> BuiltIn { get; } =
    [
        new("discipline-animalism", "Animalismo"), new("discipline-auspex", "Auspícios"),
        new("discipline-blood-sorcery", "Feitiçaria de Sangue"), new("discipline-celerity", "Celeridade"),
        new("discipline-dominate", "Dominação"), new("discipline-fortitude", "Fortitude"),
        new("discipline-obfuscate", "Ofuscação"), new("discipline-oblivion", "Oblívio"),
        new("discipline-potence", "Potência"), new("discipline-presence", "Presença"),
        new("discipline-protean", "Proteanismo")
    ];

    /// <summary>
    /// Sheet key of a discipline named by the catalog: its built-in id, or a stable custom key for a discipline that
    /// only user content names (a custom clan or power), so the same name always gets the same key.
    /// </summary>
    public static string KeyFor(string name) =>
        BuiltIn.FirstOrDefault(x => CatalogText.Normalize(x.Name) == CatalogText.Normalize(name))?.Id ?? CustomKey(name);

    /// <summary>Custom discipline key: the first half of the name's stable hash.</summary>
    public static string CustomKey(string name) => "discipline-custom-" + CatalogText.StableId("", name)[..32].ToLowerInvariant();
}
