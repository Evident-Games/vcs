namespace Game.Sheet;

/// <summary>Saved resolution of a power, including the Storyteller's explicit authorization.</summary>
public sealed record PowerEffectEntry(Guid Id, string PowerId, string Name, string Target, bool IsSelf,
    DateTime CreatedUtc, string Effect, string Duration, string Source, string Ruling,
    bool Active, int TurnsRemaining = 0, int HealthDamage = 0, bool Aggravated = false,
    int WillpowerDamage = 0, int HungerIncrease = 0, int PotencyIncrease = 0,
    bool ConsequencesApplied = false, int RemainingPoints = 0, int ProtectedSuperficial = 0, int? FormStrength = null, int? FormDexterity = null, int? FormStamina = null, DateTime? GameDate = null, bool TurnChargeUsed = false, int PoolBonus = 0,
    RitualPreparationStep? PreparationStep = null, RitualWorkflowState? RitualWorkflow = null);

/// <summary>A paid preparation check, with a table-supplied date and edition-specific sequence.</summary>
public sealed record RitualPreparationStep(int Circle, int Sequence, DateOnly NightDate);

/// <summary>Saved ritual workflow; stage names are interpreted only by the corresponding edition.</summary>
public sealed record RitualWorkflowState(int ChecksPaid = 0, string Stage = "preparing", bool Resolved = false,
    bool LongTermCircle = false, int StoredSuccesses = 0, bool StoredCritical = false,
    int Charges = 0, string LastIntruder = "", int LastCasterSuccesses = 0, bool LastCasterCritical = false,
    bool LastIntruderAffected = false, IReadOnlyList<RitualCircleIntruder>? Intruders = null, int Executions = 0,
    IReadOnlyList<DateOnly>? PreparationNights = null, int NightsRemaining = 0, DateOnly? LastNightAdvanced = null);

/// <summary>A circle keeps the original casting result for each identified intruder.</summary>
public sealed record RitualCircleIntruder(string Key, int CasterSuccesses, bool CasterCritical,
    bool Affected, bool Entered);
