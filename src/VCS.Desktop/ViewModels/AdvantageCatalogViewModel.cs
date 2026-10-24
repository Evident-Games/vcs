using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Application.Disciplines;
using VCS.Domain.Characters;

namespace VCS.Desktop.ViewModels;

/// <summary>Sheet services the advantage rows need: predator allocation and loresheet purchases.</summary>
public interface IMeritSheetHost
{
    TraitGroupViewModel Loresheets { get; }
    /// <summary>The variable predator option behind a granted line, or null when the line is fixed.</summary>
    PredatorAllocationViewModel? PredatorOption(TraitItemViewModel grant);
    IReadOnlyList<PredatorAllocationViewModel> PendingPredatorOptions(TraitCategory category);
    int PredatorRemaining(TraitCategory category);
    int PredatorRoom(PredatorAllocationViewModel option);
    void AllocatePredator(PredatorAllocationViewModel option, int points);
    /// <summary>Buys the benefit; returns the refusal message or null.</summary>
    string? AddLoresheet(AdvantageDefinition benefit);
    void RemoveLoresheet(TraitItemViewModel item);
}

public sealed class AdvantageCatalogViewModel : IDisposable
{
    public const string OtherFamily = "Outras caracteristicas";
    public const string LoresheetFamily = "Ficha de conhecimento";
    private readonly TraitGroupViewModel _advantages;
    private readonly TraitGroupViewModel _flaws;
    private readonly TraitGroupViewModel? _loresheets;
    internal IMeritSheetHost? Host { get; }
    private int _editing;
    public ObservableCollection<MeritFamilyViewModel> Families { get; } = [];
    public ObservableCollection<MeritRowViewModel> AdvantageRows { get; } = [];
    public ObservableCollection<MeritRowViewModel> FlawRows { get; } = [];
    public RelayCommand AddAdvantageRowCommand { get; }
    public RelayCommand AddFlawRowCommand { get; }

    public AdvantageCatalogViewModel(IReadOnlyList<AdvantageDefinition> entries,
        TraitGroupViewModel advantages, TraitGroupViewModel flaws, IMeritSheetHost? host = null)
    {
        _advantages = advantages;
        _flaws = flaws;
        Host = host;
        _loresheets = host?.Loresheets;
        foreach (var group in entries.Where(x => x.Category is TraitCategory.Advantage or TraitCategory.Flaw)
                     .GroupBy(x => FamilyName(x.Group)).OrderBy(x => x.Key))
            Families.Add(new(this, group.Key, group.ToArray()));
        if (!Families.Any(x => x.Name == OtherFamily))
            Families.Add(new(this, OtherFamily, []));
        // Loresheets are bought as advantages; each benefit keeps its collection as a subtitle.
        if (_loresheets is not null && entries.Where(x => x.Category == TraitCategory.Loresheet)
                .OrderBy(x => x.LoresheetCollection, StringComparer.CurrentCulture).ThenBy(x => x.Minimum).ToArray() is { Length: > 0 } lore)
            Families.Add(new(this, LoresheetFamily, lore));
        AddAdvantageRowCommand = new(() => AdvantageRows.Add(new(this, TraitCategory.Advantage)));
        AddFlawRowCommand = new(() => FlawRows.Add(new(this, TraitCategory.Flaw)));
        advantages.Changed += OnChanged;
        flaws.Changed += OnChanged;
        if (_loresheets is not null) _loresheets.Changed += OnChanged;
        _editing++;
        Synchronize();
        _editing--;
        foreach (var row in Rows(AdvantageRows, TraitCategory.Advantage).Concat(Rows(FlawRows, TraitCategory.Flaw))) row.Refresh();
    }

    private IEnumerable<MeritRowViewModel> Rows(ObservableCollection<MeritRowViewModel> rows, TraitCategory kind)
    {
        rows.Clear();
        foreach (var item in RowItems(kind).OrderByDescending(x => x.IsPredatorGrant)
                     .ThenBy(x => FamilyOf(x).Name, StringComparer.CurrentCulture)
                     .ThenBy(x => x.Name, StringComparer.CurrentCulture))
            rows.Add(new(this, kind, item));
        foreach (var option in Pending(kind))
            rows.Insert(rows.TakeWhile(x => x.IsPredator).Count(), new(this, kind, option));
        return rows;
    }
    /// <summary>Adds blank lines until each list has <paramref name="count"/> rows (NPC sheet layout).</summary>
    public void PadRows(int count)
    {
        while (AdvantageRows.Count < count) AdvantageRows.Add(new(this, TraitCategory.Advantage));
        while (FlawRows.Count < count) FlawRows.Add(new(this, TraitCategory.Flaw));
    }
    private IReadOnlyList<PredatorAllocationViewModel> Pending(TraitCategory kind) => Host?.PendingPredatorOptions(kind) ?? [];
    /// <summary>Re-lists the predator options after an allocation changed.</summary>
    public void RefreshPredator()
    {
        if (_editing == 0) Synchronize();
    }

    private static string FamilyName(string name) => name == "Visual" ? "Beleza" : name;
    internal IEnumerable<TraitItemViewModel> Items => RowItems(TraitCategory.Advantage).Concat(_flaws.Items);
    private IEnumerable<TraitItemViewModel> RowItems(TraitCategory kind) => kind == TraitCategory.Flaw ? _flaws.Items
        : _loresheets is null ? _advantages.Items : _advantages.Items.Concat(_loresheets.Items);
    internal TraitGroupViewModel Traits(TraitCategory category) => category switch
    {
        TraitCategory.Flaw => _flaws,
        TraitCategory.Loresheet when _loresheets is not null => _loresheets,
        _ => _advantages
    };
    /// <summary>Advantage rows also list loresheet benefits.</summary>
    internal static bool Fits(AdvantageDefinition definition, TraitCategory kind) =>
        definition.Category == kind || kind == TraitCategory.Advantage && definition.Category == TraitCategory.Loresheet;
    internal void Discard(TraitItemViewModel item)
    {
        if (item.Category == TraitCategory.Loresheet && Host is not null) Host.RemoveLoresheet(item);
        else Traits(item.Category).RemoveCommand.Execute(item);
    }
    private void OnChanged(object? sender, EventArgs e) => Synchronize();

    private void Synchronize()
    {
        foreach (var item in Items.ToArray())
        {
            var familyName = item.MeritGroup ?? CatalogFamily(item)?.Name ?? OtherFamily;
            if (!Families.Any(x => x.Name == familyName))
                Families.Add(new(this, familyName, []));
        }
        foreach (var family in Families) family.Refresh();
        if (_editing > 0) return;
        SyncRows(AdvantageRows, TraitCategory.Advantage);
        SyncRows(FlawRows, TraitCategory.Flaw);
    }

    private void SyncRows(ObservableCollection<MeritRowViewModel> rows, TraitCategory kind)
    {
        var items = RowItems(kind).ToArray();
        var pending = Pending(kind);
        foreach (var row in rows.Where(x => x.Item is { } item && !items.Contains(item) ||
                     x.PendingOption is { } option && !pending.Contains(option)).ToArray())
        {
            row.Dispose();
            rows.Remove(row);
        }
        foreach (var option in pending.Where(x => rows.All(row => row.PendingOption != x)))
            rows.Insert(rows.TakeWhile(x => x.IsPredator).Count(), new MeritRowViewModel(this, kind, option));
        foreach (var item in items.Where(x => rows.All(row => row.Item != x)))
        {
            // Predator lines stay grouped at the top of the list.
            var row = new MeritRowViewModel(this, kind, item);
            if (item.IsPredatorGrant) rows.Insert(rows.TakeWhile(x => x.IsPredator && !x.IsPending).Count(), row);
            else rows.Add(row);
        }
        foreach (var row in rows) row.Refresh();
    }

    // Row edits remove and add entries in several steps; rows are reconciled once at the end.
    internal void Edit(Action change)
    {
        _editing++;
        try { change(); }
        finally
        {
            if (--_editing == 0) Synchronize();
        }
    }

    internal void RemoveRow(MeritRowViewModel row) => Edit(() =>
    {
        row.Release();
        row.Dispose();
        (row.Kind == TraitCategory.Flaw ? FlawRows : AdvantageRows).Remove(row);
    });

    private IEnumerable<MeritOptionViewModel> AllOptions => Families.SelectMany(x => x.Options);
    internal AdvantageDefinition? DefinitionOf(TraitItemViewModel item) =>
        AllOptions.FirstOrDefault(x => x.Definition.Id == item.Key && x.Definition.Category == item.Category)?.Definition;
    internal MeritFamilyViewModel FamilyOf(AdvantageDefinition definition) =>
        Families.First(x => x.Options.Any(option => option.Definition.Id == definition.Id));
    internal MeritFamilyViewModel? FamilyByName(string name)
    {
        var normalized = CatalogText.Normalize(name);
        return normalized.Length == 0 ? null : Families.FirstOrDefault(x => CatalogText.Normalize(x.Name) == normalized);
    }
    internal AdvantageDefinition? FindDefinition(TraitCategory kind, string name, string category)
    {
        var normalized = CatalogText.Normalize(name);
        var matches = AllOptions.Select(x => x.Definition)
            .Where(x => Fits(x, kind) && CatalogText.Normalize(x.Name) == normalized).ToArray();
        var family = FamilyByName(category);
        return matches.FirstOrDefault(x => family is not null && FamilyOf(x) == family) ?? matches.FirstOrDefault();
    }
    internal IReadOnlyList<SuggestionItem> CategorySuggestions(TraitCategory kind) => Families
        .Where(x => x.Name == OtherFamily || x.Options.Any(option => Fits(option.Definition, kind)))
        .OrderBy(x => x.Name == OtherFamily).ThenBy(x => x.Name, StringComparer.CurrentCulture)
        .Select(x => new SuggestionItem(x.Name, x.Options.Count(option => Fits(option.Definition, kind)) is var count and > 0
            ? $"{count} {(x.Name == LoresheetFamily ? count == 1 ? "benefício" : "benefícios" : count == 1 ? "opção" : "opções")}" : "livre"))
        .ToArray();
    internal IReadOnlyList<SuggestionItem> NameSuggestions(MeritRowViewModel row)
    {
        var family = FamilyByName(row.Category);
        return (family is null ? Families : [family]).SelectMany(x => x.Options.Select(option => (Family: x, option.Definition)))
            .Where(x => Fits(x.Definition, row.Kind))
            .Where(x => x.Family.Find(x.Definition) is not { } item || item == row.Item)
            .OrderBy(x => x.Definition.Name, StringComparer.CurrentCulture)
            .Select(x => new SuggestionItem(x.Definition.Name,
                family is null ? $"{x.Family.Name} · {x.Definition.PointsLabel}" : x.Definition.PointsLabel,
                x.Definition.LoresheetCollection))
            .ToArray();
    }

    /// <summary>Catalog spelling for predator lines, whose stored names carry no accents.</summary>
    internal string DisplayName(TraitCategory kind, string name)
    {
        var normalized = CatalogText.Normalize(name);
        return AllOptions.Select(x => x.Definition).FirstOrDefault(x => x.Category == kind && CatalogText.Normalize(x.Name) == normalized)?.Name ?? name;
    }
    internal string FamilyName(TraitCategory kind, string name) =>
        FindDefinition(kind, name, "") is { } definition ? FamilyOf(definition).Name : "";

    internal MeritFamilyViewModel FamilyOf(TraitItemViewModel item) =>
        Families.FirstOrDefault(x => x.Name == item.MeritGroup) ??
        CatalogFamily(item) ??
        Families.Single(x => x.Name == "Outras caracteristicas");
    private MeritFamilyViewModel? CatalogFamily(TraitItemViewModel item) =>
        Families.FirstOrDefault(x => x.Options.Any(option => Matches(option.Definition, item)));
    internal static bool Matches(AdvantageDefinition definition, TraitItemViewModel item)
    {
        if (definition.Category != item.Category) return false;
        if (definition.Id == item.Key) return true;
        var name = CatalogText.Normalize(item.Name);
        var reference = CatalogText.Normalize(definition.Name);
        return name == reference || item.IsPredatorGrant &&
            (name.StartsWith(reference + " ", StringComparison.Ordinal) ||
             name.StartsWith(reference + "S ", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        _advantages.Changed -= OnChanged;
        _flaws.Changed -= OnChanged;
        if (_loresheets is not null) _loresheets.Changed -= OnChanged;
        foreach (var family in Families) family.Detach();
        foreach (var row in AdvantageRows.Concat(FlawRows)) row.Dispose();
    }
}

public partial class MeritFamilyViewModel : ObservableObject
{
    private readonly AdvantageCatalogViewModel _sheet;
    private bool _changingSelection;
    public MeritFamilyViewModel(AdvantageCatalogViewModel sheet, string name, IReadOnlyList<AdvantageDefinition> entries)
    {
        _sheet = sheet;
        Name = name;
        Options = entries.Select(x => new MeritOptionViewModel(this, x)).ToArray();
    }
    public string Name { get; }
    public IReadOnlyList<MeritOptionViewModel> Options { get; }
    public IReadOnlyList<MeritOptionViewModel> AdvantageOptions =>
        Options.Where(x => x.Definition.Category == TraitCategory.Advantage).ToArray();
    public IReadOnlyList<MeritOptionViewModel> FlawOptions =>
        Options.Where(x => x.Definition.Category == TraitCategory.Flaw).ToArray();
    public ObservableCollection<TraitItemViewModel> PersonalEntries { get; } = [];
    public ObservableCollection<TraitItemViewModel> AdvantagePersonalEntries { get; } = [];
    public ObservableCollection<TraitItemViewModel> FlawPersonalEntries { get; } = [];
    public bool HasAdvantages => AdvantageOptions.Count > 0 || AdvantagePersonalEntries.Count > 0;
    public bool HasFlaws => FlawOptions.Count > 0 || FlawPersonalEntries.Count > 0;
    [ObservableProperty] private string message = "";
    public string ConflictWarning => FamilyItems.Where(x => x.Rating > 0)
        .GroupBy(ExclusiveSet).Any(x => x.Key is not null && x.Select(Identity).Distinct().Count() > 1)
        ? "Ha escolhas exclusivas juntas nesta familia. Revise as entradas; nenhum dado antigo foi removido." : "";
    private IEnumerable<TraitItemViewModel> FamilyItems => _sheet.Items.Where(x => _sheet.FamilyOf(x) == this);
    internal TraitItemViewModel? Find(AdvantageDefinition definition) =>
        _sheet.Traits(definition.Category).Items.FirstOrDefault(x => x.Key == definition.Id);
    private string? ExclusiveSet(TraitItemViewModel item)
    {
        var id = Options.FirstOrDefault(x => AdvantageCatalogViewModel.Matches(x.Definition, item))?.Definition.Id;
        return Name switch
        {
            "Beleza" => "beleza",
            "Recursos" when id is "catalog-recursos" or "catalog-destituido" => "recursos",
            "Membros" when id is "catalog-status" or "catalog-suspeito" or "catalog-segregado" => "status",
            _ => null
        };
    }
    private string Identity(TraitItemViewModel item) =>
        Options.FirstOrDefault(x => AdvantageCatalogViewModel.Matches(x.Definition, item))?.Definition.Id ??
        CatalogText.Normalize(item.Name);

    internal void SetOption(AdvantageDefinition definition, int rating)
    {
        Message = "";
        var item = Find(definition);
        if (rating > 0)
        {
            var candidate = item ?? new TraitItemViewModel(new(definition.Category, definition.Id, definition.Name, rating));
            if (!ResolveConflicts(candidate))
            {
                Refresh();
                return;
            }
            rating = Math.Clamp(rating, definition.Minimum, definition.Maximum);
            if (item is null) _sheet.Traits(definition.Category).AddEntry(new(definition.Category, definition.Id, definition.Name, rating));
            else item.Rating = rating;
        }
        else if (item is not null) _sheet.Traits(definition.Category).RemoveCommand.Execute(item);
        Refresh();
    }

    [RelayCommand] private void AddAdvantage() => AddPersonal(TraitCategory.Advantage);
    [RelayCommand] private void AddFlaw() => AddPersonal(TraitCategory.Flaw);
    private void AddPersonal(TraitCategory category)
    {
        _sheet.Traits(category).AddEntry(new(category, Guid.NewGuid().ToString("N"),
            category == TraitCategory.Flaw ? "Novo defeito" : "Nova vantagem", 0, Merit: new(Name, "")));
    }
    [RelayCommand]
    private void RemovePersonal(TraitItemViewModel? item)
    {
        if (item is not null && !item.IsPredatorGrant) _sheet.Traits(item.Category).RemoveCommand.Execute(item);
    }
    private bool ResolveConflicts(TraitItemViewModel candidate)
    {
        var set = ExclusiveSet(candidate);
        var conflicts = set is null ? [] : FamilyItems.Where(x => x != candidate && x.Rating > 0 &&
            ExclusiveSet(x) == set && Identity(x) != Identity(candidate)).ToArray();
        if (conflicts.Any(x => x.IsPredatorGrant))
        {
            Message = "Esta escolha conflita com uma caracteristica do predador. Altere o predador na identidade primeiro.";
            return false;
        }
        foreach (var conflict in conflicts)
        {
            if (PersonalEntries.Contains(conflict)) conflict.Rating = 0;
            else _sheet.Traits(conflict.Category).RemoveCommand.Execute(conflict);
        }
        return true;
    }
    private void OnPersonalChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_changingSelection || e.PropertyName is not (nameof(TraitItemViewModel.Rating) or nameof(TraitItemViewModel.Name)) ||
            sender is not TraitItemViewModel { Rating: > 0, IsPredatorGrant: false } item) return;
        _changingSelection = true;
        try
        {
            Message = "";
            if (!ResolveConflicts(item)) item.Rating = 0;
        }
        finally { _changingSelection = false; }
        Refresh();
    }
    internal void Refresh()
    {
        var personal = FamilyItems.Where(x => !Options.Any(option => option.Definition.Id == x.Key)).ToArray();
        foreach (var item in personal.Where(x => !x.IsPredatorGrant && x.Category != TraitCategory.Loresheet)) item.MeritGroup ??= Name;
        foreach (var item in PersonalEntries.Where(x => !personal.Contains(x)).ToArray())
        {
            item.PropertyChanged -= OnPersonalChanged;
            PersonalEntries.Remove(item);
        }
        foreach (var item in personal.Where(x => !PersonalEntries.Contains(x)))
        {
            item.PropertyChanged += OnPersonalChanged;
            PersonalEntries.Add(item);
        }
        SyncPersonalEntries(AdvantagePersonalEntries, personal.Where(x => x.Category == TraitCategory.Advantage));
        SyncPersonalEntries(FlawPersonalEntries, personal.Where(x => x.Category == TraitCategory.Flaw));
        foreach (var option in Options) option.Refresh();
        OnPropertyChanged(nameof(ConflictWarning));
        OnPropertyChanged(nameof(AdvantageOptions));
        OnPropertyChanged(nameof(FlawOptions));
        OnPropertyChanged(nameof(HasAdvantages));
        OnPropertyChanged(nameof(HasFlaws));
    }
    private static void SyncPersonalEntries(ObservableCollection<TraitItemViewModel> target,
        IEnumerable<TraitItemViewModel> source)
    {
        var entries = source.ToArray();
        foreach (var item in target.Where(x => !entries.Contains(x)).ToArray()) target.Remove(item);
        foreach (var item in entries.Where(x => !target.Contains(x))) target.Add(item);
    }
    internal void Detach()
    {
        foreach (var item in PersonalEntries) item.PropertyChanged -= OnPersonalChanged;
    }
}

public sealed class MeritOptionViewModel(MeritFamilyViewModel family, AdvantageDefinition definition) : ObservableObject
{
    public AdvantageDefinition Definition { get; } = definition;
    public string Name => family.Find(Definition)?.Name ?? Definition.Name;
    public string Kind => Definition.Category == TraitCategory.Flaw ? "Defeito" : "Vantagem";
    public string Description => $"{Definition.PointsLabel} ponto(s). {Definition.Description}";
    public int Minimum => Definition.Minimum;
    public int Maximum => Definition.Maximum;
    public bool IsSelected
    {
        get => family.Find(Definition) is not null;
        set
        {
            if (value != IsSelected) family.SetOption(Definition, value ? Definition.Minimum : 0);
        }
    }
    public int Rating
    {
        get => family.Find(Definition)?.Rating ?? 0;
        set => family.SetOption(Definition, value);
    }
    public string Warning => family.Find(Definition) is { } item &&
        (item.Rating < Minimum || item.Rating > Maximum) ? "Pontos antigos fora da referencia; revise esta entrada." : "";
    internal void Refresh()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(IsSelected));
        OnPropertyChanged(nameof(Rating));
        OnPropertyChanged(nameof(Warning));
    }
}
