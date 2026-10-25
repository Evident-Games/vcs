using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Application.Catalogs;
using VCS.Desktop.Sheet;
using VCS.Domain.Advantages;
using VCS.Domain.Characters;

namespace VCS.Desktop.Catalogs;

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
