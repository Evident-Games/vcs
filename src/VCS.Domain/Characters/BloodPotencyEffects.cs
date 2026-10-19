namespace VCS.Domain.Characters;

/// <summary>V5 Blood Potency table: Blood Surge, damage mended per Rouse check and discipline power bonus.</summary>
public static class BloodPotencyEffects
{
    private static readonly (int Surge, int Mend, int Bonus)[] Table =
    [
        (1, 1, 0), (2, 1, 0), (2, 2, 1), (3, 2, 1), (3, 3, 2), (4, 3, 2),
        (4, 3, 3), (5, 3, 3), (5, 4, 4), (6, 4, 4), (6, 5, 5)
    ];

    private static (int Surge, int Mend, int Bonus) At(int bloodPotency) => Table[Math.Clamp(bloodPotency, 0, 10)];
    public static string BloodSurge(int bloodPotency) => $"+{At(bloodPotency).Surge} dados";
    public static string DamageMend(int bloodPotency) => $"{At(bloodPotency).Mend} superficial";
    public static string PowerBonus(int bloodPotency) => At(bloodPotency).Bonus == 0 ? "0" : $"+{At(bloodPotency).Bonus} dados";
}