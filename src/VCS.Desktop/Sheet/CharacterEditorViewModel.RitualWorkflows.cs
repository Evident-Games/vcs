using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Application.Blood;
using VCS.Content.Disciplines;
using VCS.Rules.V5.Rituals;
using VCS.Sheet;

namespace VCS.Desktop.Sheet;

public partial class CharacterEditorViewModel
{
    [ObservableProperty] private string coreRitualTarget = "";
    [ObservableProperty] private bool coreRitualTargetsSelf = true;
    [ObservableProperty] private bool coreRitualLongTermCircle;
    [ObservableProperty] private bool coreRitualConditionsConfirmed;
    [ObservableProperty] private int coreRitualOpponentSuccesses;
    [ObservableProperty] private bool coreRitualVampireTarget = true;
    [ObservableProperty] private int coreRitualAreaRadius = 3;
    [ObservableProperty] private int coreRitualActiveBloodstones;
    [ObservableProperty] private bool coreRitualCandleDamageConfirmed;
    [ObservableProperty] private string coreRitualIntruder = "";
    [ObservableProperty] private int coreRitualIntruderWillpower = 5;
    [ObservableProperty] private bool coreRitualRetry;
    [ObservableProperty] private bool coreRitualRetryCostConfirmed;
    [ObservableProperty] private bool ritualActiveSocialTest;
    [ObservableProperty] private bool ritualResistingPresence;
    [ObservableProperty] private bool ritualIncomingDamageIsFire;
    [ObservableProperty] private bool ritualInsideProtectedHaven;
    [ObservableProperty] private int ritualFollowUpPool = 5;
    [ObservableProperty] private int ritualFollowUpHunger;
    partial void OnRitualFollowUpPoolChanged(int value) => CoreRitualConditionsConfirmed = false;
    partial void OnRitualFollowUpHungerChanged(int value) => CoreRitualConditionsConfirmed = false;
    partial void OnPowerGameDateChanged(DateTime value) => CoreRitualConditionsConfirmed = false;

    private bool HasRitualEffect(string id) => PowerEffects.Any(x => x.Active && x.IsSelf &&
        x.PowerId == id && x.RitualWorkflow is { Stage: "effect", Resolved: true });
    private int RitualRollModifier => RitualOngoingRules.PoolModifier(HasRitualEffect("ritual-heart-of-stone"),
        RitualActiveSocialTest, SelectedRollSkill?.Key == "skill-intimidation", false, RitualResistingPresence);
    private int RitualPowerPoolModifier => RitualOngoingRules.PoolModifier(HasRitualEffect("ritual-heart-of-stone"),
        RitualActiveSocialTest, PowerUseTrait?.Key == "skill-intimidation",
        ActivationPower?.Entry?.Definition.DisciplineId == "discipline-dominate", RitualResistingPresence);
    partial void OnRitualActiveSocialTestChanged(bool value) { RefreshRollPool(); RefreshPowerUse(); }
    partial void OnRitualResistingPresenceChanged(bool value) { RefreshRollPool(); RefreshPowerUse(); }

    public string CoreRitualGuidance => SelectedPreparedRitual is { } row && RitualUseCatalog.Find(row.Key) is { } ritual
        ? $"{ritual.Ingredients}\n{ritual.Process}\n{ritual.Trigger}\n" +
          $"Preparação: {ritual.PreparationNights} noite(s); {ritual.PreparationMinutes} min (0: consulte o procedimento). " +
          $"{ritual.RouseChecks} Checagem(ns) de Sangue. Fonte: {ritual.Source}."
        : "Selecione um ritual do livro base conhecido pela ficha.";

    partial void OnCoreRitualTargetChanged(string value) => CoreRitualConditionsConfirmed = false;
    partial void OnCoreRitualTargetsSelfChanged(bool value) => CoreRitualConditionsConfirmed = false;
    partial void OnCoreRitualOpponentSuccessesChanged(int value) => CoreRitualConditionsConfirmed = false;
    partial void OnCoreRitualVampireTargetChanged(bool value) => CoreRitualConditionsConfirmed = false;
    partial void OnCoreRitualAreaRadiusChanged(int value) => CoreRitualConditionsConfirmed = false;
    partial void OnCoreRitualActiveBloodstonesChanged(int value) => CoreRitualConditionsConfirmed = false;
    partial void OnCoreRitualIntruderWillpowerChanged(int value) => CoreRitualConditionsConfirmed = false;
    partial void OnCoreRitualIntruderChanged(string value)
    {
        CoreRitualConditionsConfirmed = false;
        CoreRitualRetryCostConfirmed = false;
    }

    private void StartRitualWorkflow()
    {
        if (SelectedPreparedRitual is not { } row || RitualUseCatalog.Find(row.Key) is not { } ritual)
        { RitualPreparationSummary = "Selecione um ritual conhecido do livro base."; return; }
        if (!RitualConditionsConfirmed || (row.HasWarning && !RitualExceptionApproved) ||
            (RitualExceptionApproved && string.IsNullOrWhiteSpace(RitualExceptionReason)))
        { RitualPreparationSummary = "Confirme o procedimento e justifique qualquer exceção com o Narrador."; return; }
        if (!CoreRitualTargetsSelf && string.IsNullOrWhiteSpace(CoreRitualTarget))
        { RitualPreparationSummary = "Identifique o alvo, objeto ou local deste preparo."; return; }
        var target = CoreRitualTargetsSelf ? Name : CoreRitualTarget.Trim();
        if (!CoreRitualTargetsSelf && (ritual.Kind is "clinging" or "deflection" or "incorporeal" or "heart-stone") && !RitualExceptionApproved)
        { RitualPreparationSummary = "Este ritual beneficia o conjurador; outro beneficiário exige exceção registrada."; return; }
        if (PowerEffects.FirstOrDefault(x => x.Active && x.PowerId == ritual.RitualId && x.Target == target &&
            x.RitualWorkflow is { Resolved: false }) is { } pending)
        {
            SelectedPowerEffect = pending;
            RitualPreparationSummary = "Preparo pendente selecionado. Continue as etapas já salvas, sem iniciar outro custo.";
            return;
        }
        var entry = new PowerEffectEntry(Guid.NewGuid(), ritual.RitualId, row.Name, target, CoreRitualTargetsSelf,
            DateTime.UtcNow, "Preparo iniciado; nenhuma checagem paga ainda.", "Preparação", ritual.Source,
            RitualExceptionApproved ? RitualExceptionReason.Trim() : "", true,
            GameDate: PowerGameDate.Date, RitualWorkflow: new(LongTermCircle: CoreRitualLongTermCircle));
        if (RitualExceptionApproved) ExceptionRegister.Record(row.Name, row.HasWarning ? row.Warning : "Exceção ao ritual", RitualExceptionReason.Trim());
        PowerEffects.Add(entry);
        SelectedPowerEffect = entry;
        RitualPreparationSummary = $"{row.Name}: pague {ritual.RouseChecks} Checagem(ns), uma por vez; depois confirme o gatilho. {ritual.Trigger}";
        Notebook.AppendLog("Rituais", RitualPreparationSummary);
        MarkChanged();
        RitualConditionsConfirmed = false;
    }

    [RelayCommand]
    private void PayRitualWorkflowCheck()
    {
        if (SelectedPowerEffect is not { Active: true, RitualWorkflow: { } state } entry ||
            RitualUseCatalog.Find(entry.PowerId) is not { } ritual) return;
        try
        {
            var payment = new RitualWorkflowActions().PayCheck(ritual, state, Hunger);
            Hunger = payment.Check.HungerAfter;
            var checksRequired = ritual.Kind == "dagon" ? state.Executions + 1 : ritual.RouseChecks;
            RitualPreparationSummary = $"{entry.Name}: Checagem {payment.State.ChecksPaid}/{checksRequired}, dado {payment.Check.Die}; Fome {Hunger}.";
            ReplacePowerEffect(entry, entry with { RitualWorkflow = payment.State, Effect = RitualPreparationSummary });
            Notebook.AppendLog("Rituais", RitualPreparationSummary);
            MarkChanged();
            ShowDiceRoll(entry.Name, RitualPreparationSummary, [new RolledDieDisplay(payment.Check.Die, "SANGUE")]);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    private RitualWorkflowContext RitualContext(RitualUseDefinition ritual) => new(
        Math.Clamp(AttributeRating(ritual.Attribute) + (ritual.Trait.StartsWith("discipline-", StringComparison.Ordinal)
            ? DisciplineRating(ritual.Trait) : AttributeRating(ritual.Trait)), 0, 50),
        Hunger, CoreRitualOpponentSuccesses, CoreRitualVampireTarget, CoreRitualConditionsConfirmed,
        HasActivePower("power-animalism-quell-the-beast"), CoreRitualAreaRadius,
        CoreRitualActiveBloodstones, AttributeRating("resolve"), WillpowerCapacity, Humanity, CoreRitualCandleDamageConfirmed,
        SelectedPowerEffect is { Ruling.Length: > 0 });

    [RelayCommand]
    private void ResolveRitualWorkflow()
    {
        if (SelectedPowerEffect is not { Active: true, RitualWorkflow: { } state } entry ||
            RitualUseCatalog.Find(entry.PowerId) is not { } ritual) return;
        try
        {
            var resolution = new RitualWorkflowActions().Resolve(ritual, state, RitualContext(ritual));
            if (ritual.Kind == "heart-stone" && entry.IsSelf && resolution.State.Stage == "effect")
            {
                var previous = PowerEffects.Where(x => x.Id != entry.Id && x.Active && x.IsSelf &&
                    x.PowerId == entry.PowerId && x.RitualWorkflow is { Stage: "effect" }).ToArray();
                if (previous.Length > 0)
                {
                    foreach (var old in previous) ReplacePowerEffect(old, old with { Active = false });
                    resolution = resolution with { State = resolution.State with { Stage = "reverted" },
                        Proposal = resolution.Proposal with { Effect = "Coração de Pedra revertido: efeito anterior encerrado. " +
                            (resolution.State.StoredCritical ? "Vitória crítica permite curar o ponto agravado específico da vela, mediante confirmação." : ""), Duration = "Reversão concluída" } };
                }
            }
            ReplacePowerEffect(entry, entry with { RitualWorkflow = resolution.State,
                Active = resolution.State.Stage is not ("failed" or "reverted"), Effect = resolution.Proposal.Effect,
                Duration = resolution.Proposal.Duration,
                HealthDamage = entry.IsSelf ? 0 : resolution.Proposal.HealthDamage,
                Aggravated = resolution.Proposal.Aggravated, PoolBonus = resolution.Proposal.PoolBonus,
                RemainingPoints = resolution.Proposal.FollowDifficulty });
            DisplayRitualResolution(entry, resolution);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void RollRitualTracking()
    {
        if (SelectedPowerEffect is not { Active: true, PowerId: "ritual-illuminate-trail-of-prey", RitualWorkflow: { Stage: "effect" } state } entry ||
            !CoreRitualConditionsConfirmed)
        { RitualPreparationSummary = "Selecione um rastro ativo e confirme a situação atual com o Narrador."; return; }
        var pool = AttributeRating("intelligence") + (Skills.Items.FirstOrDefault(x => x.Key == "skill-survival")?.Rating ?? 0);
        var roll = VCS.Rules.V5.Dice.DicePoolRules.Roll(pool, Hunger, entry.RemainingPoints);
        var result = new RitualWorkflowResolution(state,
            new(roll.IsSuccess, roll.IsSuccess ? "O rastro foi seguido; o Narrador descreve o trajeto." : "O teste para seguir o rastro falhou.", entry.Duration), roll);
        DisplayRitualResolution(entry, result);
    }

    [RelayCommand]
    private void RecordBloodstonePreparationNight()
    {
        if (SelectedPowerEffect is not { Active: true, PowerId: "ritual-craft-bloodstone", RitualWorkflow: { } state } entry)
        { RitualPreparationSummary = "Selecione uma Pedra de Sangue em preparo."; return; }
        try
        {
            var next = new RitualWorkflowActions().RecordBloodstoneNight(state, DateOnly.FromDateTime(PowerGameDate),
                CoreRitualConditionsConfirmed, !string.IsNullOrWhiteSpace(entry.Ruling));
            RitualPreparationSummary = $"Pedra de Sangue: noite {next.PreparationNights?.Count}/3, uma hora confirmada em {PowerGameDate:dd/MM/yyyy}.";
            ReplacePowerEffect(entry, entry with { RitualWorkflow = next, Effect = RitualPreparationSummary });
            Notebook.AppendLog("Rituais", RitualPreparationSummary);
            MarkChanged();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void RollRitualShaftAttack() => ResolveShaftFollowUp(false);

    [RelayCommand]
    private void RollRitualShaftSurgery() => ResolveShaftFollowUp(true);

    private void ResolveShaftFollowUp(bool surgery)
    {
        if (SelectedPowerEffect is not { Active: true, PowerId: "ritual-shaft-of-belated-quiescence", RitualWorkflow: { } state } entry)
        { RitualPreparationSummary = "Selecione uma estaca ritual preparada ou sua lasca ativa."; return; }
        try
        {
            var actions = new RitualWorkflowActions();
            var result = surgery ? actions.RemoveShaftSplinter(state, RitualFollowUpPool, RitualFollowUpHunger, CoreRitualConditionsConfirmed)
                : actions.StrikeShaft(state, RitualFollowUpPool, RitualFollowUpHunger, CoreRitualOpponentSuccesses, CoreRitualConditionsConfirmed);
            ReplacePowerEffect(entry, entry with { RitualWorkflow = result.State, Effect = result.Proposal.Effect,
                Duration = result.Proposal.Duration, Active = result.State.Stage != "removed", PoolBonus = result.State.Stage == "effect" ? 3 : 0 });
            DisplayRitualResolution(entry, result);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void ApplyRitualCandleHealing()
    {
        if (SelectedPowerEffect is not { PowerId: "ritual-heart-of-stone", ConsequencesApplied: false,
            RitualWorkflow: { StoredCritical: true, Stage: "effect" or "reverted" } } entry ||
            !CoreRitualCandleDamageConfirmed || HealthAggravated < 1)
        { RitualPreparationSummary = "Selecione Coração de Pedra resolvido com crítico e confirme que o ponto agravado da vela ainda está marcado."; return; }
        HealthAggravated--;
        ReplacePowerEffect(entry, entry with { ConsequencesApplied = true });
        RitualPreparationSummary = "Curado uma única vez o ponto agravado específico da vela de Coração de Pedra.";
        Notebook.AppendLog("Rituais", RitualPreparationSummary);
        MarkChanged();
        CoreRitualCandleDamageConfirmed = false;
    }

    [RelayCommand]
    private void ResolveRitualCircleContact()
    {
        if (SelectedPowerEffect is not { Active: true, RitualWorkflow: { } state } entry ||
            RitualUseCatalog.Find(entry.PowerId) is not { } ritual) return;
        try
        {
            var result = new RitualWorkflowActions().CircleContact(ritual, state, RitualContext(ritual),
                CoreRitualIntruder, CoreRitualIntruderWillpower, CoreRitualRetry, CoreRitualRetryCostConfirmed);
            // The circle belongs to its location, never copy the intruder's proposed damage to its owner.
            ReplacePowerEffect(entry, entry with { RitualWorkflow = result.State,
                Effect = $"{CoreRitualIntruder.Trim()}: {result.Proposal.Effect}" });
            DisplayRitualResolution(entry, result);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void ConsumeRitualAirDose()
    {
        if (SelectedPowerEffect is not { Active: true, RitualWorkflow: { } state } entry ||
            RitualUseCatalog.Find(entry.PowerId) is not { } ritual) return;
        try
        {
            var result = new RitualWorkflowActions().ConsumeAir(ritual, state, CoreRitualConditionsConfirmed);
            ReplacePowerEffect(entry, entry with { RitualWorkflow = result.State, Active = result.State.Charges > 0,
                Effect = $"Poção: {result.State.Charges} dose(s) restante(s)." });
            var effect = new PowerEffectEntry(Guid.NewGuid(), entry.PowerId, entry.Name + " · consumo", Name, true,
                DateTime.UtcNow, result.Proposal.Effect, result.Proposal.Duration, entry.Source, entry.Ruling, true,
                RitualWorkflow: new(Stage: "effect", Resolved: true));
            PowerEffects.Add(effect);
            DisplayRitualResolution(entry, result);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    private void DisplayRitualResolution(PowerEffectEntry entry, RitualWorkflowResolution resolution)
    {
        RitualPreparationSummary = $"{entry.Name}: {resolution.Proposal.Effect} Duração: {resolution.Proposal.Duration}. Fonte: {entry.Source}.";
        if (resolution.Roll is { } roll)
            RitualPreparationSummary += $" Teste: {roll.Successes} sucesso(s); dificuldade {roll.Difficulty}; {roll.Outcome}.";
        if (resolution.OppositionRoll is { } opponent)
            RitualPreparationSummary += $" Oposição: {opponent.Successes} sucesso(s).";
        var dice = new List<RolledDieDisplay>();
        if (resolution.Roll is { } ownRoll)
        {
            dice.AddRange(ownRoll.NormalDice.Select(x => new RolledDieDisplay(x, "COMUM")));
            dice.AddRange(ownRoll.HungerDice.Select(x => new RolledDieDisplay(x, "FOME")));
        }
        if (resolution.OppositionRoll is { } opposingRoll)
            dice.AddRange(opposingRoll.NormalDice.Select(x => new RolledDieDisplay(x, "OPOSIÇÃO")));
        Notebook.AppendLog("Rituais", RitualPreparationSummary);
        MarkChanged();
        RefreshRollPool();
        RefreshPowerUse();
        ShowDiceRoll(entry.Name, RitualPreparationSummary, dice);
        CoreRitualConditionsConfirmed = false;
        CoreRitualCandleDamageConfirmed = false;
        CoreRitualRetryCostConfirmed = false;
    }
}
