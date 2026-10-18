using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Domain.Characters;

namespace VCS.Desktop.ViewModels;

public partial class NpcMeritSlotViewModel : ObservableObject
{
    private readonly CharacterEditorViewModel _editor;
    private TraitItemViewModel? _item;
    public NpcMeritSlotViewModel(CharacterEditorViewModel editor, IReadOnlyList<AdvantageDefinition> options, TraitItemViewModel? item = null)
    {
        _editor = editor;
        Options = options;
        _item = item;
        selected = item is null ? null : options.First(x => x.Id == item.Key && x.Category == item.Category);
        rating = item?.Rating ?? 0;
    }
    public IReadOnlyList<AdvantageDefinition> Options { get; }
    [ObservableProperty] private AdvantageDefinition? selected;
    [ObservableProperty] private int rating;
    [ObservableProperty] private string error = "";
    public string Kind => Selected?.Category == TraitCategory.Flaw ? "Defeito" : Selected is null ? "" : "Vantagem";
    partial void OnSelectedChanged(AdvantageDefinition? value)
    {
        if (value is not null && _editor.NpcMerits.Any(x => x != this && x.Selected?.Id == value.Id))
        {
            Selected = _item is null ? null : Options.First(x => x.Id == _item.Key);
            Error = "Esta entrada ja ocupa outro espaco.";
            return;
        }
        if (_item is not null) Group(_item.Category).RemoveCommand.Execute(_item);
        _item = null;
        if (value is not null)
        {
            Group(value.Category).AddEntry(new(value.Category, value.Id, value.Name, Rating));
            _item = Group(value.Category).Items.Last();
        }
        Error = "";
        OnPropertyChanged(nameof(Kind));
    }
    private TraitGroupViewModel Group(TraitCategory category) => category == TraitCategory.Flaw ? _editor.Flaws : _editor.Advantages;
    partial void OnRatingChanged(int value) { if (_item is not null) _item.Rating = value; }
    [RelayCommand] private void Clear() { Selected = null; Rating = 0; }
}
