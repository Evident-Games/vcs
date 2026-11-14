namespace VCS.Rules.V5.Blood;

/// <summary>All six effects in the V5 core book Blood Potency table (page 216).</summary>
public static class BloodPotencyEffects
{
    private static readonly BloodPotencyEffect[] Table =
    [
        new(0, 1, 1, 0, 0, 0, "Nenhum efeito."),
        new(1, 2, 1, 0, 1, 2, "Nenhum efeito."),
        new(2, 2, 2, 1, 1, 2, "Sangue animal ou ensacado sacia metade da Fome."),
        new(3, 3, 2, 1, 2, 3, "Sangue animal ou ensacado nao sacia Fome."),
        new(4, 3, 3, 2, 2, 3, "Sangue animal ou ensacado nao sacia Fome. Cada humano sacia 1 ponto de Fome a menos."),
        new(5, 4, 3, 2, 3, 4, "Sangue animal ou ensacado nao sacia Fome. Cada humano sacia 1 ponto a menos; para reduzir a Fome abaixo de 2, e preciso drenar e matar um humano."),
        new(6, 4, 3, 3, 3, 4, "Sangue animal ou ensacado nao sacia Fome."),
        new(7, 5, 3, 3, 4, 5, "Sangue animal ou ensacado nao sacia Fome. Cada humano sacia 2 pontos a menos; para reduzir a Fome abaixo de 2, e preciso drenar e matar um humano."),
        new(8, 5, 4, 4, 4, 5, "Sangue animal ou ensacado nao sacia Fome."),
        new(9, 6, 4, 4, 5, 6, "Sangue animal ou ensacado nao sacia Fome. Cada humano sacia 2 pontos a menos; para reduzir a Fome abaixo de 3, e preciso drenar e matar um humano."),
        new(10, 6, 5, 5, 5, 6, "Sangue animal ou ensacado nao sacia Fome. Cada humano sacia 3 pontos a menos; para reduzir a Fome abaixo de 3, e preciso drenar e matar um humano.")
    ];

    public static BloodPotencyEffect At(int bloodPotency) => Table[Math.Clamp(bloodPotency, 0, 10)];
    public static string BloodSurge(int bloodPotency) => $"+{At(bloodPotency).BloodSurgeDice} dados";
    public static string DamageMend(int bloodPotency) => $"{At(bloodPotency).MendSuperficial} superficial";
    public static string PowerBonus(int bloodPotency) => At(bloodPotency).DisciplinePowerBonus == 0
        ? "Nenhum" : $"+{At(bloodPotency).DisciplinePowerBonus} dados";
    public static string RouseReroll(int bloodPotency) => At(bloodPotency).RouseRerollLevel == 0
        ? "Nenhuma" : $"Nível {At(bloodPotency).RouseRerollLevel} e abaixo";
    public static string BaneSeverity(int bloodPotency) => At(bloodPotency).BaneSeverity.ToString();
    public static string FeedingPenalty(int bloodPotency) => At(bloodPotency).FeedingPenalty;
}

public sealed record BloodPotencyEffect(
    int Level,
    int BloodSurgeDice,
    int MendSuperficial,
    int DisciplinePowerBonus,
    int RouseRerollLevel,
    int BaneSeverity,
    string FeedingPenalty);
