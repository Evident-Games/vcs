using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Domain.Characters;

namespace VCS.Desktop.ViewModels;

public partial class TraitItemViewModel : ObservableObject
{
    public TraitItemViewModel(TraitValue value)
    {
        Category = value.Category;
        Key = value.Key;
        name = value.Name;
        rating = value.Rating;
        specialization = value.Specialization;
        Loresheet = value.Loresheet;
        MeritGroup = value.Merit?.Group;
        SkillGroup = value.Skill?.Group;
        description = value.Merit?.Description ?? "";
    }
    public TraitCategory Category { get; }
    public string Key { get; }
    public LoresheetInfo? Loresheet { get; }
    public string? MeritGroup { get; internal set; }
    public string? SkillGroup { get; internal set; }
    public int SpecializationCount => CharacterAdvancement.SpecializationCount(Specialization);
    public bool IsPredatorGrant => PredatorRules.IsGrant(ToValue());
    public bool CanEditRating => !IsPredatorGrant;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditName))]
    [NotifyPropertyChangedFor(nameof(IsNameReadOnly))]
    [NotifyPropertyChangedFor(nameof(Minimum))]
    private bool hasPredatorBonus;
    public bool CanEditName => Category != TraitCategory.Attribute && !IsPredatorGrant && !HasPredatorBonus;
    public bool IsNameReadOnly => !CanEditName;
    public int Minimum => Category == TraitCategory.Discipline && HasPredatorBonus ? 1 : 0;
    public bool IsSkill => Category == TraitCategory.Skill;
    [ObservableProperty] private string name;
    [ObservableProperty] private int rating;
    [ObservableProperty] private string advancementInfo = "";
    [ObservableProperty] private string predatorSpecialization = "";
    /// <summary>The current predator lets this discipline take the +1 or this skill take the free specialization.</summary>
    [ObservableProperty] private bool isPredatorChoice;
    [ObservableProperty] private PredatorSpecialization? predatorOption;
    /// <summary>A free-text predator specialization was chosen here and still needs its text.</summary>
    [ObservableProperty] private bool isPredatorPending;
    [ObservableProperty] private string predatorTagTip = "";
    [ObservableProperty] private bool isClanDiscipline;
    [ObservableProperty] private string description = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpecializationCount))]
    private string specialization;
    public TraitValue ToValue() => new(Category, Key, Name, Rating, Specialization, Loresheet,
        MeritGroup is { } group ? new(group, Description) : null,
        SkillGroup is { } skillGroup ? new(skillGroup) : null);
}

public partial class TraitGroupViewModel : ObservableObject, IDisposable
{
    private readonly TraitCategory _category;
    private IReadOnlyDictionary<string, AdvantageDefinition> _catalog = new Dictionary<string, AdvantageDefinition>();
    public TraitGroupViewModel(TraitCategory category, IEnumerable<TraitValue> values)
    {
        _category = category;
        var order = CharacterRules.DefaultAttributes.Select((x, index) => (x.Key, index)).ToDictionary(x => x.Key, x => x.index);
        foreach (var value in values.Where(x => x.Category == category)
                     .OrderBy(x => category == TraitCategory.Attribute ? order[x.Key] : 0)
                     .ThenBy(x => category == TraitCategory.Attribute ? "" : x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var item = new TraitItemViewModel(value);
            item.PropertyChanged += OnItemChanged;
            Items.Add(item);
        }
    }

    public ObservableCollection<TraitItemViewModel> Items { get; } = [];
    public bool CanEdit => _category != TraitCategory.Attribute;
    public event EventHandler? Changed;

    [RelayCommand]
    private void Add()
    {
        AddEntry(new(_category, Guid.NewGuid().ToString("N"), "", 0));
    }

    public void AddEntry(TraitValue value)
    {
        if (value.Category != _category)
            throw new ArgumentException("A categoria da entrada nao corresponde ao grupo.", nameof(value));
        var item = new TraitItemViewModel(value);
        UpdateCatalogInfo(item);
        item.PropertyChanged += OnItemChanged;
        Items.Add(item);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Remove(TraitItemViewModel? item)
    {
        if (item is not null && Items.Remove(item))
        {
            item.PropertyChanged -= OnItemChanged;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TraitItemViewModel.Name) or nameof(TraitItemViewModel.Rating) or
            nameof(TraitItemViewModel.Specialization) or nameof(TraitItemViewModel.Description) or nameof(TraitItemViewModel.SkillGroup))
        {
            if (sender is TraitItemViewModel item) UpdateCatalogInfo(item);
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    internal void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);

    public void ConfigureCatalog(IReadOnlyList<AdvantageDefinition> entries)
    {
        _catalog = entries.Where(x => x.Category == _category).ToDictionary(x => x.Id);
        foreach (var item in Items) UpdateCatalogInfo(item);
    }

    private void UpdateCatalogInfo(TraitItemViewModel item)
    {
        if (item.IsPredatorGrant)
        {
            item.AdvancementInfo = "Origem: Predador. Pontos adicionais; remova/troque pelo painel de predador na criacao.";
            return;
        }
        if (!_catalog.TryGetValue(item.Key, out var entry)) return;
        var warning = item.Rating < entry.Minimum || item.Rating > entry.Maximum
            ? $" Pontuacao fora da referencia ({entry.Minimum}-{entry.Maximum})." : "";
        item.AdvancementInfo = $"{entry.PointsLabel} ponto(s). {entry.Description}{warning}";
    }
    public void Dispose()
    {
        foreach (var item in Items) item.PropertyChanged -= OnItemChanged;
    }
}
