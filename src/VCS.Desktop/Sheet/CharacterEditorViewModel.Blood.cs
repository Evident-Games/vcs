using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Rules.V5.Dice;

namespace VCS.Desktop.Sheet;

public partial class CharacterEditorViewModel
{
    [ObservableProperty] private string rouseCheckSummary = "Use a checagem quando ativar um poder ou efeito que a exigir.";

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
