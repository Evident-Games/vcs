using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Application.Catalogs;
using VCS.Content.Advantages;
using VCS.Desktop.Sheet;
using VCS.Sheet;

namespace VCS.Desktop.Catalogs;

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
