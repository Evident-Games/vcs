namespace VCS.Rules.V5.Blood;
using VCS.Content;

/// <summary>Blood Potency limits by Generation from the V5 core book, page 214.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base)")]
public static class BloodPotencyGenerationRules
{
    public static BloodPotencyRange? ForGeneration(int generation) => generation switch
    {
        4 => new(5, 10),
        5 => new(4, 9),
        6 => new(3, 8),
        7 => new(3, 7),
        8 => new(2, 6),
        9 => new(2, 5),
        10 or 11 => new(1, 4),
        12 or 13 => new(1, 3),
        14 or 15 or 16 => new(0, 0),
        _ => null
    };

    public static bool IsThinBloodGeneration(int generation) => generation is >= 14 and <= 16;
}

public sealed record BloodPotencyRange(int Minimum, int Maximum);
