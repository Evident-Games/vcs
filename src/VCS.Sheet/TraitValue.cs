namespace VCS.Sheet;

public enum TraitCategory { Attribute, Skill, Discipline, Advantage, Flaw, DisciplinePower, Loresheet }

public sealed record TraitValue(TraitCategory Category, string Key, string Name, int Rating, string Specialization = "",
    LoresheetInfo? Loresheet = null, MeritInfo? Merit = null, SkillInfo? Skill = null, PowerInfo? Power = null);
