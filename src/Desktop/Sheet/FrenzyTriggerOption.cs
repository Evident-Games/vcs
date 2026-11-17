using VCS.Rules.V5.Blood;

namespace VCS.Desktop.Sheet;

public sealed record FrenzyTriggerOption(FrenzyTrigger Trigger, string Label)
{
    public static IReadOnlyList<FrenzyTriggerOption> All { get; } =
    [
        new(FrenzyTrigger.OpenWoundOrStrongBloodSmell, "Ferida aberta / cheiro forte de sangue"),
        new(FrenzyTrigger.BloodCostAtHungerFive, "Custo de sangue com Fome 5"),
        new(FrenzyTrigger.FailedRouseAtHungerFive, "Falha em Checagem de Sangue com Fome 5"),
        new(FrenzyTrigger.DeadFriend, "Amigo morto"),
        new(FrenzyTrigger.PillarOrLoverInjured, "Pilar ou amante ferido"),
        new(FrenzyTrigger.PillarOrLoverKilled, "Pilar ou amante morto"),
        new(FrenzyTrigger.ProvocationOrPhysicalAssault, "Provocação / agressão física"),
        new(FrenzyTrigger.InsultedByInferior, "Insultado por alguém considerado inferior"),
        new(FrenzyTrigger.PublicHumiliation, "Humilhação pública"),
        new(FrenzyTrigger.Bonfire, "Fogueira"),
        new(FrenzyTrigger.InsideBurningBuilding, "Dentro de prédio em chamas"),
        new(FrenzyTrigger.Burned, "Queimado"),
        new(FrenzyTrigger.ObscuredSunlight, "Luz solar obscurecida"),
        new(FrenzyTrigger.FullSunlight, "Exposição total ao sol")
    ];
}
