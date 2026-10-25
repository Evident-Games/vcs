using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Domain.Advantages;
using VCS.Domain.Characters;

namespace VCS.Desktop.Sheet;

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
