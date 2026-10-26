namespace VCS.Sheet;

/// <summary>Trait keys start with their kind in English: skill-melee, discipline-celerity, advantage-personal-…</summary>
public static class TraitKeys
{
    public static string Kind(TraitCategory category, bool isRitual = false) => category switch
    {
        TraitCategory.Attribute => "attribute",
        TraitCategory.Skill => "skill",
        TraitCategory.Discipline => "discipline",
        TraitCategory.Advantage => "advantage",
        TraitCategory.Flaw => "flaw",
        TraitCategory.DisciplinePower => isRitual ? "ritual" : "power",
        TraitCategory.Loresheet => "loresheet",
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };

    /// <summary>Key of a line the player wrote on the sheet, outside the catalog.</summary>
    public static string Personal(TraitCategory category, bool isRitual = false) =>
        $"{Kind(category, isRitual)}-personal-{Guid.NewGuid():N}";
}
