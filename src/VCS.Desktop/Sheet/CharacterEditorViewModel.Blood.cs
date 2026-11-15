using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Application.Blood;
using VCS.Content;
using VCS.Rules.V5.Blood;
using VCS.Rules.V5.Dice;

namespace VCS.Desktop.Sheet;

public partial class CharacterEditorViewModel
{
    public ObservableCollection<RolledDieDisplay> RolledDice { get; } = [];
    public ObservableCollection<RerollDieOption> RerollableDice { get; } = [];
    public bool HasRolledDice => RolledDice.Count > 0;
    public bool HasRollDisplay => HasRolledDice || !string.IsNullOrWhiteSpace(LastRollSummary);
    public bool CanReroll => _lastRollForReroll is not null && !_rerollUsed &&
        WillpowerSuperficial + WillpowerAggravated < WillpowerCapacity && RerollableDice.Any(die => die.IsSelected);
    private DicePoolResult? _lastRollForReroll;
    private int _lastRollPool;
    private int _lastRollHunger;
    private int _lastRollDifficulty;
    private bool _rerollUsed;

    partial void OnWillpowerSuperficialChanged(int value)
    {
        OnPropertyChanged(nameof(CanReroll));
        OnPropertyChanged(nameof(CanApplyConflictDamage));
    }
    partial void OnWillpowerAggravatedChanged(int value)
    {
        OnPropertyChanged(nameof(CanReroll));
        OnPropertyChanged(nameof(CanApplyConflictDamage));
    }

    private void ShowDiceRoll(string label, string summary, IEnumerable<RolledDieDisplay> dice, bool preserveReroll = false)
    {
        RolledDice.Clear();
        if (!preserveReroll)
        {
            RerollableDice.Clear();
            _lastRollForReroll = null;
            _rerollUsed = false;
        }
        foreach (var die in dice) RolledDice.Add(die);
        LastRollLabel = label;
        LastRollSummary = summary;
        OnPropertyChanged(nameof(HasRolledDice));
        OnPropertyChanged(nameof(HasRollDisplay));
        OnPropertyChanged(nameof(CanReroll));
    }

    [RelayCommand]
    private void ClearRolledDice()
    {
        RolledDice.Clear();
        RerollableDice.Clear();
        _lastRollForReroll = null;
        _rerollUsed = false;
        LastRollLabel = "";
        LastRollSummary = "";
        OnPropertyChanged(nameof(HasRolledDice));
        OnPropertyChanged(nameof(HasRollDisplay));
        OnPropertyChanged(nameof(CanReroll));
    }
    public string BloodPotencyRuleSource => VCS.Content.SourceAttribution.ForRule(typeof(BloodPotencyEffects));
    public string GenerationRuleSource => VCS.Content.SourceAttribution.ForRule(typeof(BloodPotencyGenerationRules));
    public string DiceRuleSource => VCS.Content.SourceAttribution.ForRule(typeof(DicePoolRules));
    public string RouseRuleSource => VCS.Content.SourceAttribution.ForRule(typeof(RouseCheckRules));
    public string ResonanceRuleSource => VCS.Content.SourceAttribution.ForRule(typeof(ResonanceRules));
    public string MendingRuleSource => VCS.Content.SourceAttribution.ForRule(typeof(BloodPotencyEffects));
    public string FeedingRuleSource => VCS.Content.SourceAttribution.ForRule(typeof(FeedingRules));
    public string FrenzyRuleSource => VCS.Content.SourceAttribution.ForRule(typeof(FrenzyRules));
    public string HumanityRuleSource => VCS.Content.SourceAttribution.ForRule(typeof(HumanityRules));
    [ObservableProperty] private string rouseCheckSummary = "Use a checagem ao ativar um poder ou efeito que a exija.";
    [ObservableProperty] private int rollDicePool = 5;
    [ObservableProperty] private int rollDifficulty = 1;
    [ObservableProperty] private int rollSituationalModifier;
    [ObservableProperty] private string rollModifierSource = "Circunstância da cena / decisão do Narrador";
    [ObservableProperty] private TraitItemViewModel? selectedRollAttribute;
    [ObservableProperty] private TraitItemViewModel? selectedRollSkill;
    [ObservableProperty] private bool useRollSpecialization;
    [ObservableProperty] private bool useResonanceBonus;
    [ObservableProperty] private bool useSheetDicePool;
    [ObservableProperty] private bool useHungerDice = true;
    [ObservableProperty] private bool useBloodSurge;
    [ObservableProperty] private string lastRollLabel = "";
    [ObservableProperty] private string lastRollSummary = "";
    [ObservableProperty] private string rollSummary = "Configure a parada e a dificuldade para resolver um teste.";

    public IReadOnlyList<TraitItemViewModel> RollAttributes => Attributes.Items;
    public IReadOnlyList<TraitItemViewModel> RollSkills => Skills.Items;
    public bool CanApplyRollSpecialization => SelectedRollSkill?.SpecializationCount > 0;
    public bool CanUseResonanceBonus => HasActiveResonanceBonus &&
        (UseSheetDicePool ? RollSituationalModifier < 50 : RollDicePool < 50);
    public DicePoolComposition RollPoolComposition => _diceActions.BuildPool(
        SelectedRollAttribute?.Rating ?? 0, SelectedRollSkill?.Rating ?? 0, EffectiveSituationalModifier,
        UseRollSpecialization && CanApplyRollSpecialization);
    private int EffectiveSituationalModifier => RollSituationalModifier + (UseResonanceBonus && CanUseResonanceBonus ? 1 : 0);
    public int CurrentRollDicePool => UseSheetDicePool ? RollPoolComposition.DicePool : Math.Max(0, RollDicePool + (UseResonanceBonus && CanUseResonanceBonus ? 1 : 0));
    public string RollPoolSummary => UseSheetDicePool
        ? $"Parada montada: {RollPoolComposition.Formula}. Origem do ajuste: {RollModifierSource}{ResonanceBonusSummary}."
        : $"Parada manual. Ajustes e exceções registrados: {RollModifierSource}{ResonanceBonusSummary}.";
    private string ResonanceBonusSummary => UseResonanceBonus && CanUseResonanceBonus
        ? " · +1 dado de Ressonância/Discrasia marcado para este teste"
        : "";
    private bool HasActiveResonanceBonus =>
        Extra("Resonance").Contains("Intensa", StringComparison.OrdinalIgnoreCase) ||
        Extra("Resonance").Contains("Aguda", StringComparison.OrdinalIgnoreCase) ||
        !string.IsNullOrWhiteSpace(Extra("Dyscrasia"));

    partial void OnSelectedRollAttributeChanged(TraitItemViewModel? value) => RefreshRollPool();
    partial void OnSelectedRollSkillChanged(TraitItemViewModel? value)
    {
        if (value?.SpecializationCount is not > 0) UseRollSpecialization = false;
        OnPropertyChanged(nameof(CanApplyRollSpecialization));
        RefreshRollPool();
    }
    partial void OnRollSituationalModifierChanged(int value)
    {
        if (value >= 50) UseResonanceBonus = false;
        RefreshRollPool();
    }
    partial void OnRollModifierSourceChanged(string value) => OnPropertyChanged(nameof(RollPoolSummary));
    partial void OnUseRollSpecializationChanged(bool value) => RefreshRollPool();
    partial void OnUseResonanceBonusChanged(bool value) => RefreshRollPool();
    partial void OnUseSheetDicePoolChanged(bool value) => RefreshRollPool();
    partial void OnRollDicePoolChanged(int value)
    {
        if (value >= 50) UseResonanceBonus = false;
        OnPropertyChanged(nameof(CurrentRollDicePool));
    }

    private void RefreshRollPool()
    {
        OnPropertyChanged(nameof(RollPoolComposition));
        OnPropertyChanged(nameof(CurrentRollDicePool));
        OnPropertyChanged(nameof(RollPoolSummary));
        OnPropertyChanged(nameof(CanApplyRollSpecialization));
        OnPropertyChanged(nameof(CanUseResonanceBonus));
    }

    [ObservableProperty] private BloodSourceOption selectedBloodSourceOption = BloodSourceOption.All[0];
    [ObservableProperty] private bool feedingAnimalIsLarge;
    [ObservableProperty] private bool feedingAnimalIsExceptional;
    [ObservableProperty] private bool feedingHumanWasDrained;
    [ObservableProperty] private string feedingSummary = "Selecione a fonte de sangue para conferir o quanto ela sacia.";
    [ObservableProperty] private string resonanceSummary = "Ressonância é determinada pela fonte e intensidade do sangue; a decisão sobre Discrasia cabe à mesa.";
    [ObservableProperty] private bool feedingResonanceAvailable;
    private BloodSource _pendingResonanceSource;
    private bool _pendingResonanceIsExceptionalAnimal;
    public bool CanRollResonance => FeedingResonanceAvailable && Hunger > 0;
    public IReadOnlyList<BloodSourceOption> BloodSourceOptions => BloodSourceOption.All;
    public string FeedingPreview
    {
        get
        {
            var result = _bloodActions.PreviewFeeding(SelectedBloodSourceOption.Source, BloodPotency, Hunger,
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
        FeedingResonanceAvailable = false;
        // A próxima alimentação encerra os benefícios de Ressonância/Discrasia anteriores.
        _extraFields["Resonance"].Value = "";
        _extraFields["Dyscrasia"].Value = "";
        var result = _bloodActions.ApplyFeeding(SelectedBloodSourceOption.Source, BloodPotency, Hunger,
            FeedingHumanWasDrained, FeedingAnimalIsLarge, FeedingAnimalIsExceptional);
        if (result.Status == FeedingApplicationStatus.NoHungerSlaked)
        {
            FeedingSummary = "Essa alimentação não reduz a Fome atual.";
            return;
        }

        if (result.Status == FeedingApplicationStatus.FractionalHunger)
        {
            FeedingSummary = $"A regra resulta em meia Fome ({result.Resolution.HungerSlaked:0.##}); a trilha atual usa pontos inteiros. A Fome não foi arredondada nem alterada.";
            return;
        }

        Hunger = result.HungerAfter;
        FeedingResonanceAvailable = result.Resolution.CanProduceResonance && Hunger > 0;
        if (FeedingResonanceAvailable)
        {
            _pendingResonanceSource = result.Resolution.Source;
            _pendingResonanceIsExceptionalAnimal = FeedingAnimalIsExceptional;
        }
        FeedingSummary = $"Alimentação aplicada. Fome agora em {Hunger}. Registre a Ressonância/Discrasia da vítima conforme a cena.";
    }

    partial void OnFeedingResonanceAvailableChanged(bool value)
    {
        OnPropertyChanged(nameof(CanRollResonance));
        RollResonanceCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRollResonance))]
    private void RollResonance()
    {
        if (!CanRollResonance) return;
        FeedingResonanceAvailable = false;

        var isHumanBlood = _pendingResonanceSource == BloodSource.Human;
        var result = _bloodActions.RollResonance(_pendingResonanceSource, isHumanBlood,
            _pendingResonanceSource == BloodSource.Animal && _pendingResonanceIsExceptionalAnimal);
        var resonance = result.Resolution.Resonance switch
        {
            BloodResonance.Choleric => "Colérica",
            BloodResonance.Melancholic => "Melancólica",
            BloodResonance.Phlegmatic => "Fleumática",
            BloodResonance.Sanguine => "Sanguínea",
            BloodResonance.Animal => "Animal",
            _ => result.Resolution.Resonance.ToString()
        };
        var intensity = result.Resolution.Intensity switch
        {
            ResonanceIntensity.Balanced => "equilibrada",
            ResonanceIntensity.Fleeting => "passageira",
            ResonanceIntensity.Intense => "intensa",
            ResonanceIntensity.Acute => "aguda",
            ResonanceIntensity.Dyscrasia => "discrasia",
            _ => result.Resolution.Intensity.ToString()
        };
        var disciplines = string.Join(" e ", result.Resolution.Disciplines);
        var bonus = result.Resolution.BonusDice > 0 ? $" concede +{result.Resolution.BonusDice} dado em {disciplines}" : " não concede dado extra";
        var dyscrasia = result.Resolution.CanProduceDyscrasia
            ? " A mesa pode registrar uma Discrasia conforme a cena."
            : " Esta fonte não pode produzir Discrasia.";
        ResonanceSummary = $"{resonance}, intensidade {intensity}:{bonus}.{dyscrasia} Dados: [{string.Join(", ", result.Dice)}].";
        _extraFields["Resonance"].Value = $"{resonance}; {intensity}; {disciplines}; expira ao chegar a Fome 0 ou ao alimentar-se novamente.";
        ShowDiceRoll("Ressonância", ResonanceSummary,
            result.Dice.Select(face => new RolledDieDisplay(face, "RESSONÂNCIA")));
    }

    [ObservableProperty] private bool frenzyTriggerIsPillarOrLover;
    [ObservableProperty] private FrenzyTriggerOption selectedFrenzyTriggerOption = FrenzyTriggerOption.All[0];
    [ObservableProperty] private string frenzySummary = "Selecione um gatilho para conferir a dificuldade e testar resistência.";
    public IReadOnlyList<FrenzyTriggerOption> FrenzyTriggerOptions => FrenzyTriggerOption.All;
    public string FrenzyTriggerGuidance
    {
        get
        {
            var trigger = _bloodActions.EvaluateFrenzyTrigger(SelectedFrenzyTriggerOption.Trigger, Hunger,
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
        OnPropertyChanged(nameof(CanRollResonance));
        RollResonanceCommand.NotifyCanExecuteChanged();
        if (value == 0)
        {
            FeedingResonanceAvailable = false;
            _extraFields["Resonance"].Value = "";
        }
        if (value == 5) _extraFields["Dyscrasia"].Value = "";
    }

    private void OnFrenzyInputsChanged() => OnPropertyChanged(nameof(FrenzyTriggerGuidance));
    partial void OnHumanityChanged(int value) => OnPropertyChanged(nameof(FrenzyTriggerGuidance));

    [RelayCommand]
    private void ResistFrenzy()
    {
        var result = _bloodActions.ResistFrenzy(SelectedFrenzyTriggerOption.Trigger, Hunger,
            WillpowerCapacity, Humanity, isPresent: true, isPillarOrLover: FrenzyTriggerIsPillarOrLover);
        if (result.Resistance is not { } resistance)
        {
            FrenzySummary = "O gatilho escolhido não se aplica ao estado atual; ajuste a Fome ou a condição e confira com o Narrador.";
            ShowDiceRoll("Resistência ao Frenesi", FrenzySummary, []);
            return;
        }

        var roll = resistance.Roll;
        FrenzySummary = resistance.Resisted
            ? $"Resistência bem-sucedida: {roll.Successes} sucesso(s) contra dificuldade {resistance.Difficulty}. O vampiro evitou o Frenesi."
            : $"Resistência falhou: {roll.Successes} sucesso(s) contra dificuldade {resistance.Difficulty}. O vampiro entra em Frenesi. Dados: [{string.Join(", ", roll.NormalDice)}].";
        ShowDiceRoll("Resistência ao Frenesi", FrenzySummary, roll.NormalDice.Select(face => new RolledDieDisplay(face, "FRENESI")));
    }



    [RelayCommand]
    private void RollTest()
    {
        if (CurrentRollDicePool is < 0 or > 50 || RollDifficulty is < 0 or > 50)
        {
            RollSummary = "A parada e a dificuldade devem ficar entre 0 e 50.";
            ShowDiceRoll("Teste V5", RollSummary, []);
            return;
        }

        var rouseSummary = "";
        var displayedDice = new List<RolledDieDisplay>();
        var action = _bloodActions.RollTest(CurrentRollDicePool, RollDifficulty, Hunger, BloodPotency,
            UseHungerDice, UseBloodSurge);
        if (action.BloodSurgeCheck is { } rouse)
        {
            Hunger = action.HungerAfter;
            displayedDice.Add(new(rouse.Die, "SURTO"));
            rouseSummary = rouse.Succeeded ? $"Surto de Sangue: checagem {rouse.Die}, sucesso. " :
                $"Surto de Sangue: checagem {rouse.Die}, falha; Fome {Hunger}. ";
        }

        var result = action.Roll;
        _lastRollForReroll = result;
        _lastRollPool = CurrentRollDicePool + action.BloodSurgeDice;
        _lastRollHunger = UseHungerDice ? action.HungerAfter : 0;
        _lastRollDifficulty = RollDifficulty;
        _rerollUsed = false;
        RerollableDice.Clear();
        for (var index = 0; index < result.NormalDice.Count; index++)
        {
            var face = result.NormalDice[index];
            if (face < 6) AddRerollOption(index, face);
        }
        OnPropertyChanged(nameof(CanReroll));
        displayedDice.AddRange(result.NormalDice.Select(face => new RolledDieDisplay(face, "COMUM")));
        displayedDice.AddRange(result.HungerDice.Select(face => new RolledDieDisplay(face, "FOME")));
        var outcome = result.Outcome switch
        {
            TestOutcome.Failure => "falha",
            TestOutcome.Success => "sucesso",
            TestOutcome.CriticalSuccess => "sucesso crítico",
            TestOutcome.MessyCritical => "crítico bestial",
            TestOutcome.BestialFailure => "falha bestial",
            _ => "resultado"
        };
        var compulsion = "";
        if (CompulsionRules.IsTriggered(result.Outcome))
        {
            var isCaitiffOrThinBlood = Generation >= 14 ||
                CatalogText.Normalize(Clan).Contains("caitiff", StringComparison.Ordinal) ||
                CatalogText.Normalize(Clan).Contains("sangue ralo", StringComparison.Ordinal);
            var resolution = _bloodActions.ResolveCompulsion(result.Outcome, isCaitiffOrThinBlood);
            displayedDice.AddRange(resolution.TableRolls.Select(face => new RolledDieDisplay(face, "COMPULSÃO")));
            var type = resolution.Compulsion.Type switch
            {
                CompulsionType.Hunger => "Fome",
                CompulsionType.Dominance => "Dominância",
                CompulsionType.Harm => "Dano",
                CompulsionType.Paranoia => "Paranoia",
                CompulsionType.Clan => "de Clã",
                _ => ""
            };
            compulsion = resolution.Compulsion.Type == CompulsionType.Clan
                ? $" Compulsão de Clã: registre a escolha do Narrador para {Clan}."
                : $" Compulsão: {type} (dado(s) {string.Join(", ", resolution.TableRolls)}).";
        }
        RollSummary = $"{rouseSummary}{result.Successes} sucesso(s), dificuldade {result.Difficulty}: {outcome}. " +
            $"Dados comuns [{string.Join(", ", result.NormalDice)}]; dados de Fome [{string.Join(", ", result.HungerDice)}].{compulsion}";
        ShowDiceRoll("Teste V5", RollSummary, displayedDice, preserveReroll: true);
    }

    [RelayCommand]
    private void RerollFailures()
    {
        var previous = _lastRollForReroll;
        if (previous is null || _rerollUsed) return;
        var selected = RerollableDice.Where(die => die.IsSelected).Take(4).ToArray();
        if (selected.Length is < 1 or > 3)
        {
            RollSummary = "Selecione de um a três dados comuns que falharam.";
            OnPropertyChanged(nameof(CanReroll));
            return;
        }
        if (WillpowerSuperficial + WillpowerAggravated >= WillpowerCapacity)
        {
            RollSummary = "A trilha de Vontade está cheia; não há espaço para marcar o custo da rerrolagem.";
            return;
        }

        var initialFaces = previous.HungerDice.Concat(previous.NormalDice).ToArray();
        var hungerCount = previous.HungerDice.Count;
        var indices = selected.Select(die => hungerCount + die.Index).ToArray();
        var result = _diceActions.RerollFailures(_lastRollPool, _lastRollHunger, _lastRollDifficulty,
            initialFaces, indices);
        WillpowerSuperficial = Math.Min(WillpowerCapacity - WillpowerAggravated, WillpowerSuperficial + 1);
        _rerollUsed = true;
        _lastRollForReroll = null;
        RerollableDice.Clear();
        var outcome = result.Outcome switch
        {
            TestOutcome.Failure => "falha",
            TestOutcome.Success => "sucesso",
            TestOutcome.CriticalSuccess => "sucesso crítico",
            TestOutcome.MessyCritical => "crítico bestial",
            TestOutcome.BestialFailure => "falha bestial",
            _ => "resultado"
        };
        RollSummary = $"Rerrolagem de Vontade: {result.Successes} sucesso(s), dificuldade {result.Difficulty}: {outcome}. " +
            $"Dados comuns [{string.Join(", ", result.NormalDice)}]; dados de Fome [{string.Join(", ", result.HungerDice)}]. 1 dano superficial de Vontade aplicado.";
        ShowDiceRoll("Rerrolagem de Vontade", RollSummary,
            result.NormalDice.Select(face => new RolledDieDisplay(face, "COMUM"))
                .Concat(result.HungerDice.Select(face => new RolledDieDisplay(face, "FOME"))));
    }

    private void AddRerollOption(int index, int face)
    {
        var option = new RerollDieOption(index, face);
        option.SelectionChanged += () => OnPropertyChanged(nameof(CanReroll));
        RerollableDice.Add(option);
    }

    [RelayCommand]
    private void MakeRouseCheck()
    {
        var result = _bloodActions.MakeRouseCheck(Hunger);
        Hunger = result.HungerAfter;
        RouseCheckSummary = result.Succeeded
            ? $"Checagem de Sangue: sucesso ({result.Die}). Fome permanece em {Hunger}."
            : result.HungerAfter == result.HungerBefore
                ? $"Checagem de Sangue: falha ({result.Die}). Fome já estava em 5."
                : $"Checagem de Sangue: falha ({result.Die}). Fome aumenta para {Hunger}.";
        ShowDiceRoll("Checagem de Sangue", RouseCheckSummary, [new RolledDieDisplay(result.Die, "SANGUE")]);
    }

    [RelayCommand]
    private void MendSuperficialHealth()
    {
        try
        {
            var result = _bloodActions.MendSuperficialHealth(HealthSuperficial, Hunger, BloodPotency);
            Hunger = result.HungerAfter;
            HealthSuperficial = Math.Max(0, HealthSuperficial - result.SuperficialHealed);
            RouseCheckSummary = result.RouseCheck.Succeeded
                ? $"Mending: checagem {result.RouseCheck.Die}, sucesso. Recuperou {result.SuperficialHealed} dano(s) superficial(is) de Saúde; Fome {Hunger}."
                : $"Mending: checagem {result.RouseCheck.Die}, falha. Recuperou {result.SuperficialHealed} dano(s) superficial(is) de Saúde; Fome {Hunger}.";
            ShowDiceRoll("Mending", RouseCheckSummary, [new RolledDieDisplay(result.RouseCheck.Die, "SANGUE")]);
        }
        catch (InvalidOperationException exception)
        {
            RouseCheckSummary = exception.Message;
        }
    }
}

public partial class RerollDieOption : ObservableObject
{
    public int Index { get; }
    public int Face { get; }

    public RerollDieOption(int index, int face)
    {
        Index = index;
        Face = face;
    }

    [ObservableProperty]
    private bool isSelected;

    public event Action? SelectionChanged;

    partial void OnIsSelectedChanged(bool value)
    {
        SelectionChanged?.Invoke();
    }
}

public sealed record RolledDieDisplay(int Value, string Kind)
{
    public bool IsHunger => Kind == "FOME";
}
