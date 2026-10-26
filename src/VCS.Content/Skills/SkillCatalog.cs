namespace VCS.Content.Skills;

public static class SkillCatalog
{
    public static IReadOnlyList<SkillDefinition> BuiltIn { get; } = BuiltInContent.Load<SkillDefinition>("skills.json");
}
