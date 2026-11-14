namespace VCS.Rules.V5.Blood;
using VCS.Content;

public enum BloodResonance
{
    Choleric,
    Melancholic,
    Phlegmatic,
    Sanguine,
    Animal
}

public enum ResonanceIntensity
{
    Balanced,
    Fleeting,
    Intense,
    Acute,
    Dyscrasia
}

public sealed record ResonanceResult(
    BloodResonance Resonance,
    ResonanceIntensity Intensity,
    IReadOnlyList<string> Disciplines,
    int BonusDice,
    bool CanProduceDyscrasia);

[RuleSource("Vampiro: A Máscara V5 (Livro base)")]
public static class ResonanceRules
{
    private static readonly IReadOnlyDictionary<BloodResonance, string[]> DisciplineMap =
        new Dictionary<BloodResonance, string[]>
        {
            [BloodResonance.Choleric] = ["Celeridade", "Potência"],
            [BloodResonance.Melancholic] = ["Fortitude", "Ofuscação"],
            [BloodResonance.Phlegmatic] = ["Auspícios", "Dominação"],
            [BloodResonance.Sanguine] = ["Feitiçaria de Sangue", "Presença"],
            [BloodResonance.Animal] = ["Animalismo", "Proteanismo"]
        };

    public static IReadOnlyList<string> DisciplinesFor(BloodResonance resonance) => DisciplineMap[resonance];

    public static BloodResonance FromRandomResonanceDie(int die) => die switch
    {
        >= 1 and <= 3 => BloodResonance.Phlegmatic,
        >= 4 and <= 6 => BloodResonance.Melancholic,
        >= 7 and <= 8 => BloodResonance.Choleric,
        >= 9 and <= 10 => BloodResonance.Sanguine,
        _ => throw new ArgumentOutOfRangeException(nameof(die), "O dado de Ressonância deve ficar entre 1 e 10.")
    };

    public static ResonanceIntensity FromTemperamentDice(int firstDie, int? secondDie = null)
    {
        if (firstDie is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(firstDie));
        if (firstDie <= 5) return ResonanceIntensity.Balanced;
        if (firstDie <= 8) return ResonanceIntensity.Fleeting;
        if (secondDie is null)
            throw new ArgumentException("Temperamento intenso exige uma segunda rolagem para verificar se é agudo.", nameof(secondDie));
        if (secondDie is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(secondDie));
        return secondDie >= 9 ? ResonanceIntensity.Acute : ResonanceIntensity.Intense;
    }

    public static int BonusDice(ResonanceIntensity intensity) =>
        intensity is ResonanceIntensity.Intense or ResonanceIntensity.Acute or ResonanceIntensity.Dyscrasia ? 1 : 0;

    public static bool CanProduceDyscrasia(bool isHumanBlood) => isHumanBlood;

    public static bool ResonanceBonusExpires(int hunger, bool fedAgain)
    {
        if (hunger is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(hunger));
        return fedAgain || hunger == 0;
    }

    public static bool DyscrasiaExpires(int hunger, bool fedAgain)
    {
        if (hunger is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(hunger));
        return fedAgain || hunger == 5;
    }

    public static ResonanceResult Describe(BloodResonance resonance, ResonanceIntensity intensity, bool isHumanBlood) =>
        new(resonance, intensity, DisciplinesFor(resonance), BonusDice(intensity), CanProduceDyscrasia(isHumanBlood));
}
