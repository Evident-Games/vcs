namespace VCS.Content.Advantages;

public static class AdvantageCatalog
{
    /// <summary>Built-in advantages and flaws.</summary>
    public static IReadOnlyList<AdvantageDefinition> BuiltIn { get; } = BuiltInContent.Load<AdvantageDefinition>("advantages.json");

    /// <summary>Built-in loresheet benefits, one entry per level.</summary>
    public static IReadOnlyList<AdvantageDefinition> Loresheets { get; } = BuiltInContent.Load<AdvantageDefinition>("loresheets.json");
}
