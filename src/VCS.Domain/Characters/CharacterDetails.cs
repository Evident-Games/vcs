namespace VCS.Domain.Characters;

public sealed record CharacterDetails
{
    public AdvancementState? Advancement { get; init; }
    public bool IsNpc { get; init; }
    public int NpcPhysical { get; init; }
    public int NpcSocial { get; init; }
    public int NpcMental { get; init; }
    public int NpcHealthCapacity { get; init; } = 5;
    public int NpcWillpowerCapacity { get; init; } = 5;
    public string Name { get; init; } = "Novo personagem";
    public string MortalName { get; init; } = "";
    public string Clan { get; init; } = "";
    public int Generation { get; init; } = 13;
    public string Sire { get; init; } = "";
    public string Predator { get; init; } = "";
    public PredatorSelection? PredatorSelection { get; init; }
    public int Stains { get; init; }
    public string Concept { get; init; } = "";
    public string Chronicle { get; init; } = "";
    public string Description { get; init; } = "";
    public string Notes { get; init; } = "";
    public string Ambition { get; init; } = "";
    public string Desire { get; init; } = "";
    public string Resonance { get; init; } = "";
    public string ChronicleTenets { get; init; } = "";
    public string Touchstones { get; init; } = "";
    public string Convictions { get; init; } = "";
    public string ClanBane { get; init; } = "";
    public string TrueAge { get; init; } = "";
    public string ApparentAge { get; init; } = "";
    public string BirthDate { get; init; } = "";
    public string DeathDate { get; init; } = "";
    public string Appearance { get; init; } = "";
    public string DistinguishingFeatures { get; init; } = "";
    public string History { get; init; } = "";
    public string BloodSurge { get; init; } = "";
    public string DamageMend { get; init; } = "";
    public string PowerBonus { get; init; } = "";
    public string RouseReroll { get; init; } = "";
    public string FeedingPenalty { get; init; } = "";
    public string BaneSeverity { get; init; } = "";
    public int ExperienceTotal { get; init; }
    public int ExperienceSpent { get; init; }
    public int CampaignExperienceTotal { get; init; }
    public int CampaignExperienceSpent { get; init; }
    public int Hunger { get; init; } = 1;
    public int Humanity { get; init; } = 7;
    public int BloodPotency { get; init; } = 1;
    public int HealthSuperficial { get; init; }
    public int HealthAggravated { get; init; }
    public int WillpowerSuperficial { get; init; }
    public int WillpowerAggravated { get; init; }
}
