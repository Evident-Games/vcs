using VCS.Content;

namespace VCS.Rules.V5.Dice;

public sealed record DicePoolComposition(int AttributeDots, int SkillDots, int SpecializationBonus, int SituationalModifier, int DicePool)
{
    public string Formula
    {
        get
        {
            var adjustment = SituationalModifier switch
            {
                > 0 => $"+ {SituationalModifier}",
                < 0 => $"− {Math.Abs(SituationalModifier)}",
                _ => "+ 0"
            };
            return $"{AttributeDots} Atributo + {SkillDots} Habilidade" +
                (SpecializationBonus > 0 ? " + 1 especialização" : "") +
                $" {adjustment} ajuste = {DicePool} dados";
        }
    }
}

/// <summary>Builds an ordinary V5 pool from an Attribute, a Skill and Storyteller-approved modifiers.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base)")]
public static class DicePoolCompositionRules
{
    public static DicePoolComposition Build(int attributeDots, int skillDots, int situationalModifier = 0,
        bool specializationApplies = false)
    {
        if (attributeDots is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(attributeDots), "Atributo deve ficar entre 0 e 5.");
        if (skillDots is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(skillDots), "Habilidade deve ficar entre 0 e 5.");
        if (situationalModifier is < -50 or > 50) throw new ArgumentOutOfRangeException(nameof(situationalModifier), "O ajuste situacional deve ficar entre -50 e 50.");
        if (attributeDots is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(attributeDots), "Atributo deve ficar entre 0 e 5.");
        if (skillDots is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(skillDots), "Habilidade deve ficar entre 0 e 5.");
        if (situationalModifier is < -50 or > 50) throw new ArgumentOutOfRangeException(nameof(situationalModifier), "O ajuste situacional deve ficar entre -50 e 50.");
        var specializationBonus = specializationApplies ? 1 : 0;
        return new(attributeDots, skillDots, specializationBonus, situationalModifier,
            Math.Max(0, attributeDots + skillDots + specializationBonus + situationalModifier));
    }
}
