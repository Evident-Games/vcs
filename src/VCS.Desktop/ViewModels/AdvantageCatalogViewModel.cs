using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Domain.Characters;
using VCS.Application.Disciplines;

namespace VCS.Desktop.ViewModels;

public partial class AdvantageCatalogViewModel : ObservableObject
{
    private readonly IReadOnlyList<AdvantageDefinition> _entries;
    private readonly TraitGroupViewModel _advantages;
    private readonly TraitGroupViewModel _flaws;

    public AdvantageCatalogViewModel(IReadOnlyList<AdvantageDefinition> entries,
        TraitGroupViewModel advantages, TraitGroupViewModel flaws)
    {
        _entries = entries.Where(x => x.Category is TraitCategory.Advantage or TraitCategory.Flaw).ToArray();
        _advantages = advantages;
        _flaws = flaws;
        Refresh();
    }

    public ObservableCollection<AdvantageDefinition> Entries { get; } = [];
    [ObservableProperty] private string search = "";
    [ObservableProperty] private AdvantageDefinition? selectedEntry;
    [ObservableProperty] private int points = 1;
    [ObservableProperty] private string message = "";
    public string[] Types { get; } = ["Todos", "Vantagens", "Defeitos"];
    [ObservableProperty] private string selectedType = "Todos";
    public bool IsEmpty => Entries.Count == 0;
    public string CountLabel => $"{Entries.Count} entrada(s) na busca";
    public int Minimum => SelectedEntry?.Minimum ?? 1;
    public int Maximum => SelectedEntry?.Maximum ?? 5;
    public string SelectedSummary => SelectedEntry is { } entry
        ? $"{entry.Group} | {(entry.Category == TraitCategory.Flaw ? "Defeito" : "Vantagem")} | {entry.PointsLabel} ponto(s)"
        : "Selecione uma entrada do catalogo.";
    public string Description => SelectedEntry?.Description ?? "";
    public bool HasSelection => SelectedEntry is not null;

    partial void OnSearchChanged(string value) => Refresh();
    partial void OnSelectedTypeChanged(string value) => Refresh();
    partial void OnSelectedEntryChanged(AdvantageDefinition? value)
    {
        Points = value?.Minimum ?? 1;
        OnPropertyChanged(nameof(Minimum));
        OnPropertyChanged(nameof(Maximum));
        OnPropertyChanged(nameof(SelectedSummary));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(HasSelection));
        Message = "";
    }

    private void Refresh()
    {
        var selectedId = SelectedEntry?.Id;
        var term = CatalogText.Normalize(Search);
        Entries.Clear();
        foreach (var entry in _entries.Where(x => CatalogText.Normalize($"{x.Name} {x.Group} {x.Description} " +
                     (x.Category == TraitCategory.Flaw ? "Defeito Desvantagem" : "Vantagem")).Contains(term, StringComparison.Ordinal) &&
                     (SelectedType == "Todos" || (x.Category == TraitCategory.Flaw ? SelectedType == "Defeitos" : SelectedType == "Vantagens"))))
            Entries.Add(entry);
        SelectedEntry = Entries.FirstOrDefault(x => x.Id == selectedId) ?? Entries.FirstOrDefault();
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(CountLabel));
    }

    [RelayCommand]
    private void AddSelected()
    {
        if (SelectedEntry is not { } entry)
        {
            Message = "Selecione uma vantagem ou defeito.";
            return;
        }
        if (Points < entry.Minimum || Points > entry.Maximum)
        {
            Message = $"Escolha de {entry.Minimum} a {entry.Maximum} pontos para esta entrada.";
            return;
        }
        var group = entry.Category == TraitCategory.Flaw ? _flaws : _advantages;
        if (group.Items.Any(x => x.Key == entry.Id))
        {
            Message = "Esta entrada ja esta na ficha. Edite os pontos na lista abaixo.";
            return;
        }
        group.AddEntry(new(entry.Category, entry.Id, entry.Name, Points));
        Message = "Entrada adicionada a ficha.";
    }
}
