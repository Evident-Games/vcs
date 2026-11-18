using Game.Rules.V5.Advancement;
using Game.Rules.V5.Advantages;
using Game.Sheet;

namespace Game.Application.Advantages;

/// <summary>Use cases for selecting and removing a loresheet benefit.</summary>
public sealed class LoresheetActions
{
    public AdvancementResult Select(CharacterDetails details, IReadOnlyList<TraitValue> traits,
        TraitValue benefit, LoresheetPayment payment) => LoresheetRules.Select(details, traits, benefit, payment);

    public AdvancementResult RemoveInitial(CharacterDetails details, IReadOnlyList<TraitValue> traits, string key) =>
        LoresheetRules.RemoveInitial(details, traits, key);
}
