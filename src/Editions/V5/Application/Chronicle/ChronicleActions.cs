using Game.Rules.V5.Blood;
using Game.Rules.V5.Conflicts;
using Game.Rules.V5.Dice;

namespace Game.Application.Chronicle;

/// <summary>Application use cases for chronicle systems that operate across character sheets.</summary>
public sealed class ChronicleActions
{
    public BloodBondDrinkResult RecordVitae(BloodBondState bond, DateOnly date) =>
        BloodBondRules.RecordDrink(bond, date);

    public int BloodBondLevel(BloodBondState bond, DateOnly date) => BloodBondRules.LevelAt(bond, date);

    public TorporDuration TorporForHumanity(int humanity) => TorporRules.ForHumanity(humanity);

    public DateTimeOffset NaturalTorporWakeTime(int humanity, DateTimeOffset enteredAt) =>
        TorporRules.NaturalWakeTime(humanity, enteredAt);

    public bool CanWakeNaturally(int humanity, DateTimeOffset enteredAt, DateTimeOffset now, bool staked) =>
        TorporRules.CanWakeNaturally(humanity, enteredAt, now, staked);

    public bool CanWakeWithVitae(int vampireBloodPotency, int vitaeBloodPotency, int hungerSlaked, bool staked = false) =>
        TorporRules.CanWakeWithVitae(vampireBloodPotency, vitaeBloodPotency, hungerSlaked, staked);

    public ConflictExchange ResolveConflict(ConflictKind kind, int firstSuccesses, int firstDamageModifier,
        int secondSuccesses, int secondDamageModifier, ConflictDamageType damageType,
        bool firstTargetIsVampire = false, bool secondTargetIsVampire = false) =>
        ConflictRules.ResolveOpposed(kind, firstSuccesses, secondSuccesses, firstDamageModifier,
            secondDamageModifier, damageType, firstTargetIsVampire, secondTargetIsVampire);

    public ConflictOutcome ResolveConflict(ConflictKind kind, DicePoolResult first, int firstDamageModifier,
        DicePoolResult second, int secondDamageModifier, ConflictDamageType damageType,
        bool firstTargetIsVampire = false, bool secondTargetIsVampire = false) =>
        ConflictRules.ResolveOpposed(kind, new(first, firstDamageModifier), new(second, secondDamageModifier),
            damageType, firstTargetIsVampire, secondTargetIsVampire);

    public DamageTrackResult ApplyDamage(int capacity, int superficial, int aggravated, int damage, ConflictDamageType type) =>
        DamageTrackRules.Apply(capacity, superficial, aggravated, damage, type);

    public int WeaponDamage(int margin, int weaponModifier) => ConflictRules.WeaponDamage(margin, weaponModifier);
}
