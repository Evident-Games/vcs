using System.ComponentModel.DataAnnotations.Schema;

namespace VCS.Content.Skills;

public sealed record SkillDefinition(string Id, string Name, string Group, string Description)
{
    [NotMapped] public string Source { get; init; } = "";
}
