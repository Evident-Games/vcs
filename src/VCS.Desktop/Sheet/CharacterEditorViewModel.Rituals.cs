using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Application.Blood;
using VCS.Desktop.Catalogs;
using VCS.Sheet;

namespace VCS.Desktop.Sheet;

public partial class CharacterEditorViewModel
{
    [ObservableProperty] private PowerRowViewModel? selectedPreparedRitual;
    [ObservableProperty] private bool ritualConditionsConfirmed;
    [ObservableProperty] private bool ritualExceptionApproved;
    [ObservableProperty] private string ritualExceptionReason = "";
    [ObservableProperty] private bool awakeningDangerConfirmed;
    [ObservableProperty] private string ritualPreparationSummary = "Selecione um ritual conhecido e confira suas condições.";

    partial void OnSelectedPreparedRitualChanged(PowerRowViewModel? value)
    {
        RitualConditionsConfirmed = false;
        RitualExceptionApproved = false;
        RitualExceptionReason = "";
        AwakeningDangerConfirmed = false;
        RitualPreparationSummary = "Confira novamente os ingredientes, o alvo e as condições deste ritual.";
    }

    [RelayCommand]
    private void PrepareRitual()
    {
        if (SelectedPreparedRitual is not { } ritual) { RitualPreparationSummary = "Selecione um ritual."; return; }
        if (!RitualConditionsConfirmed) { RitualPreparationSummary = "Confirme ingredientes, alvo e condições com o Narrador."; return; }
        if (ritual.HasWarning && !RitualExceptionApproved)
        { RitualPreparationSummary = ritual.Warning + " Autorize e justifique a exceção para continuar."; return; }
        if (RitualExceptionApproved && string.IsNullOrWhiteSpace(RitualExceptionReason))
        { RitualPreparationSummary = "Informe a decisão do Narrador para registrar a exceção."; return; }
        if (ritual.Entry?.Definition is not { IsRitual: true } definition)
        { RitualPreparationSummary = "Ritual próprio: resolva custos e teste manualmente com o Narrador."; return; }
        var deferred = definition.Id is "ritual-ward-against-lupines" or "ritual-ward-against-cainites" or "ritual-wake-with-evenings-freshness";
        try
        {
            var pool = Math.Clamp(AttributeRating("intelligence") + DisciplineRating("discipline-blood-sorcery"), 0, 50);
            var result = new RitualActions().Prepare(definition.Level, Hunger, pool, deferred);
            Hunger = result.Check.HungerAfter;
            if (RitualExceptionApproved)
                ExceptionRegister.Record(ritual.Name, ritual.HasWarning ? ritual.Warning : "Exceção ao ritual", RitualExceptionReason.Trim());
            if (definition.Id == "ritual-wake-with-evenings-freshness")
            {
                foreach (var previous in PowerEffects.Where(x => x.Active && x.IsSelf && x.PowerId == definition.Id).ToArray())
                    ReplacePowerEffect(previous, previous with { Active = false });
                var pending = new PowerEffectEntry(Guid.NewGuid(), definition.Id, ritual.Name, Name, true,
                    DateTime.UtcNow, "Preparado; aguardando perigo durante o sono.", "Até o fim do sono diurno", definition.Source, "", true);
                PowerEffects.Add(pending);
                SelectedPowerEffect = pending;
                MarkChanged();
            }
            RitualPreparationSummary = $"{ritual.Name}: {result.Requirements.PreparationMinutes} min; dificuldade padrão {result.Requirements.Difficulty}; Fome {Hunger}. " +
                (result.Roll is { } roll ? $"{roll.Successes} sucesso(s): {roll.Outcome}. " :
                    definition.Id == "ritual-wake-with-evenings-freshness"
                        ? "Teste adiado até surgir perigo durante o sono; resolva o despertar com o Narrador. "
                        : "Proteção: teste adiado até o primeiro contato; registre o objeto e resolva o efeito com o Narrador. ") +
                $"Fonte: {definition.Source}. Confira exceções específicas do ritual.";
            Notebook.AppendLog("Rituais", RitualPreparationSummary);
            var dice = new List<RolledDieDisplay> { new(result.Check.Die, "SANGUE") };
            if (result.Roll is { } rolled)
            {
                dice.AddRange(rolled.NormalDice.Select(x => new RolledDieDisplay(x, "COMUM")));
                dice.AddRange(rolled.HungerDice.Select(x => new RolledDieDisplay(x, "FOME")));
            }
            ShowDiceRoll(ritual.Name, RitualPreparationSummary, dice);
            RitualConditionsConfirmed = false;
            RitualExceptionApproved = false;
            RitualExceptionReason = "";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }

    [RelayCommand]
    private void ResolveAwakeningDanger()
    {
        var pending = PowerEffects.LastOrDefault(x => x.Active && x.IsSelf && x.PowerId == "ritual-wake-with-evenings-freshness");
        if (pending is null) { RitualPreparationSummary = "Nenhum ritual de despertar preparado e ativo nesta ficha."; return; }
        if (!AwakeningDangerConfirmed) { RitualPreparationSummary = "Confirme que há perigo durante o sono com o Narrador."; return; }
        var pool = Math.Clamp(AttributeRating("intelligence") + DisciplineRating("discipline-blood-sorcery"), 0, 50);
        var roll = new RitualActions().ResolveDeferred(1, Hunger, pool);
        RitualPreparationSummary = $"Despertar: {roll.Successes} sucesso(s), dificuldade {roll.Difficulty}: {roll.Outcome}. " +
            (roll.IsSuccess ? "Desperta alerta; durante a cena pode agir sem a penalidade de atividade diurna conforme o ritual." : "O ritual não desperta o personagem; o Narrador decide as consequências.") +
            " Fonte: Vampiro: A Máscara V5 (Livro base), p. 276.";
        ReplacePowerEffect(pending, pending with { Active = false, Effect = RitualPreparationSummary });
        Notebook.AppendLog("Rituais", RitualPreparationSummary);
        ShowDiceRoll("Ritual de despertar", RitualPreparationSummary,
            roll.NormalDice.Select(x => new RolledDieDisplay(x, "COMUM")).Concat(roll.HungerDice.Select(x => new RolledDieDisplay(x, "FOME"))));
        AwakeningDangerConfirmed = false;
        MarkChanged();
    }

    [RelayCommand]
    private void EndAwakeningSleep()
    {
        var pending = PowerEffects.LastOrDefault(x => x.Active && x.IsSelf && x.PowerId == "ritual-wake-with-evenings-freshness");
        if (pending is null) { RitualPreparationSummary = "Nenhum ritual de despertar pendente nesta ficha."; return; }
        RitualPreparationSummary = "Sono diurno encerrado; ritual de despertar pendente encerrado sem rolagem.";
        ReplacePowerEffect(pending, pending with { Active = false, Effect = RitualPreparationSummary });
        AwakeningDangerConfirmed = false;
        Notebook.AppendLog("Rituais", RitualPreparationSummary);
        MarkChanged();
    }
}
