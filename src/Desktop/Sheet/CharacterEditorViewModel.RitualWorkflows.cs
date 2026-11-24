using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game.Application.Blood;
using Game.Content.Disciplines;
using Game.Rules.V5.Rituals;
using Game.Sheet;

namespace Game.Desktop.Sheet;

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
    [ObservableProperty] private bool ritualFollowUpBeastSubdued;
    [ObservableProperty] private int ritualAirPassengerPool = 5;
    [ObservableProperty] private int ritualAirPassengerHunger;
    [ObservableProperty] private bool ritualAirPassengerBeastSubdued;
    [ObservableProperty] private int ritualAirGrounderPool = 5;
    [ObservableProperty] private int ritualAirGrounderHunger;
    [ObservableProperty] private int ritualAirCasterSuccesses;
    [ObservableProperty] private bool ritualAirGrounderBeastSubdued;
    [ObservableProperty] private bool ritualIncorporealShardReleased;
    [ObservableProperty] private bool ritualIncorporealInsideSolid;
    [ObservableProperty] private string ritualIncorporealNarratorResolution = "";
    partial void OnRitualFollowUpBeastSubduedChanged(bool value) => CoreRitualConditionsConfirmed = false;
    partial void OnRitualFollowUpPoolChanged(int value) => CoreRitualConditionsConfirmed = false;
    partial void OnRitualFollowUpHungerChanged(int value) => CoreRitualConditionsConfirmed = false;
    partial void OnRitualAirPassengerBeastSubduedChanged(bool value) => CoreRitualConditionsConfirmed = false;
    partial void OnRitualAirPassengerPoolChanged(int value) => CoreRitualConditionsConfirmed = false;
    partial void OnRitualAirPassengerHungerChanged(int value) => CoreRitualConditionsConfirmed = false;
    partial void OnRitualAirGrounderBeastSubduedChanged(bool value) => CoreRitualConditionsConfirmed = false;
    partial void OnRitualAirGrounderPoolChanged(int value) => CoreRitualConditionsConfirmed = false;
    partial void OnRitualAirGrounderHungerChanged(int value) => CoreRitualConditionsConfirmed = false;
    partial void OnRitualAirCasterSuccessesChanged(int value) => CoreRitualConditionsConfirmed = false;
    partial void OnRitualIncorporealShardReleasedChanged(bool value) => CoreRitualConditionsConfirmed = false;
    partial void OnRitualIncorporealInsideSolidChanged(bool value) => CoreRitualConditionsConfirmed = false;
    partial void OnRitualIncorporealNarratorResolutionChanged(string value) => CoreRitualConditionsConfirmed = false;
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
        try
        {
            RitualWorkflowActions.ValidateBeneficiary(ritual, CoreRitualTargetsSelf, CoreRitualTarget,
                RitualExceptionApproved, RitualExceptionReason);
        }
        catch (InvalidOperationException exception)
        { RitualPreparationSummary = exception.Message; return; }
        var target = CoreRitualTargetsSelf ? Name : CoreRitualTarget.Trim();
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
            var resolution = new RitualWorkflowActions().Resolve(ritual, state, RitualContext(ritual),
                DateOnly.FromDateTime(PowerGameDate));
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
            var beastAudit = RitualBeastAudit(resolution);
            ReplacePowerEffect(entry, entry with { RitualWorkflow = resolution.State,
                Active = resolution.State.Stage is not ("failed" or "reverted" or "triggered" or "completed"), Effect = resolution.Proposal.Effect + beastAudit,
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
        if (SelectedPowerEffect is not { } entry)
        { RitualPreparationSummary = "Selecione um rastro ativo e confirme a situação atual com o Narrador."; return; }
        try
        {
            var result = new RitualWorkflowActions().TrackPrey(entry, AttributeRating("intelligence"),
                Skills.Items.FirstOrDefault(x => x.Key == "skill-survival")?.Rating ?? 0, Hunger,
                CoreRitualConditionsConfirmed, HasActivePower("power-animalism-quell-the-beast"));
            DisplayRitualResolution(entry, result);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void EndSireCommunicationAfterTenMinutes() =>
        EndSireCommunication(SireCommunicationEnding.TenMinutesElapsed);

    [RelayCommand]
    private void EndSireCommunicationAfterDisturbance() =>
        EndSireCommunication(SireCommunicationEnding.Disturbed);

    private void EndSireCommunication(SireCommunicationEnding ending)
    {
        if (SelectedPowerEffect is not { Active: true, PowerId: "ritual-communicate-with-kindred-sire" } entry)
        { RitualPreparationSummary = "Selecione uma Comunicação com o Senhor ativa."; return; }
        try
        {
            var ended = new RitualWorkflowActions().EndSireCommunication(entry, ending,
                CoreRitualConditionsConfirmed);
            ReplacePowerEffect(entry, ended);
            RitualPreparationSummary = ended.Effect + $" Fonte: {ended.Source}.";
            Notebook.AppendLog("Rituais", RitualPreparationSummary);
            MarkChanged();
            CoreRitualConditionsConfirmed = false;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void EndTruthOfBloodAfterScene()
    {
        if (SelectedPowerEffect is not { Active: true, PowerId: "ritual-truth-of-blood" } entry)
        { RitualPreparationSummary = "Selecione Verdade do Sangue com afirmações ativas."; return; }
        try
        {
            var ended = new RitualWorkflowActions().EndTruthOfBloodAfterScene(entry,
                CoreRitualConditionsConfirmed);
            ReplacePowerEffect(entry, ended);
            RitualPreparationSummary = ended.Effect + $" Fonte: {ended.Source}.";
            Notebook.AppendLog("Rituais", RitualPreparationSummary);
            MarkChanged();
            CoreRitualConditionsConfirmed = false;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void FinishDagonRepetitions()
    {
        if (SelectedPowerEffect is not { Active: true, PowerId: "ritual-dagons-call" } entry)
        { RitualPreparationSummary = "Selecione O Chamado de Dagon aguardando repetição."; return; }
        try
        {
            var ended = new RitualWorkflowActions().FinishDagonRepetitions(entry,
                CoreRitualConditionsConfirmed);
            ReplacePowerEffect(entry, ended);
            RitualPreparationSummary = ended.Effect + $" Fonte: {ended.Source}.";
            Notebook.AppendLog("Rituais", RitualPreparationSummary);
            MarkChanged();
            CoreRitualConditionsConfirmed = false;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void ExpireIlluminatedTrail()
    {
        if (SelectedPowerEffect is not { Active: true, PowerId: "ritual-illuminate-trail-of-prey" } entry)
        { RitualPreparationSummary = "Selecione um rastro iluminado ativo."; return; }
        try
        {
            var ended = new RitualWorkflowActions().ExpireIlluminatedTrail(entry,
                DateOnly.FromDateTime(PowerGameDate), CoreRitualConditionsConfirmed,
                RitualEndingExceptionApproved, RitualEndingExceptionReason);
            ReplacePowerEffect(entry, ended);
            RitualPreparationSummary = ended.Effect + $" Fonte: {ended.Source}.";
            Notebook.AppendLog("Rituais", RitualPreparationSummary);
            if (ended.Duration == "Encerrado por decisão do Narrador")
                ExceptionRegister.Record(entry.Name, "Encerramento excepcional do rastro iluminado",
                    RitualEndingExceptionReason.Trim());
            MarkChanged();
            CoreRitualConditionsConfirmed = false;
            RitualEndingExceptionApproved = false;
            RitualEndingExceptionReason = "";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
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
            var result = surgery ? actions.RemoveShaftSplinter(state, RitualFollowUpPool, RitualFollowUpHunger, CoreRitualConditionsConfirmed, beastSubdued: RitualFollowUpBeastSubdued)
                : actions.StrikeShaft(state, RitualFollowUpPool, RitualFollowUpHunger, CoreRitualOpponentSuccesses, CoreRitualConditionsConfirmed, beastSubdued: RitualFollowUpBeastSubdued);
            ReplacePowerEffect(entry, entry with { RitualWorkflow = result.State, Effect = result.Proposal.Effect + RitualBeastAudit(result),
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
                Effect = $"{CoreRitualIntruder.Trim()}: {result.Proposal.Effect}{RitualBeastAudit(result)}" });
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

    [RelayCommand]
    private void ExpireRitualAirPotion()
    {
        if (SelectedPowerEffect is not { Active: true, PowerId: "ritual-essence-of-air", RitualWorkflow: { } state } entry ||
            RitualUseCatalog.Find(entry.PowerId) is not { } ritual)
        { RitualPreparationSummary = "Selecione uma poção ativa de Essência do Ar com doses restantes."; return; }
        try
        {
            var result = new RitualWorkflowActions().ExpireAirPotion(ritual, state,
                DateOnly.FromDateTime(PowerGameDate), CoreRitualConditionsConfirmed);
            ReplacePowerEffect(entry, entry with { Active = false, RitualWorkflow = result.State,
                Effect = result.Proposal.Effect, Duration = result.Proposal.Duration });
            DisplayRitualResolution(entry, result);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void EndRitualIncorporealPassage()
    {
        if (SelectedPowerEffect is not { Active: true, PowerId: "ritual-incorporeal-passage" } entry)
        { RitualPreparationSummary = "Selecione uma Passagem Incorpórea ativa."; return; }
        try
        {
            var ended = new RitualWorkflowActions().EndIncorporealPassage(entry,
                RitualIncorporealShardReleased, RitualIncorporealInsideSolid,
                CoreRitualConditionsConfirmed, RitualIncorporealNarratorResolution);
            ReplacePowerEffect(entry, ended);
            RitualPreparationSummary = ended.Effect + $" Fonte: {ended.Source}.";
            Notebook.AppendLog("Rituais", RitualPreparationSummary);
            RefreshRollPool();
            RefreshPowerUse();
            MarkChanged();
            CoreRitualConditionsConfirmed = false;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void EndSacredHavenAfterDay() => EndSacredHaven(SacredHavenEnding.DayEnded);

    [RelayCommand]
    private void EndSacredHavenAfterCasterLeft() => EndSacredHaven(SacredHavenEnding.CasterLeftArea);

    private void EndSacredHaven(SacredHavenEnding ending)
    {
        if (SelectedPowerEffect is not { Active: true, PowerId: "ritual-defense-of-sacred-haven" } entry)
        { RitualPreparationSummary = "Selecione uma Defesa do Refúgio Sagrado ativa."; return; }
        try
        {
            var ended = new RitualWorkflowActions().EndSacredHaven(entry, ending, CoreRitualConditionsConfirmed);
            ReplacePowerEffect(entry, ended);
            RitualInsideProtectedHaven = false;
            RitualPreparationSummary = ended.Effect + $" Fonte: {ended.Source}.";
            Notebook.AppendLog("Rituais", RitualPreparationSummary);
            RefreshRollPool();
            RefreshPowerUse();
            MarkChanged();
            CoreRitualConditionsConfirmed = false;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void EndDeflectionAfterNight() => EndDeflection(DeflectionEnding.NightEnded);

    [RelayCommand]
    private void EndDeflectionAfterSplinterRemoved() => EndDeflection(DeflectionEnding.SplinterRemoved);

    private void EndDeflection(DeflectionEnding ending)
    {
        if (SelectedPowerEffect is not { Active: true, PowerId: "ritual-deflection-of-wooden-doom" } entry)
        { RitualPreparationSummary = "Selecione uma Deflecção ainda aguardando a primeira tentativa genuína de estaquear."; return; }
        try
        {
            var ended = new RitualWorkflowActions().EndDeflection(entry, ending, CoreRitualConditionsConfirmed);
            ReplacePowerEffect(entry, ended);
            RitualPreparationSummary = ended.Effect + $" Fonte: {ended.Source}.";
            Notebook.AppendLog("Rituais", RitualPreparationSummary);
            MarkChanged();
            CoreRitualConditionsConfirmed = false;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void EndFirewalkerAfterNight()
    {
        if (SelectedPowerEffect is not { Active: true, PowerId: "ritual-firewalker" } entry)
        { RitualPreparationSummary = "Selecione um Andarilho do Fogo ativo."; return; }
        try
        {
            var ended = new RitualWorkflowActions().EndFirewalker(entry,
                DateOnly.FromDateTime(PowerGameDate), CoreRitualConditionsConfirmed);
            ReplacePowerEffect(entry, ended);
            if (entry.IsSelf) RitualIncomingDamageIsFire = false;
            RitualPreparationSummary = ended.Effect + $" Fonte: {ended.Source}.";
            Notebook.AppendLog("Rituais", RitualPreparationSummary);
            RefreshRollPool();
            RefreshPowerUse();
            MarkChanged();
            CoreRitualConditionsConfirmed = false;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void EndClingingAfterScene() => EndClinging(ClingingEnding.SceneEnded);

    [RelayCommand]
    private void EndClingingAfterNight() => EndClinging(ClingingEnding.NightEnded);

    private void EndClinging(ClingingEnding ending)
    {
        if (SelectedPowerEffect is not { Active: true, PowerId: "ritual-clinging-of-the-insect" } entry)
        { RitualPreparationSummary = "Selecione uma Aderência do Inseto ativa."; return; }
        try
        {
            var ended = new RitualWorkflowActions().EndClinging(entry, ending,
                DateOnly.FromDateTime(PowerGameDate), CoreRitualConditionsConfirmed);
            ReplacePowerEffect(entry, ended);
            RitualPreparationSummary = ended.Effect + $" Fonte: {ended.Source}.";
            Notebook.AppendLog("Rituais", RitualPreparationSummary);
            MarkChanged();
            CoreRitualConditionsConfirmed = false;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void ExpireEyesOfBabel()
    {
        if (SelectedPowerEffect is not { Active: true, PowerId: "ritual-eyes-of-babel" } entry)
        { RitualPreparationSummary = "Selecione Olhos de Babel ativo."; return; }
        try
        {
            var ended = new RitualWorkflowActions().ExpireEyesOfBabel(entry,
                DateOnly.FromDateTime(PowerGameDate), CoreRitualConditionsConfirmed,
                RitualEndingExceptionApproved, RitualEndingExceptionReason);
            ReplacePowerEffect(entry, ended);
            RitualPreparationSummary = ended.Effect + $" Fonte: {ended.Source}.";
            Notebook.AppendLog("Rituais", RitualPreparationSummary);
            if (ended.Duration == "Encerrado por decisão do Narrador")
                ExceptionRegister.Record(entry.Name, "Encerramento excepcional de Olhos de Babel",
                    RitualEndingExceptionReason.Trim());
            MarkChanged();
            CoreRitualConditionsConfirmed = false;
            RitualEndingExceptionApproved = false;
            RitualEndingExceptionReason = "";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void EndBloodstoneAfterWeek() => EndBloodstone(BloodstoneEnding.WeekElapsed);

    [RelayCommand]
    private void EndBloodstoneDestroyed() => EndBloodstone(BloodstoneEnding.Destroyed);

    private void EndBloodstone(BloodstoneEnding ending)
    {
        if (SelectedPowerEffect is not { Active: true, PowerId: "ritual-craft-bloodstone" } entry)
        { RitualPreparationSummary = "Selecione uma Pedra de Sangue ativa."; return; }
        try
        {
            var ended = new RitualWorkflowActions().EndBloodstone(entry, ending,
                DateOnly.FromDateTime(PowerGameDate), CoreRitualConditionsConfirmed,
                RitualEndingExceptionApproved, RitualEndingExceptionReason);
            ReplacePowerEffect(entry, ended);
            RitualPreparationSummary = ended.Effect + $" Fonte: {ended.Source}.";
            Notebook.AppendLog("Rituais", RitualPreparationSummary);
            if (ended.Duration == "Encerrado por decisão do Narrador")
                ExceptionRegister.Record(entry.Name, "Encerramento excepcional da Pedra de Sangue",
                    RitualEndingExceptionReason.Trim());
            MarkChanged();
            CoreRitualConditionsConfirmed = false;
            RitualEndingExceptionApproved = false;
            RitualEndingExceptionReason = "";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void ResolveRitualAirPassengerContest()
    {
        if (SelectedPowerEffect is not { Active: true, PowerId: "ritual-essence-of-air", RitualWorkflow: { } state } entry ||
            RitualUseCatalog.Find(entry.PowerId) is not { } ritual)
        { RitualPreparationSummary = "Selecione um consumo ativo de Essência do Ar."; return; }
        try
        {
            var result = new RitualWorkflowActions().ResolveAirPassengerContest(ritual, state,
                CoreRitualTarget, RitualAirPassengerPool, RitualAirPassengerHunger, CoreRitualOpponentSuccesses,
                CoreRitualConditionsConfirmed, beastSubdued: RitualAirPassengerBeastSubdued);
            ReplacePowerEffect(entry, entry with { Effect = result.Proposal.Effect + RitualBeastAudit(result) });
            DisplayRitualResolution(entry, result);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void ResolveRitualAirGroundingContest()
    {
        if (SelectedPowerEffect is not { Active: true, PowerId: "ritual-essence-of-air", RitualWorkflow: { } state } entry ||
            RitualUseCatalog.Find(entry.PowerId) is not { } ritual)
        { RitualPreparationSummary = "Selecione um consumo ativo de Essência do Ar."; return; }
        try
        {
            var result = new RitualWorkflowActions().ResolveAirGroundingContest(ritual, state,
                CoreRitualTarget, RitualAirGrounderPool, RitualAirGrounderHunger, RitualAirCasterSuccesses,
                CoreRitualConditionsConfirmed, beastSubdued: RitualAirGrounderBeastSubdued);
            ReplacePowerEffect(entry, entry with { Effect = result.Proposal.Effect + RitualBeastAudit(result) });
            DisplayRitualResolution(entry, result);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    private void DisplayRitualResolution(PowerEffectEntry entry, RitualWorkflowResolution resolution)
    {
        RitualPreparationSummary = $"{entry.Name}: {resolution.Proposal.Effect}{RitualBeastAudit(resolution)} Duração: {resolution.Proposal.Duration}. Fonte: {entry.Source}.";
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

    private static string RitualBeastAudit(RitualWorkflowResolution resolution) =>
        resolution.BeastSubduedConsidered
            ? resolution.MessyCriticalConverted
                ? " Besta Subjugada confirmada: crítico bestial convertido em crítico normal. Fonte: Livro base, p. 246."
                : " Besta Subjugada confirmada e considerada; nenhuma conversão foi necessária. Fonte: Livro base, p. 246."
            : "";
}
