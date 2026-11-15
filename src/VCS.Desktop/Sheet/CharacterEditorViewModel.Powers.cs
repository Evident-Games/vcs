using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Desktop.Catalogs;

namespace VCS.Desktop.Sheet;

public partial class CharacterEditorViewModel
{
    [ObservableProperty] private PowerRowViewModel? activationPower;
    [ObservableProperty] private int activationRouseChecks;
    [ObservableProperty] private int activationWillpowerCost;
    [ObservableProperty] private string activationTarget = "";
    [ObservableProperty] private string activationEffect = "";
    [ObservableProperty] private bool activationExceptionApproved;
    [ObservableProperty] private string activationSummary = "Selecione um poder conhecido e confira os custos da ativação.";

    [RelayCommand]
    private void ActivatePower()
    {
        if (ActivationPower is not { } power) { ActivationSummary = "Selecione um poder."; return; }
        if (power.HasWarning && !ActivationExceptionApproved)
        {
            ActivationSummary = power.Warning + " Marque a autorização do Narrador para esta exceção.";
            return;
        }
        try
        {
            var result = new VCS.Application.Blood.PowerActivationActions().Activate(Hunger, BloodPotency, power.Level,
                ActivationRouseChecks, ActivationWillpowerCost, WillpowerSuperficial, WillpowerAggravated, WillpowerCapacity);
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
