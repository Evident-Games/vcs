using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Desktop.Catalogs;
using VCS.Domain.Advantages;
using VCS.Domain.Characters;

namespace VCS.Desktop.Sheet;

/// <summary>One advantage or flaw line of the player sheet: picked from the catalog or written by the player.</summary>
public sealed partial class MeritRowViewModel : ObservableObject, IDisposable
{
    private readonly AdvantageCatalogViewModel _sheet;
    private TraitItemViewModel? _item;
    private string _name;
    private string _category;
    private string _notes = "";
    private string _error = "";
    private int _rating;

    internal MeritRowViewModel(AdvantageCatalogViewModel sheet, TraitCategory kind, TraitItemViewModel? item = null)
    {
        _sheet = sheet;
        Kind = kind;
        _name = item?.Name ?? "";
        _category = item is null ? "" : sheet.FamilyOf(item).Name;
        Attach(item);
    }

    /// <summary>A predator option that still has no points: it stays listed until the free points are placed.</summary>
    internal MeritRowViewModel(AdvantageCatalogViewModel sheet, TraitCategory kind, PredatorAllocationViewModel pending)
    {
        _sheet = sheet;
        Kind = kind;
        PendingOption = pending;
        _name = pending.Name;
        _category = sheet.FamilyName(kind, pending.Name);
    }

    public TraitCategory Kind { get; }
    internal PredatorAllocationViewModel? PendingOption { get; }
    public bool IsPending => PendingOption is not null;
    private PredatorAllocationViewModel? Option => PendingOption ??
        (_item is { IsPredatorGrant: true } grant ? _sheet.Host?.PredatorOption(grant) : null);
    internal TraitItemViewModel? Item => _item;
    public AdvantageDefinition? Definition => _item is null ? null : _sheet.DefinitionOf(_item);
    public bool IsCatalog => Definition is not null;
    public bool IsPredator => _item?.IsPredatorGrant == true || IsPending;
    public bool IsLoresheet => _item?.Category == TraitCategory.Loresheet;
    /// <summary>Predator and loresheet lines keep their name; they show it as text with a subtitle.</summary>
    public bool IsFixed => IsPredator || IsLoresheet;
    public bool CanEdit => !IsPredator;
    public bool IsLocked => !CanEdit;
    public bool CanEditName => CanEdit && !IsLoresheet;
    public bool CanEditCategory => CanEditName && !IsCatalog;
    public bool CanReallocate => Option is not null;
    public bool CanEditRating => CanReallocate || CanEdit && !IsLoresheet;
    public string Title => IsPredator ? _sheet.DisplayName(Kind, Name) : Name;
    private int Free => _sheet.Host?.PredatorRemaining(Kind) ?? 0;
    public string Subtitle => IsLoresheet ? (Definition?.LoresheetCollection is { Length: > 0 } collection ? collection
            : _item?.Loresheet?.Collection ?? "") + (_item?.Loresheet?.Payment is LoresheetPayment.AdvantagePoints or null ? "" : " · pago com XP")
        : IsPending ? $"predador · {Free} {(Free == 1 ? "ponto livre" : "pontos livres")} · até {Maximum}"
        : IsPredator ? CanReallocate && Free > 0 ? $"predador · {Free} {(Free == 1 ? "ponto livre" : "pontos livres")}" : "predador" : "";
    public int Minimum => CanReallocate ? 0 : Definition?.Minimum ?? 0;
    public int Maximum => Option is { } option ? _sheet.Host!.PredatorRoom(option) : Definition?.Maximum ?? 5;
    public string Reference => IsPredator
        ? (IsPending ? "Opção do predador: marque os pontos para escolhê-la." : "Concedida pelo predador.") +
          (CanReallocate ? " Diminuir devolve os pontos ao predador para escolher outra opção." : "")
        : IsLoresheet ? $"{Subtitle} · {Definition?.PointsLabel ?? _item!.Rating.ToString()} ponto(s). {Definition?.Description ?? _item!.Loresheet?.Description}"
        : Definition is { } definition ? $"{definition.PointsLabel} ponto(s). {definition.Description}"
        : Kind == TraitCategory.Flaw ? "Defeito próprio — descreva livremente." : "Vantagem própria — descreva livremente.";
    public IReadOnlyList<SuggestionItem> CategorySuggestions => _sheet.CategorySuggestions(Kind);
    public IReadOnlyList<SuggestionItem> NameSuggestions => _sheet.NameSuggestions(this);
    public string Warning => string.Join(" ", new[]
    {
        _error,
        _item is not null && Definition is { } d && (_item.Rating < d.Minimum || _item.Rating > d.Maximum)
            ? $"Pontuação fora da referência ({d.PointsLabel})." : "",
        _item is null ? "" : _sheet.FamilyOf(_item).ConflictWarning
    }.Where(x => x.Length > 0));
    public bool HasWarning => Warning.Length > 0;
    [ObservableProperty] private bool isExpanded;

    public string Category
    {
        get => _category;
        set
        {
            value ??= "";
            if (value == _category || !CanEditCategory) return;
            _category = value;
            if (_item is { } item && _sheet.FamilyByName(value) is { } family && item.MeritGroup != family.Name)
                _sheet.Edit(() =>
                {
                    item.MeritGroup = family.Name;
                    _sheet.Traits(Kind).NotifyChanged();
                });
            OnPropertyChanged();
            OnPropertyChanged(nameof(NameSuggestions));
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            value ??= "";
            if (value == _name || !CanEditName) return;
            _name = value;
            _error = "";
            _sheet.Edit(() => Resolve(value.Trim()));
            OnPropertyChanged();
            Refresh();
        }
    }

    public int Rating
    {
        get => _item?.Rating ?? PendingOption?.Points ?? _rating;
        set
        {
            if (!CanEditRating) return;
            value = Math.Clamp(value, Minimum, Maximum);
            if (value == Rating) return;
            if (Option is { } option)
            {
                _sheet.Host!.AllocatePredator(option, value);
                OnPropertyChanged();
                return;
            }
            if (_item is null) _rating = value;
            else if (Definition is { } definition) _sheet.Edit(() => _sheet.FamilyOf(definition).SetOption(definition, value));
            else _item.Rating = value;
            OnPropertyChanged();
            Refresh();
        }
    }

    public string Notes
    {
        get => _item?.Description ?? _notes;
        set
        {
            value ??= "";
            if (value == Notes || !CanEdit || IsLoresheet) return;
            if (_item is null) _notes = value;
            else
            {
                // Catalog entries only persist notes when they carry their family.
                _item.MeritGroup ??= _sheet.FamilyOf(_item).Name;
                _item.Description = value;
            }
            OnPropertyChanged();
        }
    }

    [RelayCommand] private void ToggleDetails() => IsExpanded = !IsExpanded;
    [RelayCommand(CanExecute = nameof(CanEdit))] private void Remove() => _sheet.RemoveRow(this);

    private void Resolve(string name)
    {
        var definition = name.Length == 0 ? null : _sheet.FindDefinition(Kind, name, _category);
        if (definition is not null)
        {
            var family = _sheet.FamilyOf(definition);
            var existing = family.Find(definition);
            if (existing is not null && existing == _item) return;
            if (existing is not null)
            {
                _error = "Já escolhida em outra linha.";
                return;
            }
            if (definition.Category == TraitCategory.Loresheet)
            {
                if (_sheet.Host is null) return;
                Release();
                if (_sheet.Host.AddLoresheet(definition) is { } refusal) _error = refusal;
                else if (family.Find(definition) is { } benefit)
                {
                    Attach(benefit);
                    _category = family.Name;
                }
                return;
            }
            var rating = Math.Clamp(Math.Max(Rating, definition.Minimum), definition.Minimum, definition.Maximum);
            var notes = Notes;
            Release();
            family.SetOption(definition, rating);
            if (family.Find(definition) is { } item)
            {
                Attach(item);
                _category = family.Name;
                if (notes.Length > 0) Notes = notes;
            }
            else _error = family.Message;
            return;
        }
        if (name.Length == 0)
        {
            _rating = Rating;
            _notes = Notes;
            Release();
        }
        else if (_item is { } item && !IsCatalog) item.Name = name;
        else
        {
            var rating = Math.Min(Rating, 5);
            var notes = Notes;
            Release();
            var group = _sheet.Traits(Kind);
            var family = _sheet.FamilyByName(_category)?.Name ?? AdvantageCatalogViewModel.OtherFamily;
            group.AddEntry(new(Kind, Guid.NewGuid().ToString("N"), name, rating, Merit: new(family, notes)));
            Attach(group.Items.Last());
        }
    }

    internal void Release()
    {
        if (_item is not { } item) return;
        Attach(null);
        _sheet.Discard(item);
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
        else if (e.PropertyName == nameof(TraitItemViewModel.Description)) OnPropertyChanged(nameof(Notes));
    }

    internal void Refresh()
    {
        if (_item is { } item)
        {
            if (item.Name.Trim() != _name.Trim())
            {
                _name = item.Name;
                OnPropertyChanged(nameof(Name));
            }
            var family = _sheet.FamilyOf(item).Name;
            if (IsCatalog || IsFixed || _sheet.FamilyByName(_category) is not null)
                if (_category != family)
                {
                    _category = family;
                    OnPropertyChanged(nameof(Category));
                }
        }
        foreach (var property in new[] { nameof(Definition), nameof(IsCatalog), nameof(CanEdit), nameof(IsLocked), nameof(CanEditCategory),
                     nameof(IsPredator), nameof(IsLoresheet), nameof(IsFixed), nameof(CanEditName), nameof(CanReallocate),
                     nameof(CanEditRating), nameof(Subtitle), nameof(Title), nameof(IsPending),
                     nameof(Minimum), nameof(Maximum), nameof(Reference), nameof(Rating), nameof(Notes),
                     nameof(NameSuggestions), nameof(Warning), nameof(HasWarning) })
            OnPropertyChanged(property);
        RemoveCommand.NotifyCanExecuteChanged();
    }

    public void Dispose() => Attach(null);
}
