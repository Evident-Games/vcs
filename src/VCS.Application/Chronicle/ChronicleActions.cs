using VCS.Rules.V5.Blood;
using VCS.Rules.V5.Conflicts;
using VCS.Rules.V5.Dice;

namespace VCS.Application.Chronicle;

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

    public ConflictOutcome ResolveConflict(ConflictKind kind, DicePoolResult first, int firstDamageModifier,
        DicePoolResult second, int secondDamageModifier, ConflictDamageType damageType,
        bool firstTargetIsVampire = false, bool secondTargetIsVampire = false) =>
        ConflictRules.ResolveOpposed(kind, new(first, firstDamageModifier), new(second, secondDamageModifier),
            damageType, firstTargetIsVampire, secondTargetIsVampire);

    public int WeaponDamage(int margin, int weaponModifier) => ConflictRules.WeaponDamage(margin, weaponModifier);
}
