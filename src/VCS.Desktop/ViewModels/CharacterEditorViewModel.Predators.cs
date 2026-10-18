using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Domain.Characters;

namespace VCS.Desktop.ViewModels;

public partial class PredatorAllocationViewModel(string name, int maximum) : ObservableObject
{
    public string Name { get; } = name;
    public int Maximum { get; } = maximum;
    [ObservableProperty] private int points;
}

public partial class CharacterEditorViewModel
{
    private PredatorSelection? _predatorSelection;
    public ObservableCollection<PredatorDefinition> PredatorTypes { get; } = new(PredatorCatalog.All);
    public ObservableCollection<string> PredatorDisciplines { get; } = [];
    public ObservableCollection<PredatorSpecialization> PredatorSpecializations { get; } = [];
    public ObservableCollection<PredatorAllocationViewModel> PredatorAdvantages { get; } = [];
    public ObservableCollection<PredatorAllocationViewModel> PredatorFlaws { get; } = [];
    [ObservableProperty] private PredatorDefinition? selectedPredatorType;
    [ObservableProperty] private string? selectedPredatorDiscipline;
    [ObservableProperty] private PredatorSpecialization? selectedPredatorSpecialization;
    [ObservableProperty] private string predatorSpecializationText = "";
    [ObservableProperty] private int stains;
    public bool HasAppliedPredator => _predatorSelection is not null;
    public bool CanEditPredatorText => !HasAppliedPredator;
    public bool IsPredatorSpecializationFreeText => SelectedPredatorSpecialization?.IsFreeText == true;
    public bool HasPredatorAdvantageAllocation => PredatorAdvantages.Count > 0;
    public bool HasPredatorFlawAllocation => PredatorFlaws.Count > 0;
    public string PredatorDescription => SelectedPredatorType?.Description ?? "Selecione um tipo de predador.";
    public string PredatorBenefits => SelectedPredatorType?.Benefits ?? "";
    public string PredatorCost => SelectedPredatorType?.Cost ?? "";
    public string PredatorAllocationSummary => SelectedPredatorType is { } type
        ? $"Distribua {type.AllocationPoints} ponto(s) de vantagens e {type.FlawPoints} ponto(s) de defeitos nas opcoes abaixo." +
          (type.FlawPoints > 0 && !type.SplitFlaws ? " Para o defeito, escolha uma unica opcao." : "") : "";
    public string AppliedPredatorSummary => _predatorSelection is { } receipt
        ? $"Aplicado: {Predator}. +1 {receipt.Discipline}; {receipt.Skill} ({receipt.Specialization}). " +
          $"Humanidade {receipt.HumanityDelta:+0;-0;0}; Potencia de Sangue {receipt.BloodDelta:+0;-0;0}. " +
          "Vantagens/defeitos de origem Predador sao adicionais ao orcamento basico. Trocar na criacao remove somente os efeitos registrados."
        : "Nenhum bonus aplicado. Predador em fichas antigas continua como texto, sem efeitos retroativos.";
    public string HumanityDescription => Humanity is >= 0 and <= 10 ? HumanityRules.At(Humanity).Description : "Humanidade deve estar entre 0 e 10.";
    public string HumanitySocialReference => Humanity is >= 0 and <= 10 && HumanityRules.At(Humanity) is { } level
        ? $"Referencia social: {level.SocialPenalty} dado(s)" +
          (level.BlushSocialPenalty is { } blush ? $"; com Rubor: {blush}." : ".") + " Sem rolagens ou modificadores automaticos."
        : "";
    public bool IsWassail => Humanity == 0;
    public IReadOnlyList<HumanityLevel> HumanityLevels => HumanityRules.Levels;

    public void ConfigurePredatorCatalog(IReadOnlyList<PredatorDefinition> definitions)
    {
        var selectedId = SelectedPredatorType?.Id ?? _predatorSelection?.Id;
        PredatorTypes.Clear();
        foreach (var definition in definitions) PredatorTypes.Add(definition);
        SelectedPredatorType = PredatorTypes.FirstOrDefault(x => x.Id == selectedId) ?? PredatorTypes.FirstOrDefault();
    }
    partial void OnSelectedPredatorTypeChanged(PredatorDefinition? value) => UpdatePredatorChoices();
    partial void OnSelectedPredatorSpecializationChanged(PredatorSpecialization? value)
    {
        PredatorSpecializationText = value is { IsFreeText: false } ? value.Text : "";
        OnPropertyChanged(nameof(IsPredatorSpecializationFreeText));
    }
    private void UpdatePredatorChoices()
    {
        PredatorAdvantages.Clear(); PredatorFlaws.Clear(); PredatorSpecializations.Clear();
        if (SelectedPredatorType is { } type)
        {
            foreach (var item in type.Specializations) PredatorSpecializations.Add(item);
            void Add(ObservableCollection<PredatorAllocationViewModel> target, PredatorAllocation[] options, int budget)
            {
                foreach (var option in options)
                {
                    var max = option.Name is "Perdicao Folclorica" or "Tabu Folclorico" or "Estigma" ? 1 :
                        option.Name == "Isca de Estaca" ? 2 : budget;
                    target.Add(new(option.Name, max));
                }
            }
            Add(PredatorAdvantages, type.AllocationOptions, type.AllocationPoints);
            Add(PredatorFlaws, type.FlawOptions, type.FlawPoints);
        }
        SelectedPredatorSpecialization = PredatorSpecializations.FirstOrDefault();
        RefreshPredatorDisciplines();
        RefreshPredatorInfo();
    }
    private void RefreshPredatorDisciplines()
    {
        var selected = SelectedPredatorDiscipline;
        PredatorDisciplines.Clear();
        if (SelectedPredatorType is { } type)
            foreach (var item in PredatorCatalog.DisciplinesFor(type, Clan)) PredatorDisciplines.Add(item);
        SelectedPredatorDiscipline = PredatorDisciplines.FirstOrDefault(x => x == selected) ?? PredatorDisciplines.FirstOrDefault();
    }
    private void RefreshPredatorInfo()
    {
        foreach (var item in Disciplines.Items.Concat(Skills.Items))
            item.HasPredatorBonus = _predatorSelection is { } receipt &&
                (item.Category == TraitCategory.Discipline && item.Key == receipt.DisciplineKey ||
                 item.Category == TraitCategory.Skill && item.Key == receipt.SkillKey);
        foreach (var name in new[] { nameof(PredatorDescription), nameof(PredatorBenefits), nameof(PredatorCost), nameof(PredatorAllocationSummary),
            nameof(HasPredatorAdvantageAllocation), nameof(HasPredatorFlawAllocation), nameof(HasAppliedPredator),
            nameof(CanEditPredatorText), nameof(AppliedPredatorSummary), nameof(HumanityDescription), nameof(HumanitySocialReference), nameof(IsWassail) })
            OnPropertyChanged(name);
    }

    [RelayCommand]
    private void ApplyPredator()
    {
        try
        {
            if (SelectedPredatorType is not { } type || SelectedPredatorSpecialization is not { } specialization ||
                SelectedPredatorDiscipline is not { } discipline)
                throw new InvalidOperationException("Selecione predador, disciplina e especializacao.");
            var snapshot = Snapshot();
            ApplyPredatorResult(PredatorRules.Apply(snapshot.Details, snapshot.Traits,
                new(type.Id, discipline, Array.IndexOf(type.Specializations, specialization), PredatorSpecializationText,
                    PredatorAdvantages.Select(x => x.Points).ToArray(), PredatorFlaws.Select(x => x.Points).ToArray())));
        }
        catch (InvalidOperationException exception) { ReportAdvancementError(exception); }
    }
    [RelayCommand]
    private void RemovePredator()
    {
        try
        {
            var snapshot = Snapshot();
            ApplyPredatorResult(PredatorRules.Remove(snapshot.Details, snapshot.Traits));
        }
        catch (InvalidOperationException exception) { ReportAdvancementError(exception); }
    }
    private void ApplyPredatorResult(AdvancementResult result)
    {
        _ready = false;
        try
        {
            _predatorSelection = result.Details.PredatorSelection;
            Predator = result.Details.Predator;
            Humanity = result.Details.Humanity;
            BloodPotency = result.Details.BloodPotency;
            foreach (var group in new[] { Skills, Disciplines, Advantages, Flaws })
            {
                var category = ReferenceEquals(group, Skills) ? TraitCategory.Skill : ReferenceEquals(group, Disciplines) ? TraitCategory.Discipline :
                    ReferenceEquals(group, Advantages) ? TraitCategory.Advantage : TraitCategory.Flaw;
                var values = result.Traits.Where(x => x.Category == category).ToArray();
                foreach (var item in group.Items.Where(x => !values.Any(v => v.Key == x.Key)).ToArray())
                    group.RemoveCommand.Execute(item);
                foreach (var value in values)
                {
                    var item = group.Items.FirstOrDefault(x => x.Key == value.Key);
                    if (item is null) group.AddEntry(value);
                    else { item.Rating = value.Rating; item.Specialization = value.Specialization; }
                }
            }
            RefreshPredatorInfo();
            if (IsNpc)
            {
                NpcDisciplines.Clear();
                foreach (var item in Disciplines.Items) NpcDisciplines.Add(new(this, item));
                ConfigureAdvantageCatalog(_advantageDefinitions);
            }
        }
        finally { _ready = true; }
        RefreshAdvancement(); RefreshPredatorInfo(); MarkChanged();
    }
    partial void OnHumanityChanged(int value) => RefreshPredatorInfo();
    [RelayCommand]
    private void ApplyStainDamage()
    {
        try
        {
            var snapshot = Snapshot();
            WillpowerAggravated = HumanityRules.ApplyStainDamage(snapshot.Details, snapshot.Traits).WillpowerAggravated;
        }
        catch (InvalidOperationException exception) { ReportAdvancementError(exception); }
    }
    [RelayCommand]
    private void FailRemorse()
    {
        try
        {
            var result = HumanityRules.FailRemorse(Snapshot().Details);
            _ready = false;
            try { Stains = result.Stains; Humanity = result.Humanity; }
            finally { _ready = true; }
            MarkChanged();
        }
        catch (InvalidOperationException exception) { ReportAdvancementError(exception); }
    }
}
