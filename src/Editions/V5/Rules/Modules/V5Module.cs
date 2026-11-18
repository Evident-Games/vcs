using Game.Rules.Modules;

namespace Game.Rules.V5.Modules;

/// <summary>The installed V5 base module; optional modules require explicit compatibility.</summary>
public static class V5Module
{
    public const string EditionId = "V5";
    public static RuleModuleManifest Manifest { get; } = new(
        "v5-core", "1.0", RuleModuleKind.Edition, EditionId,
        Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
}
