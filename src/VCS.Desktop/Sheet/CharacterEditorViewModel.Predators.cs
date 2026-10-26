using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Content.Clans;
using VCS.Content.Predators;
using VCS.Desktop.Catalogs;
using VCS.Rules.V5.Advancement;
using VCS.Rules.V5.Blood;
using VCS.Rules.V5.Predators;
using VCS.Sheet;

namespace VCS.Desktop.Sheet;

public partial class PredatorAllocationViewModel(string name, int maximum, TraitCategory category = TraitCategory.Advantage) : ObservableObject
{
    public string Name { get; } = name;
    public int Maximum { get; } = maximum;
    public TraitCategory Category { get; } = category;
    [ObservableProperty] private int points;
}

public partial class CharacterEditorViewModel : IMeritSheetHost
{
    private PredatorSelection? _predatorSelection;
    private bool _syncingPredator;
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
    public bool HasPredatorAdvantageAllocation => PredatorAdvantages.Count > 1;
    public bool HasPredatorFlawAllocation => PredatorFlaws.Count > 1;
    public string PredatorHelp => SelectedPredatorType is not { } type
        ? "Selecione um predador para ver os benefícios e defeitos."
        : $"Benefícios: {type.Benefits}{Environment.NewLine}Defeitos: {type.Cost}";
    public string PredatorDisciplineHint => _predatorSelection is { Discipline.Length: > 0 } applied &&
        SelectedPredatorType?.Id == applied.Id
        ? $"Bonus do predador aplicado: +1 em {applied.Discipline}."
        : SelectedPredatorType is { } type
            ? $"{type.Name}: marque \"predador\" em uma destas disciplinas para receber +1 gratuito: {string.Join(", ", PredatorDisciplines)}."
            : "Selecione um predador para ver em qual disciplina pode colocar o ponto extra.";
    public string PredatorAllocationSummary => SelectedPredatorType is { } type && (HasPredatorAdvantageAllocation || HasPredatorFlawAllocation)
        ? $"{type.Name}: as linhas destacadas vêm do predador. Distribua os pontos livres nelas; ao diminuir uma, os pontos voltam a ficar livres." : "";
    /// <summary>Header legend for the ✦ marks: the predator options, or the one already chosen.</summary>
    public string PredatorDisciplineLegend => !IsCreating || IsNpc || SelectedPredatorType is null ? ""
        : _predatorSelection is { Discipline.Length: > 0 } applied ? $"+1 em {applied.Discipline}"
        : $"{string.Join(" ou ", PredatorDisciplines)} (+1)";
    public string PredatorSkillLegend => !IsCreating || IsNpc || SelectedPredatorType is not { } type ? ""
        : _predatorSelection is { Skill.Length: > 0 } applied ? $"{applied.Skill} ({applied.Specialization})"
        : string.Join(" ou ", type.Specializations.Select(x => x.Label));
    public string AppliedPredatorSummary => _predatorSelection is not null ? $"Predador: {Predator}" : "";
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
        _syncingPredator = true;
        try
        {
            PredatorTypes.Clear();
            foreach (var definition in definitions) PredatorTypes.Add(definition);
            SelectedPredatorType = PredatorTypes.FirstOrDefault(x => x.Id == selectedId);
        }
        finally { _syncingPredator = false; }
        UpdatePredatorChoices();
    }
    partial void OnSelectedPredatorTypeChanged(PredatorDefinition? value)
    {
        if (_syncingPredator) return;
        UpdatePredatorChoices();
        // Choosing a predator applies it right away; discipline and specialization are marked later on their own rows.
        if (value is not null && value.Id != _predatorSelection?.Id) ApplyPredatorLive();
    }
    partial void OnSelectedPredatorSpecializationChanged(PredatorSpecialization? value)
    {
        if (!_syncingPredator) PredatorSpecializationText = value is { IsFreeText: false } ? value.Text : "";
        OnPropertyChanged(nameof(IsPredatorSpecializationFreeText));
    }
    partial void OnPredatorSpecializationTextChanged(string value)
    {
        if (!_syncingPredator && SelectedPredatorSpecialization is { IsFreeText: true }) ApplyPredatorLive();
    }
    private void UpdatePredatorChoices()
    {
        _syncingPredator = true;
        try
        {
            PredatorAdvantages.Clear(); PredatorFlaws.Clear(); PredatorSpecializations.Clear();
            SelectedPredatorSpecialization = null;
            if (SelectedPredatorType is { } type)
            {
                foreach (var item in type.Specializations) PredatorSpecializations.Add(item);
                void Add(ObservableCollection<PredatorAllocationViewModel> target, PredatorAllocation[] options, int budget)
                {
                    foreach (var option in options)
                    {
                        target.Add(new(option.Name, Math.Min(PredatorRules.MaximumFor(option.Name), budget), option.Category));
                    }
                }
                Add(PredatorAdvantages, type.AllocationOptions, type.AllocationPoints);
                Add(PredatorFlaws, type.FlawOptions, type.FlawPoints);
            }
        }
        finally { _syncingPredator = false; }
        RefreshPredatorDisciplines();
        SyncPredatorChoices();
    }
    private void RefreshPredatorDisciplines()
    {
        var selected = SelectedPredatorDiscipline;
        PredatorDisciplines.Clear();
        if (SelectedPredatorType is { } type)
            foreach (var item in PredatorRules.DisciplinesFor(type, Clan)) PredatorDisciplines.Add(item);
        _syncingPredator = true;
        try { SelectedPredatorDiscipline = PredatorDisciplines.FirstOrDefault(x => x == selected); }
        finally { _syncingPredator = false; }
        RefreshPredatorInfo();
    }
    /// <summary>Mirrors the applied receipt in the choice fields, or fills defaults for a predator not applied yet.</summary>
    private void SyncPredatorChoices()
    {
        _syncingPredator = true;
        try
        {
            var receipt = _predatorSelection is { } applied && applied.Id == SelectedPredatorType?.Id ? applied : null;
            SelectedPredatorDiscipline = receipt is { Discipline.Length: > 0 }
                ? PredatorDisciplines.FirstOrDefault(x => ClanCatalog.SameName(x, receipt.Discipline)) ?? receipt.Discipline : null;
            var pending = SelectedPredatorSpecialization is { IsFreeText: true } choice && PredatorSpecializations.Contains(choice)
                ? choice : null;
            if (receipt is { Skill.Length: > 0 })
            {
                SelectedPredatorSpecialization = PredatorSpecializations.FirstOrDefault(x => ClanCatalog.SameName(x.Skill, receipt.Skill) &&
                    (x.IsFreeText || ClanCatalog.SameName(x.Text, receipt.Specialization)));
                PredatorSpecializationText = receipt.Specialization;
            }
            else if (pending is null)
            {
                SelectedPredatorSpecialization = null;
                PredatorSpecializationText = "";
            }
            void Points(ObservableCollection<PredatorAllocationViewModel> target, TraitCategory category)
            {
                if (receipt is not null)
                {
                    foreach (var option in target)
                        option.Points = receipt.Grants.Where(x => x.Category == category && ClanCatalog.SameName(x.Name, option.Name))
                            .Sum(x => x.Rating) - (SelectedPredatorType!.FixedGrants
                            .Select((grant, index) => (grant, points: SelectedPredatorType.FixedPoints[index]))
                            .Where(x => x.grant.Category == category && ClanCatalog.SameName(x.grant.Name, option.Name)).Sum(x => x.points));
                    return;
                }
                // A predator not applied yet starts with every variable point free, so the sheet lists all options.
                foreach (var option in target) option.Points = 0;
            }
            Points(PredatorAdvantages, TraitCategory.Advantage);
            Points(PredatorFlaws, TraitCategory.Flaw);
        }
        finally { _syncingPredator = false; }
        RefreshPredatorInfo();
    }
    private void RefreshPredatorInfo()
    {
        foreach (var item in Disciplines.Items.Concat(Skills.Items))
            item.HasPredatorBonus = _predatorSelection is { } receipt &&
                (item.Category == TraitCategory.Discipline && item.Key == receipt.DisciplineKey ||
                 item.Category == TraitCategory.Skill && item.Key == receipt.SkillKey);
        foreach (var item in Skills.Items)
            item.PredatorSpecialization = _predatorSelection is { } applied && item.Key == applied.SkillKey ? applied.Specialization : "";
        var type = IsCreating && !IsNpc ? SelectedPredatorType : null;
        foreach (var item in Disciplines.Items)
        {
            item.IsPredatorChoice = type is not null && PredatorDisciplines.Any(x => ClanCatalog.SameName(x, item.Name));
            item.PredatorTagTip = item.HasPredatorBonus
                ? $"{type?.Name}: +1 gratuito nesta disciplina. Clique para desmarcar."
                : $"{type?.Name}: clique para receber +1 gratuito nesta disciplina.";
        }
        foreach (var item in Skills.Items)
        {
            item.PredatorOption = type?.Specializations.FirstOrDefault(x => ClanCatalog.SameName(x.Skill, item.Name));
            item.IsPredatorChoice = item.PredatorOption is not null;
            item.IsPredatorPending = item.PredatorOption is { IsFreeText: true } option && !item.HasPredatorBonus &&
                SelectedPredatorSpecialization == option;
            item.PredatorTagTip = item.PredatorOption is not { } choice ? ""
                : item.HasPredatorBonus ? $"{type?.Name}: especialização gratuita aplicada. Clique para desmarcar."
                : $"{type?.Name}: clique para ganhar a especialização gratuita {(choice.IsFreeText ? $"({choice.Text})" : choice.Text)}.";
        }
        foreach (var name in new[] { nameof(PredatorHelp),
            nameof(PredatorDisciplineHint), nameof(PredatorAllocationSummary),
            nameof(HasPredatorAdvantageAllocation), nameof(HasPredatorFlawAllocation), nameof(HasAppliedPredator),
            nameof(CanEditPredatorText), nameof(AppliedPredatorSummary), nameof(PredatorDisciplineLegend), nameof(PredatorSkillLegend), nameof(HumanityDescription), nameof(HumanitySocialReference), nameof(IsWassail) })
            OnPropertyChanged(name);
        AdvantageCatalog?.RefreshPredator();
    }
    partial void OnSelectedPredatorDisciplineChanged(string? value)
    {
        OnPropertyChanged(nameof(PredatorHelp));
        OnPropertyChanged(nameof(PredatorDisciplineHint));
    }

    /// <summary>Re-applies the predator with the current choices; a refused change rolls the fields back to the receipt.</summary>
    private void ApplyPredatorLive()
    {
        if (!_ready || _syncingPredator || IsNpc || !IsCreating || SelectedPredatorType is not { } type) return;
        var specialization = SelectedPredatorSpecialization is { } choice &&
            (!choice.IsFreeText || !string.IsNullOrWhiteSpace(PredatorSpecializationText))
            ? Array.IndexOf(type.Specializations, choice) : -1;
        try
        {
            var snapshot = Snapshot();
            ApplyPredatorResult(PredatorRules.Apply(snapshot.Details, snapshot.Traits,
                new(type.Id, SelectedPredatorDiscipline ?? "", specialization, PredatorSpecializationText,
                    PredatorAdvantages.Select(x => x.Points).ToArray(), PredatorFlaws.Select(x => x.Points).ToArray())));
        }
        catch (InvalidOperationException exception)
        {
            ReportAdvancementError(exception);
            if (SelectedPredatorSpecialization is { IsFreeText: true } && specialization >= 0) SelectedPredatorSpecialization = null;
        }
        SyncPredatorChoices();
    }

    [RelayCommand]
    private void TogglePredatorDiscipline(TraitItemViewModel? item)
    {
        if (item is not { IsPredatorChoice: true }) return;
        _syncingPredator = true;
        try
        {
            SelectedPredatorDiscipline = item.HasPredatorBonus ? null
                : PredatorDisciplines.FirstOrDefault(x => ClanCatalog.SameName(x, item.Name));
        }
        finally { _syncingPredator = false; }
        ApplyPredatorLive();
    }

    [RelayCommand]
    private void TogglePredatorSpecialization(TraitItemViewModel? item)
    {
        if (item?.PredatorOption is not { } option) return;
        var active = item.HasPredatorBonus || item.IsPredatorPending;
        _syncingPredator = true;
        try
        {
            SelectedPredatorSpecialization = active ? null : option;
            PredatorSpecializationText = active || option.IsFreeText ? "" : option.Text;
        }
        finally { _syncingPredator = false; }
        ApplyPredatorLive();
    }

    private ObservableCollection<PredatorAllocationViewModel> PredatorOptions(TraitCategory category) =>
        category == TraitCategory.Flaw ? PredatorFlaws : PredatorAdvantages;
    private bool IsPredatorAllocationOpen => IsCreating && !IsNpc && SelectedPredatorType is { } type && _predatorSelection?.Id == type.Id;
    private bool IsSingleFlawChoice(TraitCategory category) =>
        category == TraitCategory.Flaw && SelectedPredatorType is { FlawPoints: > 0, SplitFlaws: false };
    private int PredatorBudget(TraitCategory category) => SelectedPredatorType is not { } type ? 0
        : category == TraitCategory.Flaw ? type.FlawPoints : type.AllocationPoints;
    public int PredatorRemaining(TraitCategory category) =>
        IsPredatorAllocationOpen ? Math.Max(0, PredatorBudget(category) - PredatorOptions(category).Sum(x => x.Points)) : 0;

    PredatorAllocationViewModel? IMeritSheetHost.PredatorOption(TraitItemViewModel grant)
    {
        if (!IsPredatorAllocationOpen || !grant.IsPredatorGrant || SelectedPredatorType is not { } type ||
            !int.TryParse(grant.Key[(grant.Key.LastIndexOf(':') + 1)..], out var index) || index < type.FixedGrants.Length) return null;
        return PredatorOptions(grant.Category).FirstOrDefault(x => x.Points > 0 && ClanCatalog.SameName(x.Name, grant.Name));
    }

    /// <summary>Options still worth showing while variable predator points are unallocated.</summary>
    IReadOnlyList<PredatorAllocationViewModel> IMeritSheetHost.PendingPredatorOptions(TraitCategory category)
    {
        if (PredatorRemaining(category) == 0 || IsSingleFlawChoice(category) && PredatorOptions(category).Any(x => x.Points > 0)) return [];
        return PredatorOptions(category).Where(x => x.Points == 0 && PredatorRoom(x) >= (x.Name == "Isca de Estaca" ? 2 : 1)).ToArray();
    }

    public int PredatorRoom(PredatorAllocationViewModel option)
    {
        var options = PredatorOptions(option.Category);
        if (!options.Contains(option) || IsSingleFlawChoice(option.Category) && options.Any(x => x != option && x.Points > 0)) return 0;
        return Math.Min(option.Maximum, option.Points + PredatorRemaining(option.Category));
    }

    /// <summary>Places points on one predator option; lowering it returns the points to the free pool.</summary>
    public void AllocatePredator(PredatorAllocationViewModel option, int points)
    {
        var room = PredatorRoom(option);
        points = Math.Clamp(points, 0, room);
        // Isca de Estaca only exists at two points.
        if (option.Name == "Isca de Estaca" && points == 1) points = option.Points == 2 || room < 2 ? 0 : 2;
        if (points == option.Points) return;
        _syncingPredator = true;
        try { option.Points = points; }
        finally { _syncingPredator = false; }
        ApplyPredatorLive();
    }
    [RelayCommand]
    private void ApplyPredator()
    {
        try
        {
            if (SelectedPredatorType is not { } type) throw new InvalidOperationException("Selecione um predador.");
            var specialization = SelectedPredatorSpecialization is { } choice ? Array.IndexOf(type.Specializations, choice) : -1;
            var snapshot = Snapshot();
            ApplyPredatorResult(PredatorRules.Apply(snapshot.Details, snapshot.Traits,
                new(type.Id, SelectedPredatorDiscipline ?? "", specialization, PredatorSpecializationText,
                    PredatorAdvantages.Select(x => x.Points).ToArray(), PredatorFlaws.Select(x => x.Points).ToArray())));
        }
        catch (InvalidOperationException exception) { ReportAdvancementError(exception); }
        SyncPredatorChoices();
    }
    [RelayCommand]
    private void RemovePredator()
    {
        try
        {
            var snapshot = Snapshot();
            ApplyPredatorResult(PredatorRules.Remove(snapshot.Details, snapshot.Traits));
            _syncingPredator = true;
            try { SelectedPredatorType = null; }
            finally { _syncingPredator = false; }
            UpdatePredatorChoices();
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
                foreach (var item in group.Items.Where(x => !values.Any(v => v.Key == x.Key && v.Name == x.Name)).ToArray())
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
        Error = "";
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
