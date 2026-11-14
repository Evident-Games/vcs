using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Rules.V5.Blood;
using VCS.Rules.V5.Dice;

namespace VCS.Desktop.Sheet;

public partial class CharacterEditorViewModel
{
    [ObservableProperty] private string rouseCheckSummary = "Use a checagem ao ativar um poder ou efeito que a exija.";
    [ObservableProperty] private int rollDicePool = 5;
    [ObservableProperty] private int rollDifficulty = 1;
    [ObservableProperty] private bool useHungerDice = true;
    [ObservableProperty] private bool useBloodSurge;
    [ObservableProperty] private string rollSummary = "Configure a parada e a dificuldade para resolver um teste.";

    [RelayCommand]
    private void RollTest()
    {
        if (RollDicePool is < 0 or > 50 || RollDifficulty is < 0 or > 50)
        {
            RollSummary = "A parada e a dificuldade devem ficar entre 0 e 50.";
            return;
        }

        var surgeDice = UseBloodSurge ? BloodPotencyEffects.At(BloodPotency).BloodSurgeDice : 0;
        var rouseSummary = "";
        if (UseBloodSurge)
        {
            var rouse = RouseCheckRules.Roll(Hunger);
            Hunger = rouse.HungerAfter;
            rouseSummary = rouse.Succeeded ? $"Surto de Sangue: checagem {rouse.Die}, sucesso. " :
                $"Surto de Sangue: checagem {rouse.Die}, falha; Fome {Hunger}. ";
        }

        var pool = RollDicePool + surgeDice;
        var hungerDice = UseHungerDice ? Hunger : 0;
        var result = DicePoolRules.Roll(pool, hungerDice, RollDifficulty);
        var outcome = result.Outcome switch
        {
            TestOutcome.Failure => "falha",
            TestOutcome.Success => "sucesso",
            TestOutcome.CriticalSuccess => "sucesso crítico",
            TestOutcome.MessyCritical => "crítico bestial",
            TestOutcome.BestialFailure => "falha bestial",
            _ => "resultado"
        };
        RollSummary = $"{rouseSummary}{result.Successes} sucesso(s), dificuldade {result.Difficulty}: {outcome}. " +
            $"Dados comuns [{string.Join(", ", result.NormalDice)}]; dados de Fome [{string.Join(", ", result.HungerDice)}].";
    }

    [RelayCommand]
    private void MakeRouseCheck()
    {
        var result = RouseCheckRules.Roll(Hunger);
        Hunger = result.HungerAfter;
        RouseCheckSummary = result.Succeeded
            ? $"Checagem de Sangue: sucesso ({result.Die}). Fome permanece em {Hunger}."
            : result.HungerAfter == result.HungerBefore
                ? $"Checagem de Sangue: falha ({result.Die}). Fome já estava em 5."
                : $"Checagem de Sangue: falha ({result.Die}). Fome aumenta para {Hunger}.";
    }
}
