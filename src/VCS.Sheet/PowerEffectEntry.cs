namespace VCS.Sheet;

/// <summary>Saved resolution of a power, including the Storyteller's explicit authorization.</summary>
public sealed record PowerEffectEntry(Guid Id, string PowerId, string Name, string Target, bool IsSelf,
    DateTime CreatedUtc, string Effect, string Duration, string Source, string Ruling,
    bool Active, int TurnsRemaining = 0, int HealthDamage = 0, bool Aggravated = false,
    int WillpowerDamage = 0, int HungerIncrease = 0, int PotencyIncrease = 0,
    bool ConsequencesApplied = false, int RemainingPoints = 0, int ProtectedSuperficial = 0, int? FormStrength = null, int? FormDexterity = null, int? FormStamina = null, DateTime? GameDate = null, bool TurnChargeUsed = false, int PoolBonus = 0);
