using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Application.Disciplines;
using VCS.Domain.Characters;

namespace VCS.Desktop.ViewModels;

public sealed class AdvantageCatalogViewModel : IDisposable
{
    private readonly TraitGroupViewModel _advantages;
    private readonly TraitGroupViewModel _flaws;
    public ObservableCollection<MeritFamilyViewModel> Families { get; } = [];

    public AdvantageCatalogViewModel(IReadOnlyList<AdvantageDefinition> entries,
        TraitGroupViewModel advantages, TraitGroupViewModel flaws)
    {
        _advantages = advantages;
        _flaws = flaws;
        foreach (var group in entries.Where(x => x.Category is TraitCategory.Advantage or TraitCategory.Flaw)
                     .GroupBy(x => FamilyName(x.Group)).OrderBy(x => x.Key))
            Families.Add(new(this, group.Key, group.ToArray()));
        if (!Families.Any(x => x.Name == "Outras caracteristicas"))
            Families.Add(new(this, "Outras caracteristicas", []));
        advantages.Changed += OnChanged;
        flaws.Changed += OnChanged;
        Synchronize();
    }

    private static string FamilyName(string name) => name == "Visual" ? "Beleza" : name;
    internal IEnumerable<TraitItemViewModel> Items => _advantages.Items.Concat(_flaws.Items);
    internal TraitGroupViewModel Traits(TraitCategory category) => category == TraitCategory.Flaw ? _flaws : _advantages;
    private void OnChanged(object? sender, EventArgs e) => Synchronize();

    private void Synchronize()
    {
        foreach (var item in Items.ToArray())
        {
            var familyName = item.MeritGroup ?? CatalogFamily(item)?.Name ?? "Outras caracteristicas";
            if (!Families.Any(x => x.Name == familyName))
                Families.Add(new(this, familyName, []));
        }
        foreach (var family in Families) family.Refresh();
    }

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
        foreach (var family in Families) family.Detach();
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
    public ObservableCollection<TraitItemViewModel> PersonalEntries { get; } = [];
    [ObservableProperty] private string message = "";
    public string SelectionHint => Name switch
    {
        "Beleza" => "Escolha uma opcao de beleza.",
        "Recursos" => "Recursos ou Destituido: escolha uma opcao.",
        "Membros" => "Status, Suspeito e Segregado sao exclusivos entre si. As demais opcoes podem acumular.",
        _ => "As opcoes desta familia podem acumular."
    };
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
        foreach (var item in personal.Where(x => !x.IsPredatorGrant)) item.MeritGroup ??= Name;
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
        foreach (var option in Options) option.Refresh();
        OnPropertyChanged(nameof(ConflictWarning));
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
