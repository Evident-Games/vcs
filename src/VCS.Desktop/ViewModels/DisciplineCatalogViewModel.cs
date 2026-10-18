using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using VCS.Application.Disciplines;
using VCS.Domain.Characters;

namespace VCS.Desktop.ViewModels;

public partial class PowerItemViewModel : ObservableObject
{
    private readonly Func<string, int> _rating;
    private readonly Func<string> _clan;
    public PowerItemViewModel(PowerDefinition definition, bool isKnown, Func<string, int> rating, bool unavailable = false,
        Func<string>? clan = null)
    {
        Definition = definition;
        this.isKnown = isKnown;
        _rating = rating;
        _clan = clan ?? (() => "");
        IsUnavailable = unavailable;
    }

    public PowerDefinition Definition { get; }
    public bool IsUnavailable { get; }
    [ObservableProperty] private bool isKnown;
    public string Heading => $"{Definition.Name} · Nível {Definition.Level}" +
        (Definition.AmalgamDiscipline is { } discipline ? $" · Amalgama: {discipline} {Definition.AmalgamLevel}" : "");
    public string Warning => IsUnavailable
        ? "Entrada fora do catalogo atual. A selecao foi preservada; os detalhes originais nao estao disponiveis."
        : IsKnown && Definition.Level > _rating(Definition.RequiredDiscipline)
            ? $"Aviso: requer nivel {Definition.Level} de {Definition.RequiredDiscipline}; sua ficha tem {_rating(Definition.RequiredDiscipline)}. Selecao livre."
            : IsKnown && Definition.AmalgamDiscipline is { } amalgam && _rating(amalgam) < Definition.AmalgamLevel
                ? $"Aviso: amalgama requer {amalgam} {Definition.AmalgamLevel}; sua ficha tem {_rating(amalgam)}. Selecao livre."
                : "";
    public string AdvancementInfo => IsUnavailable ? "Disciplina de origem nao identificada; custo nao calculado."
        : Definition.IsRitual ? "Ritual: os multiplicadores de disciplinas nao calculam o custo deste ritual."
        : DisciplineXpText.Describe(_clan(), Definition.RequiredDiscipline, _rating(Definition.RequiredDiscipline));
    public void RefreshWarning()
    {
        OnPropertyChanged(nameof(Warning));
        OnPropertyChanged(nameof(AdvancementInfo));
    }
    public TraitValue ToValue() => new(TraitCategory.DisciplinePower, Definition.Id, Definition.Name, Definition.Level);
}

public partial class DisciplineCatalogViewModel : ObservableObject, IDisposable
{
    public const string AllDisciplines = "Todas as disciplinas";
    private readonly TraitGroupViewModel _disciplines;
    private readonly PowerItemViewModel[] _all;
    public DisciplineCatalogViewModel(IDisciplineCatalog catalog, TraitGroupViewModel disciplines, IEnumerable<TraitValue> traits)
    {
        _disciplines = disciplines;
        var known = traits.Where(x => x.Category == TraitCategory.DisciplinePower).ToDictionary(x => x.Key);
        var entries = catalog.Powers.Select(p => new PowerItemViewModel(p, known.ContainsKey(p.Id), Rating, clan: () => Clan)).ToList();
        var ids = catalog.Powers.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var missing in known.Values.Where(x => !ids.Contains(x.Key)))
            entries.Add(new(new(missing.Key, "Fora do catalogo", missing.Name, missing.Rating, "", "", ""),
                true, Rating, unavailable: true));
        _all = entries.ToArray();
        DisciplineOptions = [AllDisciplines, .. catalog.Powers.Where(x => !x.IsRitual)
            .Select(x => x.Discipline).Distinct().Order(StringComparer.CurrentCultureIgnoreCase), "Fora do catalogo"];
        foreach (var entry in _all) entry.PropertyChanged += OnPowerChanged;
        _disciplines.Changed += OnDisciplineChanged;
        ApplyFilter();
    }

    public IReadOnlyList<PowerItemViewModel> AllPowers => _all;
    public string[] DisciplineOptions { get; }
    public ObservableCollection<PowerItemViewModel> Powers { get; } = [];
    public ObservableCollection<PowerItemViewModel> Rituals { get; } = [];
    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private string selectedDiscipline = AllDisciplines;
    [ObservableProperty] private bool knownOnly;
    [ObservableProperty] private string clan = "";
    [ObservableProperty] private PowerItemViewModel? selectedPower;
    [ObservableProperty] private PowerItemViewModel? selectedRitual;
    public int KnownCount => _all.Count(x => x.IsKnown);
    public string CountLabel => $"{KnownCount} selecionado(s) · {Powers.Count} poderes e {Rituals.Count} rituais na busca";
    public bool NoPowers => Powers.Count == 0;
    public bool NoRituals => Rituals.Count == 0;
    public event EventHandler? Changed;

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedDisciplineChanged(string value) => ApplyFilter();
    partial void OnKnownOnlyChanged(bool value) => ApplyFilter();
    partial void OnClanChanged(string value)
    {
        foreach (var power in _all) power.RefreshWarning();
    }

    private int Rating(string discipline) => _disciplines.Items
        .Where(x => x.Key == CatalogText.StableId("disc-", discipline) ||
                    CatalogText.Normalize(x.Name) == CatalogText.Normalize(discipline))
        .Select(x => x.Rating).DefaultIfEmpty(0).Max();

    private void OnPowerChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PowerItemViewModel.IsKnown) || sender is not PowerItemViewModel entry) return;
        if (entry.IsKnown && !entry.IsUnavailable &&
            !_disciplines.Items.Any(x => x.Key == CatalogText.StableId("disc-", entry.Definition.RequiredDiscipline) ||
                CatalogText.Normalize(x.Name) == CatalogText.Normalize(entry.Definition.RequiredDiscipline)))
            _disciplines.AddEntry(new(TraitCategory.Discipline, CatalogText.StableId("disc-", entry.Definition.RequiredDiscipline),
                entry.Definition.RequiredDiscipline, 0));
        entry.RefreshWarning();
        ApplyFilter();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnDisciplineChanged(object? sender, EventArgs e)
    {
        foreach (var power in _all) power.RefreshWarning();
    }

    public TraitValue[] SelectedValues() => _all.Where(x => x.IsKnown).Select(x => x.ToValue()).ToArray();

    private void ApplyFilter()
    {
        var term = CatalogText.Normalize(SearchText);
        bool Matches(PowerItemViewModel item) =>
            (!KnownOnly || item.IsKnown) &&
            (SelectedDiscipline == AllDisciplines ||
             CatalogText.Normalize(item.Definition.RequiredDiscipline) == CatalogText.Normalize(SelectedDiscipline)) &&
            CatalogText.Normalize($"{item.Definition.Name}\n{item.Definition.Discipline}\n{item.Definition.Cost}\n{item.Definition.DicePool}\n{item.Definition.Summary}\n{item.Definition.AmalgamDiscipline}")
                .Contains(term, StringComparison.Ordinal);

        var previousPower = SelectedPower;
        var previousRitual = SelectedRitual;
        Powers.Clear();
        Rituals.Clear();
        foreach (var entry in _all.Where(Matches).OrderBy(x => x.Definition.Discipline, StringComparer.CurrentCultureIgnoreCase)
                     .ThenBy(x => x.Definition.Level).ThenBy(x => x.Definition.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            if (entry.Definition.IsRitual) Rituals.Add(entry);
            else Powers.Add(entry);
        }
        SelectedPower = previousPower is not null && Powers.Contains(previousPower) ? previousPower : Powers.FirstOrDefault();
        SelectedRitual = previousRitual is not null && Rituals.Contains(previousRitual) ? previousRitual : Rituals.FirstOrDefault();
        OnPropertyChanged(nameof(KnownCount));
        OnPropertyChanged(nameof(CountLabel));
        OnPropertyChanged(nameof(NoPowers));
        OnPropertyChanged(nameof(NoRituals));
    }

    public void Dispose()
    {
        _disciplines.Changed -= OnDisciplineChanged;
        foreach (var power in _all) power.PropertyChanged -= OnPowerChanged;
    }
}
