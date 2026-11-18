using Game.Rules.V5.Blood;
using Game.Sheet;

namespace Game.Application.Humanity;

/// <summary>Coordinates V5 Humanity actions independently of any presentation framework.</summary>
public sealed class HumanityActions
{
    public IReadOnlyList<HumanityLevel> Levels => HumanityRules.Levels;
    public HumanityLevel? At(int humanity) => humanity is >= 0 and <= 10 ? HumanityRules.At(humanity) : null;

    public CharacterDetails ApplyStainDamage(CharacterDetails details, IReadOnlyList<TraitValue> traits) =>
        HumanityRules.ApplyStainDamage(details, traits);

    public RemorseResult RollRemorse(CharacterDetails details, Random? random = null, int poolModifier = 0) =>
        HumanityRules.RollRemorse(details, random, poolModifier);

    public CharacterDetails FailRemorse(CharacterDetails details) =>
        HumanityRules.FailRemorse(details);
}
