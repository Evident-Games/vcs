using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game.Application.Blood;
using Game.Sheet;

namespace Game.Desktop.Sheet;

public partial class CharacterEditorViewModel
{
    public IReadOnlyList<int> SanctuaryCircles { get; } = [1, 2];
    [ObservableProperty] private int sanctuaryCircle = 1;
    [ObservableProperty] private DateTime? sanctuaryNight;
    [ObservableProperty] private bool sanctuaryPreparationConfirmed;
    [ObservableProperty] private bool sanctuaryResetConfirmed;
    [ObservableProperty] private bool sanctuaryCircleDamageConfirmed;
    [ObservableProperty] private string sanctuaryDepartureLocation = "";
    [ObservableProperty] private string sanctuaryDestinationLocation = "";
    [ObservableProperty] private bool sanctuaryLocationsConfirmed;

    private RitualPreparationStep[] SanctuaryPaidChecks() => PowerEffects.Where(x => x.Active && x.IsSelf &&
            x.PowerId == "ritual-sanctuary-preparation-check" && x.PreparationStep is not null)
        .Select(x => x.PreparationStep!).ToArray();

    private PowerEffectEntry? SanctuaryPairEntry() => PowerEffects.LastOrDefault(x => x.Active && x.IsSelf &&
        x.PowerId == "ritual-sanctuary-circle-pair" && x.CirclePair is not null);

    private RitualCirclePair? SanctuaryActivePair() => SanctuaryPairEntry()?.CirclePair;

    private bool SanctuaryPairWasDamaged()
    {
        var lastDamage = PowerEffects.Where(x => x.PowerId == "ritual-sanctuary-circle-damage")
            .Select(x => (DateTime?)x.CreatedUtc).Max();
        if (lastDamage is null) return false;
        var lastNewPreparation = PowerEffects.Where(x => x.IsSelf &&
                x.PowerId == "ritual-sanctuary-preparation-check" && x.PreparationStep is not null)
            .Select(x => (DateTime?)x.CreatedUtc).Max();
        return lastNewPreparation is null || lastNewPreparation <= lastDamage;
    }

    public string SanctuaryPreparationProgress
    {
        get
        {
            var paid = SanctuaryPaidChecks();
            var damage = SanctuaryPairWasDamaged() ? " · último par inerte por dano confirmado" : "";
            var pair = SanctuaryActivePair();
            var locations = pair is null ? "locais ainda não registrados" : $"{pair.Departure} → {pair.Destination}";
            return $"Partida: {paid.Count(x => x.Circle == 1)}/6 checagens · Chegada: {paid.Count(x => x.Circle == 2)}/6 checagens · {locations}{damage}";
        }
    }

    partial void OnSanctuaryDepartureLocationChanged(string value)
    {
        SanctuaryLocationsConfirmed = false;
        SanctuaryPreparationConfirmed = false;
        SanctuaryTravelConfirmed = false;
    }

    partial void OnSanctuaryDestinationLocationChanged(string value)
    {
        SanctuaryLocationsConfirmed = false;
        SanctuaryPreparationConfirmed = false;
        SanctuaryTravelConfirmed = false;
    }

    [RelayCommand]
    private void RegisterSanctuaryCirclePair()
    {
        if (SelectedPreparedRitual?.Entry?.Definition is not
            { Id: "ritual-escape-to-true-sanctuary", IsRitual: true })
        { RitualPreparationSummary = "Selecione Fuga para o Verdadeiro Santuário nos rituais desta ficha."; return; }
        try
        {
            var result = new TrueSanctuaryActions().RegisterCirclePair(PowerEffects,
                SanctuaryDepartureLocation, SanctuaryDestinationLocation, SanctuaryLocationsConfirmed,
                RitualExceptionApproved && !string.IsNullOrWhiteSpace(RitualExceptionReason));
            var pair = result.Pair;
            if (result.EndedPair is { } ended && PowerEffects.FirstOrDefault(x => x.Id == ended.Id) is { } currentPair)
                ReplacePowerEffect(currentPair, ended);
            foreach (var updated in result.UpdatedPreparation)
                if (PowerEffects.FirstOrDefault(x => x.Id == updated.Id) is { } currentPreparation)
                    ReplacePowerEffect(currentPreparation, updated with
                    {
                        Ruling = result.CorrectionUsed
                            ? string.Join(" | ", new[] { updated.Ruling, RitualExceptionReason.Trim() }
                                .Where(x => !string.IsNullOrWhiteSpace(x)))
                            : updated.Ruling
                    });
            var summary = $"Par do Santuário registrado: partida em {pair.Departure}; chegada em {pair.Destination}.";
            PowerEffects.Add(new PowerEffectEntry(Guid.NewGuid(), "ritual-sanctuary-circle-pair",
                "Fuga para o Verdadeiro Santuário", $"{pair.Departure} → {pair.Destination}", true,
                DateTime.UtcNow, summary, "Enquanto este par permanecer ativo",
                "Vampiro: A Máscara V5 (Livro base), p. 281",
                result.CorrectionUsed ? RitualExceptionReason.Trim() : "", true,
                CirclePair: pair));
            if (result.CorrectionUsed)
                ExceptionRegister.Record("Fuga para o Verdadeiro Santuário",
                    "Correção dos locais do par de círculos", RitualExceptionReason.Trim());
            SanctuaryDepartureLocation = pair.Departure;
            SanctuaryDestinationLocation = pair.Destination;
            SanctuaryLocationsConfirmed = false;
            RitualExceptionApproved = false;
            RitualExceptionReason = "";
            RitualPreparationSummary = summary + " Nenhum personagem foi movido.";
            OnPropertyChanged(nameof(SanctuaryPreparationProgress));
            Notebook.AppendLog("Rituais", RitualPreparationSummary);
            MarkChanged();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void InvalidateSanctuaryPairAfterDamage()
    {
        try
        {
            var result = new TrueSanctuaryActions().InvalidatePairAfterCircleDamage(PowerEffects,
                SanctuaryCircle, SanctuaryCircleDamageConfirmed);
            foreach (var ended in result.EndedEntries)
                if (PowerEffects.FirstOrDefault(x => x.Id == ended.Id) is { } current)
                    ReplacePowerEffect(current, ended);
            PowerEffects.Add(new PowerEffectEntry(Guid.NewGuid(), "ritual-sanctuary-circle-damage",
                "Fuga para o Verdadeiro Santuário", $"Círculo {SanctuaryCircle}", true, DateTime.UtcNow,
                result.Summary, "Par de círculos inerte", "Vampiro: A Máscara V5 (Livro base), p. 281", "", false));
            SanctuaryCircleDamageConfirmed = false;
            SanctuaryLocationsConfirmed = false;
            SanctuaryPreparationConfirmed = false;
            SanctuaryTravelConfirmed = false;
            OnPropertyChanged(nameof(SanctuaryPreparationProgress));
            RitualPreparationSummary = result.Summary + " Custos anteriores permaneceram no histórico.";
            Notebook.AppendLog("Rituais", RitualPreparationSummary);
            MarkChanged();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void ResetSanctuaryPreparation()
    {
        if (!SanctuaryResetConfirmed)
        { RitualPreparationSummary = "Confirme com o Narrador o abandono ou a inutilização do par de círculos."; return; }
        foreach (var entry in PowerEffects.Where(x => x.Active && x.IsSelf &&
            (x.PowerId == "ritual-sanctuary-preparation-check" ||
             x.PowerId == "ritual-sanctuary-circle-pair")).ToArray())
            ReplacePowerEffect(entry, entry with { Active = false });
        SanctuaryResetConfirmed = false;
        SanctuaryCircleDamageConfirmed = false;
        SanctuaryLocationsConfirmed = false;
        SanctuaryPreparationConfirmed = false;
        SanctuaryTravelConfirmed = false;
        OnPropertyChanged(nameof(SanctuaryPreparationProgress));
        RitualPreparationSummary = "Preparação do par de círculos encerrada pela mesa. Custos anteriores permanecem no histórico; nenhuma Fome foi devolvida.";
        Notebook.AppendLog("Rituais", RitualPreparationSummary);
        MarkChanged();
    }

    partial void OnSanctuaryCircleChanged(int value)
    {
        SanctuaryPreparationConfirmed = false;
        SanctuaryCircleDamageConfirmed = false;
    }
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
            var pair = SanctuaryActivePair() ?? throw new InvalidOperationException(
                "Registre e confirme os locais dos círculos de partida e chegada antes de pagar a preparação.");
            var result = new TrueSanctuaryActions().PrepareNextCheck(SanctuaryCircle, DateOnly.FromDateTime(night),
                paid, pair, Hunger, SanctuaryPreparationConfirmed, RitualExceptionApproved);
            var step = result.Advance.Step;
            var check = result.Check;
            Hunger = check.HungerAfter;
            RitualPreparationSummary = $"Santuário, círculo {step.Circle} em {step.Location}, noite {(step.Sequence + 1) / 2}/3, " +
                $"checagem {(step.Sequence - 1) % 2 + 1}/2 em {step.NightDate:dd/MM/yyyy}: dado {check.Die}; Fome {Hunger}. " +
                (step.Sequence == 6 ? "As seis checagens deste círculo foram pagas. " : "Preparação ainda em andamento. ") +
                "Fonte: Livro base, p. 281.";
            PowerEffects.Add(new PowerEffectEntry(Guid.NewGuid(), "ritual-sanctuary-preparation-check", ritual.Name,
                $"Círculo {step.Circle}: {step.Location}", true, DateTime.UtcNow, RitualPreparationSummary,
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
