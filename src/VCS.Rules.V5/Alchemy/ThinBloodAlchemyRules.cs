using VCS.Content;

namespace VCS.Rules.V5.Alchemy;

public enum DistillationMethod { AthanorCorporis, Calcinatio, Fixatio }
public sealed record DistillationResult(int Pool, int FormulaCost, string Effect);

[RuleSource("Vampiro: A Máscara V5 (Livro base), pp. 282–284")]
public static class ThinBloodAlchemyRules
{
    public static int Pool(int attribute, int alchemy, int ingredientModifier, bool improvisedFixatio)
    {
        if (attribute is < 0 or > 5 || alchemy is < 1 or > 5 || ingredientModifier is < -20 or > 20)
            throw new ArgumentOutOfRangeException(nameof(alchemy));
        return Math.Max(0, attribute + alchemy + ingredientModifier - (improvisedFixatio ? 2 : 0));
    }

    public static DistillationResult Resolve(int attribute, int alchemy, int formulaLevel, int ingredientModifier,
        bool improvisedFixatio, int successes)
    {
        if (formulaLevel is < 1 or > 5 || successes < 0) throw new ArgumentOutOfRangeException(nameof(formulaLevel));
        return new(Pool(attribute, alchemy, ingredientModifier, improvisedFixatio), checked(formulaLevel * 3), successes switch
        {
            >= 6 => "Efeito aumentado; confirme a ampliação específica com o Narrador.",
            >= 4 => "A fórmula funciona como pretendido.",
            >= 2 => "Poder fraco, imprevisto ou atrasado; o Narrador define a consequência.",
            _ => "Destilação sem efeito utilizável; não há ativação bem-sucedida."
        });
    }

    public static string Attribute(DistillationMethod method) => method switch
    {
        DistillationMethod.AthanorCorporis => "Vigor",
        DistillationMethod.Calcinatio => "Manipulação",
        DistillationMethod.Fixatio => "Inteligência",
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };
}
