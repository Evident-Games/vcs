namespace VCS.Domain.Characters;

public enum TraitCategory { Attribute, Skill, Discipline, Advantage, Flaw, DisciplinePower, Loresheet }

public enum LoresheetPayment { AdvantagePoints, InitialXp, CampaignXp }
public sealed record LoresheetInfo(string Collection, bool IsLineage, string Description, LoresheetPayment Payment);
public sealed record MeritInfo(string Group, string Description);
public sealed record SkillInfo(string Group);
/// <summary>Sheet line of a discipline power or ritual; overrides or replaces the catalog details.</summary>
public sealed record PowerInfo(string Discipline, string Cost = "", string DicePool = "", string Duration = "",
    string Description = "", bool IsRitual = false, string Ingredients = "", string Process = "", string System = "",
    string Prerequisite = "");
public sealed record TraitValue(TraitCategory Category, string Key, string Name, int Rating, string Specialization = "",
    LoresheetInfo? Loresheet = null, MeritInfo? Merit = null, SkillInfo? Skill = null, PowerInfo? Power = null);

public sealed class CharacterTrait
{
    private CharacterTrait() { }

    internal CharacterTrait(TraitValue value)
    {
        Id = Guid.NewGuid();
        Category = value.Category;
        Key = value.Key;
        Name = value.Name.Trim();
        Rating = value.Rating;
        Specialization = value.Specialization;
        Loresheet = value.Loresheet;
        Merit = value.Merit;
        Skill = value.Skill;
        Power = value.Power;
    }

    public Guid Id { get; private set; }
    public TraitCategory Category { get; private set; }
    public string Key { get; private set; } = "";
    public string Name { get; private set; } = "";
    public int Rating { get; private set; }
    public string Specialization { get; private set; } = "";
    public LoresheetInfo? Loresheet { get; private set; }
    public MeritInfo? Merit { get; private set; }
    public SkillInfo? Skill { get; private set; }
    public PowerInfo? Power { get; private set; }

    public TraitValue ToValue() => new(Category, Key, Name, Rating, Specialization, Loresheet, Merit, Skill, Power);
}
