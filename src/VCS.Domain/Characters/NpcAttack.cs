namespace VCS.Domain.Characters;

public sealed record NpcAttack(string Name = "", string DicePool = "", string DamageBonus = "",
    string DamageType = "", string ActiveDisciplines = "", string Notes = "");
