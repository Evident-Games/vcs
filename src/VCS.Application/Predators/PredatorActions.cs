using VCS.Content.Predators;
using VCS.Rules.V5.Advancement;
using VCS.Rules.V5.Predators;
using VCS.Sheet;

namespace VCS.Application.Predators;

/// <summary>Use cases for applying and removing a predator choice during character creation.</summary>
public sealed class PredatorActions
{
    public IReadOnlyList<string> DisciplinesFor(PredatorDefinition definition, string clan) =>
        PredatorRules.DisciplinesFor(definition, clan);

    public AdvancementResult Apply(CharacterDetails details, IReadOnlyList<TraitValue> traits, PredatorChoice choice) =>
        PredatorRules.Apply(details, traits, choice);

    public AdvancementResult Remove(CharacterDetails details, IReadOnlyList<TraitValue> traits) =>
        PredatorRules.Remove(details, traits);
}
