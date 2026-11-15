using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Application.Blood;
using VCS.Desktop.Catalogs;

namespace VCS.Desktop.Sheet;

public partial class CharacterEditorViewModel
{
    [ObservableProperty] private PowerRowViewModel? selectedPreparedRitual;
    [ObservableProperty] private bool ritualConditionsConfirmed;
    [ObservableProperty] private string ritualPreparationSummary = "Selecione um ritual conhecido e confira suas condições.";

    partial void OnSelectedPreparedRitualChanged(PowerRowViewModel? value)
    {
        RitualConditionsConfirmed = false;
        RitualPreparationSummary = "Confira novamente os ingredientes, o alvo e as condições deste ritual.";
    }

    [RelayCommand]
    private void PrepareRitual()
    {
        if (SelectedPreparedRitual is not { } ritual) { RitualPreparationSummary = "Selecione um ritual."; return; }
        if (!RitualConditionsConfirmed) { RitualPreparationSummary = "Confirme ingredientes, alvo e condições com o Narrador."; return; }
        if (ritual.HasWarning) { RitualPreparationSummary = ritual.Warning + " Registre a exceção na ficha antes de usar o fluxo manual."; return; }
        if (ritual.Entry?.Definition is not { IsRitual: true } definition)
        { RitualPreparationSummary = "Ritual próprio: resolva custos e teste manualmente com o Narrador."; return; }
        var deferred = definition.Id is "ritual-ward-against-lupines" or "ritual-ward-against-cainites" or "ritual-wake-with-evenings-freshness";
        try
        {
            var pool = Math.Clamp(AttributeRating("intelligence") + DisciplineRating("discipline-blood-sorcery"), 0, 50);
            var result = new RitualActions().Prepare(definition.Level, Hunger, pool, deferred);
            Hunger = result.Check.HungerAfter;
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
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { RitualPreparationSummary = exception.Message; }
    }
}
