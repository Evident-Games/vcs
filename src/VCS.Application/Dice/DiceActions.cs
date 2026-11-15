using VCS.Rules.V5.Dice;

namespace VCS.Application.Dice;

/// <summary>Application use cases for composing and resolving V5 dice pools.</summary>
public sealed class DiceActions
{
    public DicePoolComposition BuildPool(int attributeDots, int skillDots, int situationalModifier = 0,
        bool specializationApplies = false) =>
        DicePoolCompositionRules.Build(attributeDots, skillDots, situationalModifier, specializationApplies);

    public DicePoolResult Roll(int pool, int hunger, int difficulty, Random? random = null) =>
        DicePoolRules.Roll(pool, hunger, difficulty, random);

    public DicePoolResult RerollFailures(int pool, int hunger, int difficulty,
        IReadOnlyList<int> initialFaces, IReadOnlyList<int> regularDieIndices, Random? random = null)
    {
        random ??= Random.Shared;
        var replacements = regularDieIndices.Select(_ => random.Next(1, 11)).ToArray();
        return DicePoolRules.RerollRegularFailures(pool, hunger, difficulty, initialFaces,
            regularDieIndices, replacements);
    }
}
