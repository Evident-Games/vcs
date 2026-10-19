using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    public const string RitualDiscipline = "Feitiçaria de Sangue";
    private readonly TraitGroupViewModel _disciplines;
    private readonly PowerItemViewModel[] _all;
    private readonly string[] _catalogDisciplines;
    private bool _syncing;
    public DisciplineCatalogViewModel(IDisciplineCatalog catalog, TraitGroupViewModel disciplines, IEnumerable<TraitValue> traits)
    {
        _disciplines = disciplines;
        var powers = traits.Where(x => x.Category == TraitCategory.DisciplinePower).ToArray();
        var known = powers.ToDictionary(x => x.Key);
        var entries = catalog.Powers.Select(p => new PowerItemViewModel(p, known.ContainsKey(p.Id), Rating, clan: () => Clan)).ToList();
        var ids = catalog.Powers.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var missing in powers.Where(x => !ids.Contains(x.Key) && x.Power is null))
            entries.Add(new(new(missing.Key, "Fora do catalogo", missing.Name, missing.Rating, "", "", ""),
                true, Rating, unavailable: true));
        _all = entries.ToArray();
        _catalogDisciplines = catalog.Powers.Where(x => !x.IsRitual).Select(x => x.Discipline).Distinct()
            .Order(StringComparer.CurrentCultureIgnoreCase).ToArray();
        DisciplineOptions = [AllDisciplines, .. _catalogDisciplines, "Fora do catalogo"];
        _syncing = true;
        foreach (var value in powers) LoadRow(value);
        _syncing = false;
        foreach (var entry in _all) entry.PropertyChanged += OnPowerChanged;
        _disciplines.Changed += OnDisciplineChanged;
        ApplyFilter();
    }

    public ObservableCollection<PowerRowViewModel> PowerRows { get; } = [];
    public ObservableCollection<PowerRowViewModel> RitualRows { get; } = [];
    public bool HasNoPowerRows => PowerRows.Count == 0;
    public bool HasNoRitualRows => RitualRows.Count == 0;

    [RelayCommand] private void AddPower() => AddRow(false);
    [RelayCommand] private void AddRitual() => AddRow(true);

    public PowerRowViewModel AddRow(bool isRitual)
    {
        var row = new PowerRowViewModel(this, isRitual, NewKey()) { IsExpanded = isRitual };
        Rows(isRitual).Add(row);
        RowsChanged();
        return row;
    }

    private static string NewKey() => "power-" + Guid.NewGuid().ToString("N");
    private ObservableCollection<PowerRowViewModel> Rows(bool isRitual) => isRitual ? RitualRows : PowerRows;

    private void LoadRow(TraitValue value)
    {
        var entry = _all.FirstOrDefault(x => x.Definition.Id == value.Key);
        var isRitual = value.Power?.IsRitual ?? entry?.Definition.IsRitual ?? false;
        var row = new PowerRowViewModel(this, isRitual, value.Key) { Loading = true, Entry = entry };
        row.Name = value.Name;
        row.Level = Math.Clamp(value.Rating, 1, 5);
        if (value.Power is { } info) row.Apply(info);
        else if (entry is { IsUnavailable: false }) row.Apply(PowerRowViewModel.Defaults(entry.Definition));
        else row.Discipline = "";
        row.Loading = false;
        Rows(isRitual).Add(row);
    }

    internal void RemoveRow(PowerRowViewModel row)
    {
        if (!Rows(row.IsRitual).Remove(row)) return;
        Unbind(row);
        RowsChanged();
    }

    internal void RowEdited() => Changed?.Invoke(this, EventArgs.Empty);

    private void RowsChanged()
    {
        OnPropertyChanged(nameof(HasNoPowerRows));
        OnPropertyChanged(nameof(HasNoRitualRows));
        ApplyFilter();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Unbind(PowerRowViewModel row)
    {
        if (row.Entry is not { } entry) return;
        row.Entry = null;
        if (entry.IsUnavailable) return;
        _syncing = true;
        entry.IsKnown = false;
        _syncing = false;
        entry.RefreshWarning();
    }

    /// <summary>Links the row to the catalog entry whose name it now matches, or turns it into a personal power.</summary>
    internal void Resolve(PowerRowViewModel row)
    {
        var term = CatalogText.Normalize(row.Name);
        var candidates = _all.Where(x => !x.IsUnavailable && x.Definition.IsRitual == row.IsRitual &&
            CatalogText.Normalize(x.Definition.Name) == term).ToArray();
        var match = candidates.FirstOrDefault(x => SameDiscipline(x.Definition.RequiredDiscipline, row.Discipline)) ??
            candidates.FirstOrDefault();
        if (ReferenceEquals(match, row.Entry) || term.Length == 0 && row.Entry is null) return;
        if (match is not null && PowerRows.Concat(RitualRows).Any(x => !ReferenceEquals(x, row) && ReferenceEquals(x.Entry, match)))
            match = null;
        if (match is null && row.Entry is { IsUnavailable: true }) return;
        Unbind(row);
        if (match is null)
        {
            row.Key = NewKey();
        }
        else
        {
            row.Entry = match;
            row.Key = match.Definition.Id;
            _syncing = true;
            match.IsKnown = true;
            _syncing = false;
            FillFromCatalog(row);
            EnsureDiscipline(match);
        }
        row.Refresh();
        ApplyFilter();
    }

    internal void FillFromCatalog(PowerRowViewModel row)
    {
        if (row.Entry is not { IsUnavailable: false } entry) return;
        row.Loading = true;
        row.Level = entry.Definition.Level;
        row.Apply(PowerRowViewModel.Defaults(entry.Definition));
        row.Loading = false;
        row.Refresh();
        RowEdited();
    }

    private static bool SameDiscipline(string a, string b) => CatalogText.Normalize(a) == CatalogText.Normalize(b);

    internal IReadOnlyList<SuggestionItem> NameSuggestions(PowerRowViewModel row)
    {
        var filtered = string.IsNullOrWhiteSpace(row.Discipline) || row.IsRitual
            ? null : row.Discipline;
        return _all.Where(x => !x.IsUnavailable && x.Definition.IsRitual == row.IsRitual &&
                (filtered is null || SameDiscipline(x.Definition.RequiredDiscipline, filtered)))
            .OrderBy(x => x.Definition.Level).ThenBy(x => x.Definition.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(x => new SuggestionItem(x.Definition.Name, filtered is null && !row.IsRitual
                ? $"{x.Definition.Discipline} · {new string('●', x.Definition.Level)}"
                : new string('●', x.Definition.Level)))
            .ToArray();
    }

    internal IReadOnlyList<SuggestionItem> DisciplineSuggestions(bool isRitual)
    {
        IEnumerable<string> names = isRitual
            ? [RitualDiscipline, "Oblívio"]
            : _disciplines.Items.Where(x => x.Rating > 0).Select(x => x.Name).Concat(_catalogDisciplines)
                .Concat(_disciplines.Items.Select(x => x.Name));
        return names.Where(x => !string.IsNullOrWhiteSpace(x)).DistinctBy(CatalogText.Normalize)
            .Select(x => new SuggestionItem(x, Rating(x) is var r and > 0 ? new string('●', r) : "")).ToArray();
    }

    internal string WarningFor(PowerRowViewModel row)
    {
        if (string.IsNullOrWhiteSpace(row.Name) || string.IsNullOrWhiteSpace(row.Discipline)) return "";
        var rating = Rating(row.Discipline);
        if (row.Level > rating)
            return $"Requer {row.Discipline} {row.Level}; a ficha tem {rating}. Seleção livre.";
        return row.Entry is { IsUnavailable: false } entry && entry.Definition.AmalgamDiscipline is { } amalgam &&
            Rating(amalgam) < entry.Definition.AmalgamLevel
            ? $"Amálgama requer {amalgam} {entry.Definition.AmalgamLevel}; a ficha tem {Rating(amalgam)}. Seleção livre."
            : "";
    }

    private void EnsureDiscipline(PowerItemViewModel entry)
    {
        if (entry.IsUnavailable || _disciplines.Items.Any(x => x.Key == CatalogText.StableId("disc-", entry.Definition.RequiredDiscipline) ||
                CatalogText.Normalize(x.Name) == CatalogText.Normalize(entry.Definition.RequiredDiscipline))) return;
        _disciplines.AddEntry(new(TraitCategory.Discipline, CatalogText.StableId("disc-", entry.Definition.RequiredDiscipline),
            entry.Definition.RequiredDiscipline, 0));
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
        if (_syncing || e.PropertyName != nameof(PowerItemViewModel.IsKnown) || sender is not PowerItemViewModel entry) return;
        var rows = Rows(entry.Definition.IsRitual);
        var bound = PowerRows.Concat(RitualRows).FirstOrDefault(x => ReferenceEquals(x.Entry, entry));
        if (entry.IsKnown && bound is null)
        {
            EnsureDiscipline(entry);
            var row = new PowerRowViewModel(this, entry.Definition.IsRitual, entry.Definition.Id) { Loading = true, Entry = entry };
            row.Name = entry.Definition.Name;
            row.Level = Math.Clamp(entry.Definition.Level, 1, 5);
            if (!entry.IsUnavailable) row.Apply(PowerRowViewModel.Defaults(entry.Definition));
            row.Loading = false;
            rows.Add(row);
        }
        else if (!entry.IsKnown && bound is not null)
        {
            Rows(bound.IsRitual).Remove(bound);
            bound.Entry = null;
        }
        entry.RefreshWarning();
        RowsChanged();
    }

    private void OnDisciplineChanged(object? sender, EventArgs e)
    {
        foreach (var power in _all) power.RefreshWarning();
        foreach (var row in PowerRows.Concat(RitualRows)) row.Refresh();
    }

    public TraitValue[] SelectedValues() => PowerRows.Concat(RitualRows)
        .Where(x => !string.IsNullOrWhiteSpace(x.Name))
        .GroupBy(x => x.Key).Select(x => x.First())
        .Select(ToValue).ToArray();

    private static TraitValue ToValue(PowerRowViewModel row)
    {
        var info = row.Info();
        var plain = row.Entry is { IsUnavailable: false } entry
            ? info == PowerRowViewModel.Defaults(entry.Definition) && row.Level == entry.Definition.Level
            : row.Entry is { IsUnavailable: true } && info == new PowerInfo("", IsRitual: row.IsRitual);
        return new(TraitCategory.DisciplinePower, row.Key, row.Name.Trim(), Math.Clamp(row.Level, 1, 5), Power: plain ? null : info);
    }

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
