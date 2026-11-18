namespace Game.Rules.V5.Blood;
using Game.Content;

public enum FrenzyTrigger
{
    OpenWoundOrStrongBloodSmell,
    BloodCostAtHungerFive,
    FailedRouseAtHungerFive,
    DeadFriend,
    PillarOrLoverInjured,
    PillarOrLoverKilled,
    ProvocationOrPhysicalAssault,
    InsultedByInferior,
    PublicHumiliation,
    Bonfire,
    InsideBurningBuilding,
    Burned,
    ObscuredSunlight,
    FullSunlight
}

public sealed record FrenzyTriggerResult(
    FrenzyTrigger Trigger,
    FrenzyType Type,
    int Difficulty,
    bool Applies,
    string Description);

/// <summary>Core V5 trigger examples; the Storyteller can set another difficulty for the scene.</summary>
[RuleSource("Vampiro: A Máscara V5 (Livro base)")]
public static class FrenzyTriggerRules
{
    public static FrenzyTriggerResult Evaluate(
        FrenzyTrigger trigger,
        int hunger,
        bool isPresent = true,
        bool isPillarOrLover = false)
    {
        if (hunger is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(hunger));

        return trigger switch
        {
            FrenzyTrigger.OpenWoundOrStrongBloodSmell => Make(trigger, FrenzyType.Hunger, 7,
                isPresent && hunger >= 4, "Ferida aberta ou odor forte de sangue com Fome 4 ou maior."),
            FrenzyTrigger.BloodCostAtHungerFive => Make(trigger, FrenzyType.Hunger, 1,
                isPresent && hunger == 5, "Custo de sangue com Fome 5."),
            FrenzyTrigger.FailedRouseAtHungerFive => Make(trigger, FrenzyType.Hunger, 4,
                isPresent && hunger == 5, "Falha em Checagem de Sangue com Fome 5."),
            FrenzyTrigger.DeadFriend => Make(trigger, FrenzyType.Fury, 1, isPresent, "Amigo morto."),
            FrenzyTrigger.PillarOrLoverInjured => Make(trigger, FrenzyType.Fury, 3,
                isPresent && isPillarOrLover, "Pilar ou amante ferido."),
            FrenzyTrigger.PillarOrLoverKilled => Make(trigger, FrenzyType.Fury, 4,
                isPresent && isPillarOrLover, "Pilar ou amante morto."),
            FrenzyTrigger.ProvocationOrPhysicalAssault => Make(trigger, FrenzyType.Fury, 2,
                isPresent, "Provocação, assédio ou agressão física."),
            FrenzyTrigger.InsultedByInferior => Make(trigger, FrenzyType.Fury, 2,
                isPresent, "Insulto por alguém considerado inferior."),
            FrenzyTrigger.PublicHumiliation => Make(trigger, FrenzyType.Fury, 1,
                isPresent, "Humilhação pública."),
            FrenzyTrigger.Bonfire => Make(trigger, FrenzyType.Terror, 2, isPresent, "Fogueira."),
            FrenzyTrigger.InsideBurningBuilding => Make(trigger, FrenzyType.Terror, 1,
                isPresent, "Dentro de uma construção em chamas."),
            FrenzyTrigger.Burned => Make(trigger, FrenzyType.Terror, 2, isPresent, "Ser queimado."),
            FrenzyTrigger.ObscuredSunlight => Make(trigger, FrenzyType.Terror, 3,
                isPresent, "Luz solar obscurecida, como através de uma janela."),
            FrenzyTrigger.FullSunlight => Make(trigger, FrenzyType.Terror, 4,
                isPresent, "Exposição total à luz solar."),
            _ => throw new ArgumentOutOfRangeException(nameof(trigger))
        };
    }

    private static FrenzyTriggerResult Make(
        FrenzyTrigger trigger, FrenzyType type, int difficulty, bool applies, string description) =>
        new(trigger, type, difficulty, applies, description);
}
