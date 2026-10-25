using VCS.Domain.Advancement;
using VCS.Domain.Predators;

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
    private readonly IReadOnlyList<NpcAttack> _npcAttacks = ValueList<NpcAttack>.Empty;
    public IReadOnlyList<NpcAttack> NpcAttacks { get => _npcAttacks; init => _npcAttacks = value is null ? null! : new ValueList<NpcAttack>(value); }
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
    /// <summary>Single free-text notes; NPC sheets use it, PC sheets moved to <see cref="NoteEntries"/>.</summary>
    public string Notes { get; init; } = "";
    private readonly IReadOnlyList<CharacterNote> _noteEntries = ValueList<CharacterNote>.Empty;
    public IReadOnlyList<CharacterNote> NoteEntries { get => _noteEntries; init => _noteEntries = value is null ? null! : new ValueList<CharacterNote>(value); }
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
