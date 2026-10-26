using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using VCS.Content;
using VCS.Content.Disciplines;
using VCS.Desktop.Catalogs;
using VCS.Sheet;

namespace VCS.Desktop.Sheet;

// One NPC discipline line. It points to an entry of the sheet's discipline group instead of creating
// parallel data, so catalog disciplines keep their keys and personal names stay unique.
public sealed partial class NpcDisciplineSlotViewModel : ObservableObject, IDisposable
{
    private readonly CharacterEditorViewModel _editor;
    private TraitItemViewModel? _item;
    private string _selected;
    private int _rating;

    public NpcDisciplineSlotViewModel(CharacterEditorViewModel editor, TraitItemViewModel? item = null)
    {
        _editor = editor;
        _selected = item?.Name ?? "";
        Attach(item);
        Options = [.. editor.Powers.AllPowers.Where(x => !x.Definition.IsRitual).Select(x => x.Definition.Discipline)
            .Concat(editor.Disciplines.Items.Select(x => x.Name)).Where(x => x.Length != 0).Distinct().Order()
            .Select(x => new SuggestionItem(x))];
        RefreshPowers();
    }

    public SuggestionItem[] Options { get; }
    public bool IsCatalogDiscipline => IsCatalogName(_selected);
    public bool IsCustomDiscipline => !IsCatalogDiscipline;
    public bool CanSelectDiscipline => _item?.CanEditName ?? true;
    public int Minimum => _item?.Minimum ?? 0;
    public ObservableCollection<PowerItemViewModel> Powers { get; } = [];
    [ObservableProperty] private PowerItemViewModel? selectedPower;
    [ObservableProperty] private string error = "";

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void Remove()
    {
        Release();
        _editor.RemoveNpcDiscipline(this);
    }

    public string SelectedDiscipline
    {
        get => _selected;
        set
        {
            value ??= "";
            if (value == _selected) return;
            _selected = value;
            OnPropertyChanged();
            Resolve();
        }
    }

    public int Rating
    {
        get => _item?.Rating ?? _rating;
        set
        {
            value = Math.Clamp(value, Minimum, 5);
            if (value == Rating) return;
            if (_item is not null) _item.Rating = value;
            else _rating = value;
            OnPropertyChanged();
        }
    }

    private void Resolve()
    {
        var name = _selected.Trim();
        var key = CatalogText.Normalize(name);
        var duplicate = key.Length != 0 && _editor.NpcDisciplines.Any(x => x != this && CatalogText.Normalize(x.SelectedDiscipline) == key);
        Error = duplicate ? "Esta disciplina já ocupa outro espaço." : "";
        var rating = Rating;
        if (key.Length == 0 || duplicate)
        {
            Release();
            if (key.Length == 0) _rating = 0;
        }
        else if (_editor.Disciplines.Items.FirstOrDefault(x => CatalogText.Normalize(x.Name) == key) is { } match)
        {
            if (match != _item)
            {
                Release();
                Attach(match);
                match.Rating = Math.Max(match.Minimum, rating);
            }
        }
        else if (_item is { CanEditName: true } && !IsCatalogName(_item.Name)) _item.Name = name;
        else
        {
            Release();
            var id = DisciplineCatalog.KeyFor(name);
            if (_editor.Disciplines.Items.Any(x => x.Key == id)) id = TraitKeys.Personal(TraitCategory.Discipline);
            _editor.Disciplines.AddEntry(new(TraitCategory.Discipline, id, name, rating));
            Attach(_editor.Disciplines.Items.Last());
        }
        RefreshPowers();
        OnPropertyChanged(nameof(Rating));
        OnPropertyChanged(nameof(Minimum));
        OnPropertyChanged(nameof(CanSelectDiscipline));
        OnPropertyChanged(nameof(IsCatalogDiscipline));
        OnPropertyChanged(nameof(IsCustomDiscipline));
    }

    // Catalog disciplines stay on the sheet at zero dots; personal ones are removed when the line changes.
    private void Release()
    {
        if (_item is not { } item) return;
        _rating = item.Rating;
        Attach(null);
        if (IsCatalogName(item.Name)) item.Rating = item.Minimum;
        else _editor.Disciplines.RemoveCommand.Execute(item);
    }

    private void Attach(TraitItemViewModel? item)
    {
        if (_item is not null) _item.PropertyChanged -= OnItemChanged;
        _item = item;
        if (item is not null) item.PropertyChanged += OnItemChanged;
    }

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TraitItemViewModel.Rating)) OnPropertyChanged(nameof(Rating));
        else if (e.PropertyName == nameof(TraitItemViewModel.Name) && _item is { } item &&
                 CatalogText.Normalize(item.Name) != CatalogText.Normalize(_selected))
        {
            _selected = item.Name;
            OnPropertyChanged(nameof(SelectedDiscipline));
            RefreshPowers();
        }
        else if (e.PropertyName is nameof(TraitItemViewModel.Minimum) or nameof(TraitItemViewModel.CanEditName))
        {
            OnPropertyChanged(nameof(Minimum));
            OnPropertyChanged(nameof(CanSelectDiscipline));
        }
    }

    private bool IsCatalogName(string name) => _editor.Powers.AllPowers.Any(x => !x.Definition.IsRitual &&
        CatalogText.Normalize(x.Definition.Discipline) == CatalogText.Normalize(name));

    private void RefreshPowers()
    {
        Powers.Clear();
        var key = CatalogText.Normalize(_selected);
        foreach (var power in _editor.Powers.AllPowers.Where(x => !x.Definition.IsRitual &&
                     key.Length != 0 && CatalogText.Normalize(x.Definition.Discipline) == key))
            Powers.Add(power);
        SelectedPower = Powers.FirstOrDefault();
    }

    public void Dispose() => Attach(null);
}