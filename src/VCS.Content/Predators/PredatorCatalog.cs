using System.Text.Json.Serialization;
using VCS.Sheet;

namespace VCS.Content.Predators;

public sealed record PredatorSpecialization(string Skill, string Text, bool IsFreeText = false)
{
    [JsonIgnore] public string Label => $"{Skill} ({Text})";
}
public sealed record PredatorAllocation(string Name, TraitCategory Category);
public sealed record PredatorDefinition(string Id, string Name, string Description, string Benefits, string Cost,
    string[] Disciplines, PredatorSpecialization[] Specializations, PredatorAllocation[] FixedGrants,
    int[] FixedPoints, PredatorAllocation[] AllocationOptions, int AllocationPoints,
    PredatorAllocation[] FlawOptions, int FlawPoints, bool SplitFlaws = false, int HumanityDelta = 0, int BloodDelta = 0);

public static class PredatorCatalog
{
    public static IReadOnlyList<PredatorDefinition> All { get; } = BuiltInContent.Load<PredatorDefinition>("predators.json");

    public static PredatorDefinition? Find(string id) => All.FirstOrDefault(x => x.Id == id);
}
