using VCS.Rules.V5.Blood;
using VCS.Sheet;

namespace VCS.Application.Humanity;

/// <summary>Coordinates V5 Humanity actions independently of any presentation framework.</summary>
public sealed class HumanityActions
{
    public CharacterDetails ApplyStainDamage(CharacterDetails details, IReadOnlyList<TraitValue> traits) =>
        HumanityRules.ApplyStainDamage(details, traits);

    public RemorseResult RollRemorse(CharacterDetails details, Random? random = null) =>
        HumanityRules.RollRemorse(details, random);

    public CharacterDetails FailRemorse(CharacterDetails details) =>
        HumanityRules.FailRemorse(details);
}
