using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Application.Blood;
using VCS.Content.Disciplines;
using VCS.Rules.V5.Rituals;
using VCS.Sheet;

namespace VCS.Desktop.Sheet;

public partial class CharacterEditorViewModel
{
    [ObservableProperty] private bool ritualEndingExceptionApproved;
    [ObservableProperty] private string ritualEndingExceptionReason = "";
    public string RitualBlindnessSummary => string.Join(" · ", PowerEffects.Where(x => x.Active &&
        x.RitualWorkflow is { Stage: "blindness", NightsRemaining: > 0 }).Select(x =>
        $"{x.Target}: cegueira, {x.RitualWorkflow!.NightsRemaining} noite(s) restante(s)"));

    [RelayCommand] private void EndNighthawkWithEyesRemoved() => EndNighthawkFlight(true);
    [RelayCommand] private void EndNighthawkWithoutEyesRemoved() => EndNighthawkFlight(false);

    private void EndNighthawkFlight(bool eyesRemoved)
    {
        if (SelectedPowerEffect is not { Active: true, RitualWorkflow: { } state } entry ||
            RitualUseCatalog.Find(entry.PowerId) is not { Kind: "nighthawk" } ritual)
        { RitualPreparationSummary = "Selecione Olhos do Falcão Noturno com voo ativo."; return; }
        try
        {
            var result = new RitualWorkflowActions().EndNighthawk(ritual, state, DateOnly.FromDateTime(PowerGameDate),
                eyesRemoved, CoreRitualConditionsConfirmed);
            ReplacePowerEffect(entry, entry with { Active = false, RitualWorkflow = result.EndedState,
                Effect = result.Proposal.Effect + " Fonte: " + RitualOngoingRules.NighthawkEndingSource,
                Duration = result.Proposal.Duration });
            if (result.Condition is { } condition)
            {
                var blindness = new PowerEffectEntry(Guid.NewGuid(), entry.PowerId, entry.Name + " · cegueira",
                    entry.IsSelf ? Name : entry.Target, entry.IsSelf, DateTime.UtcNow,
                    entry.IsSelf ? "Cegueira por três noites após o voo, confirmada pela mesa."
                        : "Proposta de cegueira por três noites ao beneficiário confirmado; nenhuma outra ficha foi alterada.",
                    "Três noites", RitualOngoingRules.NighthawkEndingSource, entry.Ruling, true,
                    GameDate: PowerGameDate.Date, RitualWorkflow: condition);
                PowerEffects.Add(blindness);
                SelectedPowerEffect = blindness;
            }
            DisplayRitualResolution(entry with { Source = RitualOngoingRules.NighthawkEndingSource },
                new(result.EndedState, result.Proposal, null));
            OnPropertyChanged(nameof(RitualBlindnessSummary));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void AdvanceRitualBlindnessNights()
    {
        if (SelectedPowerEffect is not { Active: true, PowerId: "ritual-eyes-of-nighthawk", RitualWorkflow: { } state } entry)
        { RitualPreparationSummary = "Selecione o registro de cegueira de Olhos do Falcão Noturno."; return; }
        try
        {
            var result = new RitualWorkflowActions().AdvanceNighthawkBlindness(state, DateOnly.FromDateTime(PowerGameDate),
                CoreRitualConditionsConfirmed);
            ReplacePowerEffect(entry, entry with { RitualWorkflow = result.State, Active = result.State.NightsRemaining > 0,
                Effect = result.Proposal.Effect, Duration = result.Proposal.Duration });
            DisplayRitualResolution(entry, result);
            OnPropertyChanged(nameof(RitualBlindnessSummary));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void EndRitualBlindnessByRuling()
    {
        if (SelectedPowerEffect is not { Active: true, PowerId: "ritual-eyes-of-nighthawk", RitualWorkflow: { Stage: "blindness" } state } entry ||
            !CoreRitualConditionsConfirmed || !RitualEndingExceptionApproved || string.IsNullOrWhiteSpace(RitualEndingExceptionReason))
        { RitualPreparationSummary = "Selecione a cegueira ativa, confirme a decisão e registre a autorização e a justificativa do Narrador."; return; }
        var reason = RitualEndingExceptionReason.Trim();
        ExceptionRegister.Record(entry.Name, "Encerramento da cegueira antes das três noites", reason);
        ReplacePowerEffect(entry, entry with { Active = false, RitualWorkflow = state with { Stage = "ended", NightsRemaining = 0 },
            Ruling = string.IsNullOrWhiteSpace(entry.Ruling) ? reason : entry.Ruling + "\n" + reason,
            Effect = "Cegueira encerrada por decisão excepcional do Narrador: " + reason });
        RitualPreparationSummary = "Cegueira encerrada; a decisão excepcional foi registrada na ficha.";
        Notebook.AppendLog("Rituais", RitualPreparationSummary);
        MarkChanged();
        OnPropertyChanged(nameof(RitualBlindnessSummary));
        CoreRitualConditionsConfirmed = false;
        RitualEndingExceptionApproved = false;
        RitualEndingExceptionReason = "";
    }
}
