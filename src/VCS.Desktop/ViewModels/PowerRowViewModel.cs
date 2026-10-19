using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Application.Disciplines;
using VCS.Domain.Characters;

namespace VCS.Desktop.ViewModels;

/// <summary>A suggestion value; <see cref="Detail"/> shows at the right and <see cref="Note"/> as a muted line below.</summary>
public sealed record SuggestionItem(string Value, string Detail = "", string Note = "")
{
    public bool HasNote => Note.Length > 0;
    public override string ToString() => Value;
}

/// <summary>One power or ritual line of the sheet: picked from the catalog or written by the player.</summary>
public partial class PowerRowViewModel : ObservableObject
{
    private readonly DisciplineCatalogViewModel _owner;
    internal bool Loading;

    internal PowerRowViewModel(DisciplineCatalogViewModel owner, bool isRitual, string key)
    {
        _owner = owner;
        IsRitual = isRitual;
        Key = key;
        if (isRitual) discipline = DisciplineCatalogViewModel.RitualDiscipline;
    }

    public bool IsRitual { get; }
    public string Key { get; internal set; }
    public PowerItemViewModel? Entry { get; internal set; }
    public bool IsCatalog => Entry is { IsUnavailable: false };
    public bool CanEditLevel => !IsCatalog;
    public string SourceLabel => IsCatalog ? "Do catálogo — os campos podem ser ajustados." : "Poder próprio — preencha livremente.";

    [ObservableProperty] private string discipline = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private int level = 1;
    [ObservableProperty] private string cost = "";
    [ObservableProperty] private string dicePool = "";
    [ObservableProperty] private string duration = "";
    [ObservableProperty] private string description = "";
    [ObservableProperty] private string ingredients = "";
    [ObservableProperty] private string process = "";
    [ObservableProperty] private string system = "";
    [ObservableProperty] private string prerequisite = "";
    [ObservableProperty] private bool isExpanded;

    public IReadOnlyList<SuggestionItem> NameSuggestions => _owner.NameSuggestions(this);
    public IReadOnlyList<SuggestionItem> DisciplineSuggestions => _owner.DisciplineSuggestions(IsRitual);
    public string Warning => _owner.WarningFor(this);
    public bool HasWarning => Warning.Length > 0;

    [RelayCommand] private void Remove() => _owner.RemoveRow(this);
    [RelayCommand] private void ToggleDetails() => IsExpanded = !IsExpanded;
    [RelayCommand(CanExecute = nameof(IsCatalog))] private void RestoreCatalog() => _owner.FillFromCatalog(this);

    partial void OnDisciplineChanged(string value)
    {
        OnPropertyChanged(nameof(NameSuggestions));
        Edited();
    }
    partial void OnNameChanged(string value)
    {
        if (!Loading) _owner.Resolve(this);
        Edited();
    }
    partial void OnLevelChanged(int value) => Edited();
    partial void OnCostChanged(string value) => Edited();
    partial void OnDicePoolChanged(string value) => Edited();
    partial void OnDurationChanged(string value) => Edited();
    partial void OnDescriptionChanged(string value) => Edited();
    partial void OnIngredientsChanged(string value) => Edited();
    partial void OnProcessChanged(string value) => Edited();
    partial void OnSystemChanged(string value) => Edited();
    partial void OnPrerequisiteChanged(string value) => Edited();

    private void Edited()
    {
        if (Loading) return;
        Refresh();
        _owner.RowEdited();
    }

    internal void Refresh()
    {
        OnPropertyChanged(nameof(Warning));
        OnPropertyChanged(nameof(HasWarning));
        OnPropertyChanged(nameof(IsCatalog));
        OnPropertyChanged(nameof(CanEditLevel));
        OnPropertyChanged(nameof(SourceLabel));
        RestoreCatalogCommand.NotifyCanExecuteChanged();
    }

    internal PowerInfo Info() => new(Discipline.Trim(), Cost.Trim(), DicePool.Trim(), Duration.Trim(), Description.Trim(),
        IsRitual, Ingredients.Trim(), Process.Trim(), System.Trim(), Prerequisite.Trim());

    internal void Apply(PowerInfo info)
    {
        Discipline = info.Discipline; Cost = info.Cost; DicePool = info.DicePool; Duration = info.Duration;
        Description = info.Description; Ingredients = info.Ingredients; Process = info.Process;
        System = info.System; Prerequisite = info.Prerequisite;
    }

    public static PowerInfo Defaults(PowerDefinition definition) => new(
        definition.RequiredDiscipline, definition.Cost, definition.DicePool, "",
        definition.IsRitual ? "" : definition.Summary, definition.IsRitual, "", "",
        definition.IsRitual ? definition.Summary : "",
        definition.AmalgamDiscipline is { } amalgam ? $"Amálgama: {amalgam} {definition.AmalgamLevel}" : "");
}
