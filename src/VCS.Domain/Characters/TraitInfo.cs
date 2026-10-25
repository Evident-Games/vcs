namespace VCS.Domain.Characters;

public enum LoresheetPayment { AdvantagePoints, InitialXp, CampaignXp }

public sealed record LoresheetInfo(string Collection, bool IsLineage, string Description, LoresheetPayment Payment);

public sealed record MeritInfo(string Group, string Description);

public sealed record SkillInfo(string Group);

/// <summary>Sheet line of a discipline power or ritual; overrides or replaces the catalog details.</summary>
public sealed record PowerInfo(string Discipline, string Cost = "", string DicePool = "", string Duration = "",
    string Description = "", bool IsRitual = false, string Ingredients = "", string Process = "", string System = "",
    string Prerequisite = "");
