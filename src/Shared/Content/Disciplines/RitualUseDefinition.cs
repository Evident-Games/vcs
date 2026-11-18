namespace Game.Content.Disciplines;

/// <summary>Core ritual procedure; mechanics are implemented by the V5 rules module.</summary>
public sealed record RitualUseDefinition(string RitualId, int Level, string Kind, int RouseChecks,
    int PreparationMinutes, int PreparationNights, string Attribute, string Trait,
    string Ingredients, string Process, string Trigger, string Source);

public static class RitualUseCatalog
{
    public static IReadOnlyList<RitualUseDefinition> All { get; } = BuiltInContent.Load<RitualUseDefinition>("ritual-uses.json");
    private static readonly IReadOnlyDictionary<string, RitualUseDefinition> ById = All.ToDictionary(x => x.RitualId, StringComparer.Ordinal);
    public static RitualUseDefinition? Find(string id) => ById.GetValueOrDefault(id);
}
