namespace VCS.Content.Disciplines;

/// <summary>Explicit core-book activation data, independent of localized display text.</summary>
public sealed record PowerUseDefinition(string PowerId, string Attribute, string Trait, string Mode,
    int Difficulty, int RouseChecks, string Operation, string Duration, string Conditions, string Source, string Defense, int Level);

public static class PowerUseCatalog
{
    public static IReadOnlyList<PowerUseDefinition> All { get; } = BuiltInContent.Load<PowerUseDefinition>("power-uses.json");
    private static readonly IReadOnlyDictionary<string, PowerUseDefinition> ById = All.ToDictionary(x => x.PowerId, StringComparer.Ordinal);
    public static PowerUseDefinition? Find(string id) => ById.GetValueOrDefault(id);
}
