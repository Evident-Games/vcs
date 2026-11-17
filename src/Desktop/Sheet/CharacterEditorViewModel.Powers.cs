using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Desktop.Catalogs;

namespace VCS.Desktop.Sheet;

public partial class CharacterEditorViewModel
{
    [ObservableProperty] private bool heightenedSensesActive;
    [ObservableProperty] private bool perceptionRoll;
    [ObservableProperty] private bool sensoryOverload;
    [ObservableProperty] private bool aweActive;
    [ObservableProperty] private bool aweOtherCharismaApproved;
    [ObservableProperty] private bool aweTargetResisted;
    [ObservableProperty] private bool innerFacadeActive;
    [ObservableProperty] private bool mentalProbeResistance;
    private int ContextPowerBonus => VCS.Rules.V5.Conflicts.DisciplinePoolRules.StrongestBonus(
        FleetnessPoolBonus,
        EntrancementPoolBonus,
        ProwessActive && StrengthFeatContext && SelectedRollAttribute?.Key == "attribute-strength" ? DisciplineRating("discipline-potence") : 0,
        DauntActive && !RollIsCombat && SelectedRollSkill?.Key == "skill-intimidation" ? DisciplineRating("discipline-presence") : 0,
        EyesOfTheBeastActive && IntimidationTargetIsMortal && SelectedRollSkill?.Key == "skill-intimidation" ? 2 : 0,
        VCS.Rules.V5.Conflicts.DisciplinePoolRules.GhostInTheMachineBonus(
            GhostInTheMachineApplies, KnowsPower("power-obfuscate-ghost-in-the-machine")),
        HeightenedSensesActive && PerceptionRoll ? DisciplineRating("discipline-auspex") : 0,
        VCS.Rules.V5.Conflicts.DisciplinePoolRules.AweBonus(DisciplineRating("discipline-presence"),
            AweActive && !RollIsCombat, AweTargetResisted, SelectedRollSkill?.Key,
            AweOtherCharismaApproved && SelectedRollAttribute?.Key == "attribute-charisma"),
        InnerFacadeActive && MentalProbeResistance ? DisciplineRating("discipline-fortitude") : 0,
        UnswayableMindApplies && KnowsPower("power-fortitude-unswayable-mind") ? DisciplineRating("discipline-fortitude") : 0);
    private int SensoryPenalty => VCS.Rules.V5.Conflicts.DisciplinePoolRules.SensoryOverloadPenalty(PerceptionRoll, SensoryOverload);
    public int MentalProbeDifficultyIncrease => InnerFacadeActive && !MentalProbeResistance
        ? VCS.Rules.V5.Conflicts.DisciplineCombatRules.InnerFacadeDifficulty(DisciplineRating("discipline-fortitude")) : 0;
    public string ContextPowerSummary => $"Disciplinas: +{ContextPowerBonus} dados (maior bônus aplicável); sobrecarga sensorial: {SensoryPenalty}. " +
        $"Sondagem sem resistência: +{MentalProbeDifficultyIncrease} à dificuldade do oponente. Fonte: Livro base, pp. 244, 249, 259, 267.";
    private void RefreshContextPowers()
    {
        RefreshRollPool();
    }
    partial void OnHeightenedSensesActiveChanged(bool value) => RefreshContextPowers();
    partial void OnPerceptionRollChanged(bool value) => RefreshContextPowers();
    partial void OnSensoryOverloadChanged(bool value) => RefreshContextPowers();
    partial void OnAweActiveChanged(bool value) { if (value) DauntActive = false; RefreshContextPowers(); }
    partial void OnAweOtherCharismaApprovedChanged(bool value) => RefreshContextPowers();
    partial void OnAweTargetResistedChanged(bool value) => RefreshContextPowers();
    partial void OnInnerFacadeActiveChanged(bool value) => RefreshContextPowers();
    partial void OnMentalProbeResistanceChanged(bool value) => RefreshContextPowers();
    [ObservableProperty] private bool activationCostsConfirmed;
    [ObservableProperty] private bool feralWeaponsActive;
    [ObservableProperty] private bool fleetnessActive;
    [ObservableProperty] private bool rollIsCombat;
    [ObservableProperty] private bool rollIsDodge;
    [ObservableProperty] private bool fleetnessDodgeUsed;
    private int FleetnessPoolBonus => FleetnessActive
        ? VCS.Rules.V5.Clans.CelerityRules.FleetnessBonus(DisciplineRating("discipline-celerity"),
            SelectedRollAttribute?.Key == "attribute-dexterity", RollIsCombat,
            RollIsDodge && SelectedRollSkill?.Key == "skill-athletics", FleetnessDodgeUsed) : 0;
    partial void OnFleetnessActiveChanged(bool value) => RefreshRollPool();
    partial void OnRollIsCombatChanged(bool value) => RefreshRollPool();
    partial void OnRollIsDodgeChanged(bool value) => RefreshRollPool();
    partial void OnFleetnessDodgeUsedChanged(bool value) => RefreshRollPool();
    [RelayCommand]
    private void BeginPowerTurn()
    {
        AdvancePowerTurn();
        ReflexesMinorActionUsed = false;
        FleetnessDodgeUsed = false;
        MarbleFirstSourceAvailable = FleshOfMarbleActive;
    }
    [ObservableProperty] private bool prowessFromPainActive;
    [ObservableProperty] private bool fleshOfMarbleActive;
    [ObservableProperty] private bool marbleFirstSourceAvailable;
    [ObservableProperty] private bool incomingSourceIsSunlight;
    [ObservableProperty] private bool incomingAttackIsCritical;
    private int PainPoolBonus => ProwessFromPainActive && SelectedRollAttribute?.Key is
        "attribute-strength" or "attribute-dexterity" or "attribute-stamina"
        ? VCS.Rules.V5.Conflicts.DisciplineCombatRules.ProwessFromPainBonus(SelectedRollAttribute?.Rating ?? 0,
            HealthSuperficial + HealthAggravated, VCS.Rules.V5.Blood.BloodPotencyEffects.At(EffectiveBloodPotency).BloodSurgeDice) : 0;
    partial void OnProwessFromPainActiveChanged(bool value) => RefreshRollPool();
    [ObservableProperty] private bool toughnessActive;
    [ObservableProperty] private bool prowessActive;
    [ObservableProperty] private bool usingMeleeWeapon;
    [ObservableProperty] private bool unswayableMindApplies;
    private int DisciplineRating(string key) => Disciplines.Items.FirstOrDefault(x => x.Key == key)?.Rating ?? 0;
    private bool KnowsPower(string key) => Powers.PowerRows.Any(x => x.Key == key);
    public string PhysicalPowerGuidance => $"Salto vertical: {DisciplineRating("discipline-potence") * 3} m; horizontal: {DisciplineRating("discipline-potence") * 5} m (requer Salto Vertiginoso). " +
        $"Fachada Interior: +{VCS.Rules.V5.Conflicts.DisciplineCombatRules.InnerFacadeDifficulty(DisciplineRating("discipline-fortitude"))} dificuldade quando aplicável.";
    partial void OnUnswayableMindAppliesChanged(bool value) => RefreshRollPool();
    [ObservableProperty] private PowerRowViewModel? activationPower;
    [ObservableProperty] private int activationRouseChecks;
    [ObservableProperty] private int activationWillpowerCost;
    [ObservableProperty] private string activationTarget = "";
    [ObservableProperty] private string activationEffect = "";
    [ObservableProperty] private bool activationExceptionApproved;
    [ObservableProperty] private string activationSummary = "Selecione um poder conhecido e confira os custos da ativação.";

    partial void OnActivationPowerChanged(PowerRowViewModel? value)
    {
        ActivationExceptionApproved = false;
        ActivationCostsConfirmed = false;
        ActivationRouseChecks = 0;
        ActivationWillpowerCost = 0;
        if (value is null) { RefreshPowerUse(); return; }
        ActivationEffect = value.Description;
        if (value.Entry?.Definition is { } definition)
        {
            var guidance = new VCS.Application.Blood.PowerEffectActions().Describe(definition);
            ActivationEffect = $"{guidance.Description} Duração: {guidance.Duration}.";
        }
        PreparePowerUse();
        ActivationSummary = $"Custo informado pelo poder: {value.Cost}. Confira as checagens e Vontade antes de ativar.";
    }

    [RelayCommand]
    private void EndPowerScene()
    {
        HeightenedSensesActive = false;
        PerceptionRoll = false;
        SensoryOverload = false;
        AweActive = false;
        AweTargetResisted = false;
        AweOtherCharismaApproved = false;
        InnerFacadeActive = false;
        MentalProbeResistance = false;
        ProwessFromPainActive = false;
        FleshOfMarbleActive = false;
        MarbleFirstSourceAvailable = false;
        FleetnessActive = false;
        FleetnessDodgeUsed = false;
        FeralWeaponsActive = false;
        ToughnessActive = false;
        ProwessActive = false;
        UnswayableMindApplies = false;
        EndScenePowerEntries();
        foreach (var modifier in PoolModifiers) modifier.IsApplied = false;
        ActivationSummary = "Cena encerrada: efeitos temporários de combate e modificadores desativados.";
        Notebook.AppendLog("Ativações de Disciplinas", ActivationSummary);
    }

    [RelayCommand]
    private void ActivatePower()
    {
        if (SelectedPowerUse is not null) { ResolvePowerUse(); return; }
        if (ActivationPower is not { } power) { ActivationSummary = "Selecione um poder."; return; }
        if (!ActivationCostsConfirmed) { ActivationSummary = "Confira e confirme os custos desta ativação."; return; }
        if (power.HasWarning && !ActivationExceptionApproved)
        {
            ActivationSummary = power.Warning + " Marque a autorização do Narrador para esta exceção.";
            return;
        }
        try
        {
            var result = new VCS.Application.Blood.PowerActivationActions().Activate(Hunger, BloodPotency, power.Level,
                ActivationRouseChecks, ActivationWillpowerCost, WillpowerSuperficial, WillpowerAggravated, WillpowerCapacity);
            if (power.Entry?.Definition.Id == "power-protean-feral-weapons") FeralWeaponsActive = true;
            if (power.Entry?.Definition.Id == "power-fortitude-prowess-from-pain") ProwessFromPainActive = true;
            if (power.Entry?.Definition.Id == "power-fortitude-flesh-of-marble")
            { FleshOfMarbleActive = true; MarbleFirstSourceAvailable = true; }
            if (power.Entry?.Definition.Id == "power-celerity-fleetness") FleetnessActive = true;
            if (power.Entry?.Definition.Id == "power-fortitude-toughness") ToughnessActive = true;
            if (power.Entry?.Definition.Id == "power-potence-prowess") ProwessActive = true;
            if (power.Entry?.Definition.Id == "power-auspex-heightened-senses") HeightenedSensesActive = true;
            if (power.Entry?.Definition.Id == "power-presence-awe") AweActive = true;
            if (power.Entry?.Definition.Id == "power-fortitude-fortify-the-inner-facade") InnerFacadeActive = true;
            Hunger = result.Hunger;
            WillpowerSuperficial = result.WillpowerSuperficial;
            ActivationSummary = $"{power.Name}: custos pagos; Fome {Hunger}. Alvo: {ActivationTarget}. Efeito/duração: {ActivationEffect}. " +
                (ActivationExceptionApproved ? "Exceção autorizada pelo Narrador. " : "") + power.SourceLabel;
            Notebook.AppendLog("Ativações de Disciplinas", ActivationSummary);
            var dice = result.Checks.SelectMany(check => check.RerolledDie is { } reroll
                ? new[] { new RolledDieDisplay(check.Die, "SANGUE"), new RolledDieDisplay(reroll, "SANGUE") }
                : new[] { new RolledDieDisplay(check.Die, "SANGUE") });
            ShowDiceRoll(power.Name, ActivationSummary, dice);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { ActivationSummary = exception.Message; }
    }
}
