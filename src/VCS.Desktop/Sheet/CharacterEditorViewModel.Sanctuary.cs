using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Application.Blood;
using VCS.Sheet;

namespace VCS.Desktop.Sheet;

public partial class CharacterEditorViewModel
{
    public IReadOnlyList<int> SanctuaryCircles { get; } = [1, 2];
    [ObservableProperty] private int sanctuaryCircle = 1;
    [ObservableProperty] private DateTime? sanctuaryNight;
    [ObservableProperty] private bool sanctuaryPreparationConfirmed;
    [ObservableProperty] private bool sanctuaryResetConfirmed;

    private RitualPreparationStep[] SanctuaryPaidChecks() => PowerEffects.Where(x => x.Active && x.IsSelf &&
            x.PowerId == "ritual-sanctuary-preparation-check" && x.PreparationStep is not null)
        .Select(x => x.PreparationStep!).ToArray();

    public string SanctuaryPreparationProgress
    {
        get
        {
            var paid = SanctuaryPaidChecks();
            return $"Partida: {paid.Count(x => x.Circle == 1)}/6 checagens · Chegada: {paid.Count(x => x.Circle == 2)}/6 checagens";
        }
    }

    [RelayCommand]
    private void ResetSanctuaryPreparation()
    {
        if (!SanctuaryResetConfirmed)
        { RitualPreparationSummary = "Confirme com o Narrador o abandono ou a inutilização do par de círculos."; return; }
        foreach (var entry in PowerEffects.Where(x => x.Active && x.IsSelf &&
            x.PowerId == "ritual-sanctuary-preparation-check").ToArray())
            ReplacePowerEffect(entry, entry with { Active = false });
        SanctuaryResetConfirmed = false;
        SanctuaryPreparationConfirmed = false;
        SanctuaryTravelConfirmed = false;
        OnPropertyChanged(nameof(SanctuaryPreparationProgress));
        RitualPreparationSummary = "Preparação do par de círculos encerrada pela mesa. Custos anteriores permanecem no histórico; nenhuma Fome foi devolvida.";
        Notebook.AppendLog("Rituais", RitualPreparationSummary);
        MarkChanged();
    }

    partial void OnSanctuaryCircleChanged(int value) => SanctuaryPreparationConfirmed = false;
    partial void OnSanctuaryNightChanged(DateTime? value) => SanctuaryPreparationConfirmed = false;

    [RelayCommand]
    private void PrepareSanctuaryCheck()
    {
        if (SelectedPreparedRitual is not { } ritual ||
            ritual.Entry?.Definition is not { Id: "ritual-escape-to-true-sanctuary", IsRitual: true } definition)
        { RitualPreparationSummary = "Selecione Fuga para o Verdadeiro Santuário nos rituais desta ficha."; return; }
        if (SanctuaryNight is not { } night)
        { RitualPreparationSummary = "Informe a data desta noite na crônica."; return; }
        if (ritual.HasWarning && (!RitualExceptionApproved || string.IsNullOrWhiteSpace(RitualExceptionReason)))
        { RitualPreparationSummary = ritual.Warning + " Autorize e justifique a exceção com o Narrador."; return; }
        if (RitualExceptionApproved && string.IsNullOrWhiteSpace(RitualExceptionReason))
        { RitualPreparationSummary = "Informe a justificativa do Narrador para a exceção."; return; }
        try
        {
            var paid = SanctuaryPaidChecks();
            var result = new TrueSanctuaryActions().PrepareNextCheck(SanctuaryCircle, DateOnly.FromDateTime(night),
                paid, Hunger, SanctuaryPreparationConfirmed, RitualExceptionApproved);
            var step = result.Advance.Step;
            var check = result.Check;
            Hunger = check.HungerAfter;
            RitualPreparationSummary = $"Santuário, círculo {step.Circle}, noite {(step.Sequence + 1) / 2}/3, " +
                $"checagem {(step.Sequence - 1) % 2 + 1}/2 em {step.NightDate:dd/MM/yyyy}: dado {check.Die}; Fome {Hunger}. " +
                (step.Sequence == 6 ? "As seis checagens deste círculo foram pagas. " : "Preparação ainda em andamento. ") +
                "Fonte: Livro base, p. 281.";
            PowerEffects.Add(new PowerEffectEntry(Guid.NewGuid(), "ritual-sanctuary-preparation-check", ritual.Name,
                $"Círculo {step.Circle}", true, DateTime.UtcNow, RitualPreparationSummary,
                "Preparação dos círculos; encerramento pela mesa", definition.Source,
                RitualExceptionApproved ? RitualExceptionReason.Trim() : "", true,
                GameDate: night.Date, PreparationStep: step));
            OnPropertyChanged(nameof(SanctuaryPreparationProgress));
            if (ritual.HasWarning)
                ExceptionRegister.Record(ritual.Name, ritual.Warning, RitualExceptionReason.Trim());
            if (result.Advance.ScheduleExceptionUsed)
                ExceptionRegister.Record(ritual.Name, "Calendário da preparação dos círculos", RitualExceptionReason.Trim());
            Notebook.AppendLog("Rituais", RitualPreparationSummary);
            MarkChanged();
            ShowDiceRoll(ritual.Name, RitualPreparationSummary, [new RolledDieDisplay(check.Die, "SANGUE")]);
            SanctuaryPreparationConfirmed = false;
            SanctuaryTravelConfirmed = false;
            RitualExceptionApproved = false;
            RitualExceptionReason = "";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }
}
