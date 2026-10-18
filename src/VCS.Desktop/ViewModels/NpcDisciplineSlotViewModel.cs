using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using VCS.Application.Disciplines;
using VCS.Domain.Characters;

namespace VCS.Desktop.ViewModels;

public partial class NpcDisciplineSlotViewModel : ObservableObject
{
    private readonly CharacterEditorViewModel _editor;
    private TraitItemViewModel? _item;
    public NpcDisciplineSlotViewModel(CharacterEditorViewModel editor, TraitItemViewModel? item = null)
    {
        _editor = editor;
        _item = item;
        selectedDiscipline = item?.Name ?? "";
        rating = item?.Rating ?? 0;
        Options = ["", .. editor.Powers.AllPowers.Where(x => !x.Definition.IsRitual).Select(x => x.Definition.Discipline)
            .Concat(editor.Disciplines.Items.Select(x => x.Name)).Distinct().Order()];
        RefreshPowers();
    }
    public string[] Options { get; }
    public bool IsCatalogDiscipline => _editor.Powers.AllPowers.Any(x => !x.Definition.IsRitual &&
        CatalogText.Normalize(x.Definition.Discipline) == CatalogText.Normalize(SelectedDiscipline));
    public bool IsCustomDiscipline => !IsCatalogDiscipline;
    public bool CanSelectDiscipline => _item?.CanEditName ?? true;
    public int Minimum => _item?.Minimum ?? 0;
    public ObservableCollection<PowerItemViewModel> Powers { get; } = [];
    [ObservableProperty] private string selectedDiscipline;
    [ObservableProperty] private int rating;
    [ObservableProperty] private PowerItemViewModel? selectedPower;
    [ObservableProperty] private string error = "";
    partial void OnSelectedDisciplineChanged(string value)
    {
        if (value.Length != 0 && _editor.NpcDisciplines.Any(x => x != this && x.SelectedDiscipline == value))
        {
            SelectedDiscipline = _item?.Name ?? "";
            Error = "Esta disciplina ja ocupa outro espaco.";
            return;
        }
        if (_item is not null) _editor.Disciplines.RemoveCommand.Execute(_item);
        _item = null;
        if (value.Length != 0)
        {
            _editor.Disciplines.AddEntry(new(TraitCategory.Discipline, CatalogText.StableId("disc-", value), value, Rating));
            _item = _editor.Disciplines.Items.Last();
        }
        else Rating = 0;
        Error = "";
        RefreshPowers();
        OnPropertyChanged(nameof(IsCatalogDiscipline));
        OnPropertyChanged(nameof(IsCustomDiscipline));
    }
    partial void OnRatingChanged(int value)
    {
        if (_item is not null) _item.Rating = value;
    }
    private void RefreshPowers()
    {
        Powers.Clear();
        foreach (var power in _editor.Powers.AllPowers.Where(x => !x.Definition.IsRitual &&
                     CatalogText.Normalize(x.Definition.Discipline) == CatalogText.Normalize(SelectedDiscipline)))
            Powers.Add(power);
        SelectedPower = Powers.FirstOrDefault();
    }
}
