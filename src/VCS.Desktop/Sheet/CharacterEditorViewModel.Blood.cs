using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Rules.V5.Blood;
using VCS.Rules.V5.Dice;

namespace VCS.Desktop.Sheet;

public partial class CharacterEditorViewModel
{
    public ObservableCollection<RolledDieDisplay> RolledDice { get; } = [];
    public string BloodPotencyRuleSource => VCS.Content.SourceAttribution.ForRule(typeof(BloodPotencyEffects));
    public string GenerationRuleSource => VCS.Content.SourceAttribution.ForRule(typeof(BloodPotencyGenerationRules));
    public string DiceRuleSource => VCS.Content.SourceAttribution.ForRule(typeof(DicePoolRules));
    public string RouseRuleSource => VCS.Content.SourceAttribution.ForRule(typeof(RouseCheckRules));
    public string FeedingRuleSource => VCS.Content.SourceAttribution.ForRule(typeof(FeedingRules));
    public string FrenzyRuleSource => VCS.Content.SourceAttribution.ForRule(typeof(FrenzyRules));
    public string HumanityRuleSource => VCS.Content.SourceAttribution.ForRule(typeof(HumanityRules));
    [ObservableProperty] private string rouseCheckSummary = "Use a checagem ao ativar um poder ou efeito que a exija.";
    [ObservableProperty] private int rollDicePool = 5;
    [ObservableProperty] private int rollDifficulty = 1;
    [ObservableProperty] private bool useHungerDice = true;
    [ObservableProperty] private bool useBloodSurge;
    [ObservableProperty] private bool showDice3D = true;
    [ObservableProperty] private string rollSummary = "Configure a parada e a dificuldade para resolver um teste.";

    [ObservableProperty] private BloodSourceOption selectedBloodSourceOption = BloodSourceOption.All[0];
    [ObservableProperty] private bool feedingAnimalIsLarge;
    [ObservableProperty] private bool feedingAnimalIsExceptional;
    [ObservableProperty] private bool feedingHumanWasDrained;
    [ObservableProperty] private string feedingSummary = "Selecione a fonte de sangue para conferir o quanto ela sacia.";
    public IReadOnlyList<BloodSourceOption> BloodSourceOptions => BloodSourceOption.All;
    public string FeedingPreview
    {
        get
        {
            var result = FeedingRules.Resolve(SelectedBloodSourceOption.Source, BloodPotency, Hunger, Hunger,
                FeedingHumanWasDrained, FeedingAnimalIsLarge, FeedingAnimalIsExceptional);
            var resonance = result.CanProduceResonance ? "pode conceder Ressonância" : "não concede Ressonância";
            var dyscrasia = result.CanProduceDyscrasia ? "pode produzir Discrasia" : "não produz Discrasia";
            return $"{result.RuleNote} Esta alimentação sacia {result.HungerSlaked:0.##} de Fome " +
                $"(Fome {result.HungerBefore:0.##} → {result.HungerAfter:0.##}); {resonance} e {dyscrasia}.";
        }
    }

    partial void OnSelectedBloodSourceOptionChanged(BloodSourceOption value) => OnFeedingInputsChanged();
    partial void OnFeedingAnimalIsLargeChanged(bool value) => OnFeedingInputsChanged();
    partial void OnFeedingAnimalIsExceptionalChanged(bool value) => OnFeedingInputsChanged();
    partial void OnFeedingHumanWasDrainedChanged(bool value) => OnFeedingInputsChanged();

    private void OnFeedingInputsChanged() => OnPropertyChanged(nameof(FeedingPreview));

    [RelayCommand]
    private void ApplyFeeding()
    {
        var result = FeedingRules.Resolve(SelectedBloodSourceOption.Source, BloodPotency, Hunger, Hunger,
            FeedingHumanWasDrained, FeedingAnimalIsLarge, FeedingAnimalIsExceptional);
        if (result.HungerSlaked <= 0)
        {
            FeedingSummary = "Essa alimentação não reduz a Fome atual.";
            return;
        }

        if (decimal.Truncate(result.HungerSlaked) != result.HungerSlaked)
        {
            FeedingSummary = $"A regra resulta em meia Fome ({result.HungerSlaked:0.##}); a trilha atual usa pontos inteiros. A Fome não foi arredondada nem alterada.";
            return;
        }

        Hunger = (int)result.HungerAfter;
        FeedingSummary = $"Alimentação aplicada. Fome agora em {Hunger}. Registre a Ressonância/Discrasia da vítima conforme a cena.";
    }

    [ObservableProperty] private bool frenzyTriggerIsPillarOrLover;
    [ObservableProperty] private FrenzyTriggerOption selectedFrenzyTriggerOption = FrenzyTriggerOption.All[0];
    [ObservableProperty] private string frenzySummary = "Selecione um gatilho para conferir a dificuldade e testar resistência.";
    public IReadOnlyList<FrenzyTriggerOption> FrenzyTriggerOptions => FrenzyTriggerOption.All;
    public string FrenzyTriggerGuidance
    {
        get
        {
            var trigger = FrenzyTriggerRules.Evaluate(SelectedFrenzyTriggerOption.Trigger, Hunger,
                isPresent: true, isPillarOrLover: FrenzyTriggerIsPillarOrLover);
            return trigger.Applies
                ? $"{trigger.Description} Dificuldade {trigger.Difficulty}; Vontade {WillpowerCapacity} + Humanidade/3."
                : $"{trigger.Description} Este gatilho não se aplica com a Fome/condição atual.";
        }
    }

    partial void OnSelectedFrenzyTriggerOptionChanged(FrenzyTriggerOption value) => OnFrenzyInputsChanged();
    partial void OnFrenzyTriggerIsPillarOrLoverChanged(bool value) => OnFrenzyInputsChanged();
    partial void OnHungerChanged(int value)
    {
        OnPropertyChanged(nameof(FrenzyTriggerGuidance));
        OnPropertyChanged(nameof(FeedingPreview));
    }

    private void OnFrenzyInputsChanged() => OnPropertyChanged(nameof(FrenzyTriggerGuidance));
    partial void OnHumanityChanged(int value) => OnPropertyChanged(nameof(FrenzyTriggerGuidance));

    [RelayCommand]
    private void ResistFrenzy()
    {
        var trigger = FrenzyTriggerRules.Evaluate(SelectedFrenzyTriggerOption.Trigger, Hunger,
            isPresent: true, isPillarOrLover: FrenzyTriggerIsPillarOrLover);
        if (!trigger.Applies)
        {
            FrenzySummary = "O gatilho escolhido não se aplica ao estado atual; ajuste a Fome ou a condição e confira com o Narrador.";
            return;
        }

        var result = FrenzyRules.RollResistance(trigger.Type, trigger.Description,
            WillpowerCapacity, trigger.Difficulty, Humanity);
        var roll = result.Roll;
        FrenzySummary = result.Resisted
            ? $"Resistência bem-sucedida: {roll.Successes} sucesso(s) contra dificuldade {trigger.Difficulty}. O vampiro evitou o Frenesi."
            : $"Resistência falhou: {roll.Successes} sucesso(s) contra dificuldade {trigger.Difficulty}. O vampiro entra em Frenesi. Dados: [{string.Join(", ", roll.NormalDice)}].";
    }



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
        RolledDice.Clear();
        foreach (var face in result.NormalDice) RolledDice.Add(new(face, IsHunger: false));
        foreach (var face in result.HungerDice) RolledDice.Add(new(face, IsHunger: true));
        var outcome = result.Outcome switch
        {
            TestOutcome.Failure => "falha",
            TestOutcome.Success => "sucesso",
            TestOutcome.CriticalSuccess => "sucesso crítico",
            TestOutcome.MessyCritical => "crítico bestial",
            TestOutcome.BestialFailure => "falha bestial",
            _ => "resultado"
        };
        var compulsion = result.Outcome switch
        {
            TestOutcome.MessyCritical => " Compulsão de clã desencadeada.",
            TestOutcome.BestialFailure => " Compulsão desencadeada; determine o tipo conforme a mesa.",
            _ => ""
        };
        RollSummary = $"{rouseSummary}{result.Successes} sucesso(s), dificuldade {result.Difficulty}: {outcome}. " +
            $"Dados comuns [{string.Join(", ", result.NormalDice)}]; dados de Fome [{string.Join(", ", result.HungerDice)}].{compulsion}";
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

public sealed record RolledDieDisplay(int Value, bool IsHunger);
