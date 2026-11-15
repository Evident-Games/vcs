using CommunityToolkit.Mvvm.ComponentModel;

namespace VCS.Desktop.Sheet;

public partial class CharacterEditorViewModel
{
    [ObservableProperty] private string currentPowerRollTarget = "";
    private int EntrancementPoolBonus => !RollIsCombat && SelectedRollAttribute?.Key is
        "attribute-charisma" or "attribute-manipulation" or "attribute-composure"
        ? PowerEffects.Where(x => x.Active && x.PowerId == "power-presence-entrancement" &&
            string.Equals(x.Target.Trim(), CurrentPowerRollTarget.Trim(), StringComparison.OrdinalIgnoreCase))
            .Select(x => x.PoolBonus).DefaultIfEmpty(0).Max() : 0;
    partial void OnCurrentPowerRollTargetChanged(string value) => RefreshRollPool();
    [ObservableProperty] private bool powerBalanceContext;
    [ObservableProperty] private bool powerClimbingContext;
    [ObservableProperty] private bool powerTerrainSupportsWeight;
    [ObservableProperty] private bool powerNonMetalSurface;
    [ObservableProperty] private bool powerCounteredByCelerity;
    [ObservableProperty] private bool strengthFeatContext;
    [ObservableProperty] private int visionPenalty;
    [ObservableProperty] private int lackOfCoverPenalty;
    [ObservableProperty] private int minorActionPenalty;
    [ObservableProperty] private bool reflexesMinorActionUsed;
    [ObservableProperty] private bool powerFormProfileConfirmed;
    [ObservableProperty] private int powerFormStrength = 1;
    [ObservableProperty] private int powerFormDexterity = 1;
    [ObservableProperty] private int powerFormStamina = 1;

    private bool HasActivePower(string id) => PowerEffects.Any(x => x.Active && x.IsSelf && x.PowerId == id);
    private string? PendingAttackPower => !UseSheetDicePool || !RollIsCombat ? null
        : SelectedRollSkill?.Key == "skill-firearms" && HasActivePower("power-celerity-unerring-aim") ? "power-celerity-unerring-aim"
        : SelectedRollSkill?.Key is "skill-brawl" or "skill-melee" && HasActivePower("power-celerity-lightning-strike") ? "power-celerity-lightning-strike"
        : null;
    private int EffectiveRollDifficulty => PowerCounteredByCelerity || PendingAttackPower is null ? RollDifficulty : 1;
    private int ContextPenalties => -(EyesOfTheBeastActive ? 0 : Math.Clamp(VisionPenalty, 0, 5))
        - (KnowsPower("power-celerity-rapid-reflexes") ? 0 : Math.Clamp(LackOfCoverPenalty, 0, 5))
        - (KnowsPower("power-celerity-rapid-reflexes") && !ReflexesMinorActionUsed
            ? Math.Max(0, Math.Clamp(MinorActionPenalty, 0, 5) - 2) : Math.Clamp(MinorActionPenalty, 0, 5));
    private int FormAttributeAdjustment
    {
        get
        {
            var form = PowerEffects.LastOrDefault(x => x.Active && x.IsSelf && x.TurnsRemaining == 0 &&
                x.PowerId is "power-protean-shapechange" or "power-protean-metamorphosis");
            if (form is null) return 0;
            var rating = SelectedRollAttribute?.Key switch
            {
                "attribute-strength" => form.FormStrength,
                "attribute-dexterity" => form.FormDexterity,
                "attribute-stamina" => form.FormStamina,
                _ => null
            };
            return rating is { } value ? value - (SelectedRollAttribute?.Rating ?? 0) : 0;
        }
    }
    partial void OnStrengthFeatContextChanged(bool value) => RefreshRollPool();
    partial void OnVisionPenaltyChanged(int value) => RefreshRollPool();
    partial void OnLackOfCoverPenaltyChanged(int value) => RefreshRollPool();
    partial void OnMinorActionPenaltyChanged(int value) => RefreshRollPool();
    partial void OnReflexesMinorActionUsedChanged(bool value) => RefreshRollPool();
    partial void OnPowerCounteredByCelerityChanged(bool value) => RefreshRollPool();

    private bool ResolveAutomaticPowerTest()
    {
        if (!UseSheetDicePool || RollIsCombat) return false;
        var balance = PowerBalanceContext && PowerTerrainSupportsWeight && KnowsPower("power-celerity-cats-grace") &&
            (SelectedRollAttribute?.Key == "attribute-dexterity" || SelectedRollSkill?.Key == "skill-athletics");
        var climb = PowerClimbingContext && PowerNonMetalSurface && SelectedRollSkill?.Key == "skill-athletics" && HasActivePower("power-potence-uncanny-grip");
        if (!balance && !climb) return false;
        RollSummary = balance ? "Graça Felina: equilíbrio bem-sucedido automaticamente; suporte confirmado para o peso."
            : "Pegada Sobrenatural: escalada bem-sucedida na superfície não metálica confirmada. Rastros permanecem.";
        ShowDiceRoll("Sucesso automático", RollSummary, []);
        Notebook.AppendLog("Usos de Disciplinas", RollSummary);
        return true;
    }

    private void ConsumeAttackPower(string? powerId)
    {
        if (powerId is null) return;
        foreach (var entry in PowerEffects.Where(x => x.Active && x.IsSelf && x.PowerId == powerId).ToArray())
            ReplacePowerEffect(entry, entry with { Active = false });
        MarkChanged();
    }
}
