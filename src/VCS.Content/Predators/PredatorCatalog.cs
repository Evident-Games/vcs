using System.Text.Json.Serialization;
using VCS.Content.Advantages;
using VCS.Content.Skills;
using VCS.Sheet;

namespace VCS.Content.Predators;

public sealed record PredatorSpecialization(string SkillId, string Text, bool IsFreeText = false)
{
    /// <summary>Name of the skill, as shown.</summary>
    [JsonIgnore] public string Skill => SkillCatalog.BuiltIn.FirstOrDefault(x => x.Id == SkillId)?.Name ?? SkillId;
    [JsonIgnore] public string Label => $"{Skill} ({Text})";
}

/// <summary>A predator grant; <paramref name="AdvantageId"/> is the catalog entry it is, when it is one, and sets its points.</summary>
public sealed record PredatorAllocation(string Name, TraitCategory Category, string? AdvantageId = null)
{
    /// <summary>Points this grant can take: the catalog entry's range, or 1 to 5 for a grant outside the catalog.</summary>
    [JsonIgnore] public (int Minimum, int Maximum) Points =>
        AdvantageCatalog.BuiltIn.FirstOrDefault(x => x.Id == AdvantageId) is { } entry ? (entry.Minimum, entry.Maximum) : (1, 5);
}

/// <param name="Disciplines">Ids of the disciplines the predator can raise.</param>
public sealed record PredatorDefinition(string Id, string Name, string Description, string Benefits, string Cost,
    string[] Disciplines, PredatorSpecialization[] Specializations, PredatorAllocation[] FixedGrants,
    int[] FixedPoints, PredatorAllocation[] AllocationOptions, int AllocationPoints,
    PredatorAllocation[] FlawOptions, int FlawPoints, bool SplitFlaws = false, int HumanityDelta = 0, int BloodDelta = 0);

public static class PredatorCatalog
{
    public static IReadOnlyList<PredatorDefinition> All { get; } = BuiltInContent.Load<PredatorDefinition>("predators.json");

    public static PredatorDefinition? Find(string id) => All.FirstOrDefault(x => x.Id == id);
}
