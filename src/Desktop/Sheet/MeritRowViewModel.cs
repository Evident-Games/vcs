using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Game.Content;
using CommunityToolkit.Mvvm.Input;
using Game.Content.Advantages;
using Game.Desktop.Catalogs;
using Game.Sheet;

namespace Game.Desktop.Sheet;

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
    public bool IsLoresheetCategory => Kind == TraitCategory.Advantage &&
        (IsLoresheet || CatalogText.Normalize(_category) == CatalogText.Normalize("Ficha de Conhecimento"));
    public bool CanApproveLoresheet => IsLoresheetCategory && _item is null;
    /// <summary>Predator and loresheet lines keep their name; they show it as text with a subtitle.</summary>
    public bool IsFixed => IsPredator || IsLoresheet;
    public bool CanEdit => !IsPredator;
    public bool IsLocked => !CanEdit;
    public bool CanEditName => CanEdit && !IsLoresheet;
    public bool CanEditCategory => CanEditName && !IsCatalog;
    public bool CanReallocate => Option is not null;
    private TraitItemViewModel? Investment => _item is { IsPredatorGrant: true } grant
        ? _sheet.InvestmentFor(grant) : null;
    private int GrantedPoints => _item?.IsPredatorGrant == true ? _item.Rating : 0;
    private int InvestedPoints => Investment?.Rating ?? 0;
    public bool CanInvest => _item is { IsPredatorGrant: true } grant &&
        Definition is { } definition && _sheet.GrantFor(definition) == grant;
    public bool CanEditRating => CanInvest || CanReallocate || CanEdit && !IsLoresheet;
    public string Title => IsPredator ? _sheet.DisplayName(Kind, Name) : Name;
    private int Free => _sheet.Host?.PredatorRemaining(Kind) ?? 0;
    public string Subtitle => IsLoresheet ? (Definition?.LoresheetCollection is { Length: > 0 } collection ? collection
            : _item?.Loresheet?.Collection ?? "") + (_item?.Loresheet?.Payment is LoresheetPayment.AdvantagePoints or null ? "" : " · pago com XP")
        : IsPending ? $"predador · {Free} {(Free == 1 ? "ponto livre" : "pontos livres")} · até {Maximum}"
        : IsPredator ? $"{GrantedPoints} predador + {InvestedPoints} adicionais = {Rating}" +
          (CanReallocate && Free > 0 ? $" · {Free} livre(s) do predador" : "") : "";
    public int Minimum => CanReallocate ? 0 : IsPredator ? GrantedPoints : Definition?.Minimum ?? 0;
    public int Maximum => CanInvest ? Math.Max(Rating, Definition!.Maximum)
        : Option is { } option ? _sheet.Host!.PredatorRoom(option) : Definition?.Maximum ?? 5;
    public string Reference => IsPredator
        ? (IsPending ? "Opção do predador: marque os pontos para escolhê-la." : "Concedida pelo predador.") +
          (CanInvest ? $" Total: {GrantedPoints} do predador + {InvestedPoints} adicionais. " +
              $"Limite de referência: {Definition!.PointsLabel}. Fonte: {SourceAttribution.Format(Definition!.Source)}. " +
              "Os pontos adicionais entram na sua distribuição normal; defeitos não geram vantagens extras." : "") +
          (CanReallocate ? " Ao diminuir, primeiro retire os pontos adicionais; depois devolva os pontos ao predador." : "")
        : IsLoresheet ? $"{Subtitle} · {Definition?.PointsLabel ?? _item!.Rating.ToString()} ponto(s). {Definition?.Description ?? _item!.Loresheet?.Description}" +
          (_item?.Loresheet is { } lore ? $" {(!string.IsNullOrWhiteSpace(lore.Source) ? $"Fonte: {lore.Source}. " : "")}{(!string.IsNullOrWhiteSpace(lore.Prerequisite) ? $"Pré-requisito: {lore.Prerequisite}. " : "")}" +
            (lore.PrerequisiteConfirmed || lore.LimitOverrideApproved ? $"Aprovado pelo Narrador: {lore.ApprovalNote}" : "") : "")
        : Definition is { } definition ? $"{definition.PointsLabel} ponto(s). {definition.Description} Fonte: {SourceAttribution.Format(definition.Source)}"
        : Kind == TraitCategory.Flaw ? "Defeito próprio — descreva livremente." : "Vantagem própria — descreva livremente.";
    public IReadOnlyList<SuggestionItem> CategorySuggestions => _sheet.CategorySuggestions(Kind);
    public IReadOnlyList<SuggestionItem> NameSuggestions => _sheet.NameSuggestions(this);
    public string Warning => string.Join(" ", new[]
    {
        _error,
        _item is not null && Definition is { } d && (Rating < d.Minimum || Rating > d.Maximum)
            ? $"Pontuação fora da referência ({d.PointsLabel})." : "",
        _item is null ? "" : _sheet.FamilyOf(_item).ConflictWarning
    }.Where(x => x.Length > 0));
    public bool HasWarning => Warning.Length > 0;
    [ObservableProperty] private bool isExpanded;
    [ObservableProperty] private bool prerequisiteConfirmed;
    [ObservableProperty] private bool limitOverrideApproved;
    [ObservableProperty] private string approvalNote = "";

    [RelayCommand]
    private void ConfirmLoresheet()
    {
        if (IsLoresheetCategory && _item is null && !string.IsNullOrWhiteSpace(Name))
            _sheet.Edit(() => { Resolve(Name.Trim()); Refresh(); });
    }

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
            OnPropertyChanged(nameof(IsLoresheetCategory));
            OnPropertyChanged(nameof(CanApproveLoresheet));
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
        get => _item is { IsPredatorGrant: true } grant ? grant.Rating + InvestedPoints
            : _item?.Rating ?? PendingOption?.Points ?? _rating;
        set
        {
            if (!CanEditRating) return;
            value = Math.Clamp(value, Minimum, Maximum);
            if (value == Rating) return;
            if (CanInvest)
            {
                var granted = GrantedPoints;
                if (value < granted)
                {
                    // Remove the ordinary investment before returning variable predator points.
                    _sheet.Edit(() => SetInvestment(0));
                    if (Option is { } reduced) _sheet.Host!.AllocatePredator(reduced, value);
                }
                else
                {
                    // Keep allocation choices explicit: the normal track edits only the extra portion.
                    _sheet.Edit(() => SetInvestment(value - granted));
                }
                Refresh();
                return;
            }
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

    private void SetInvestment(int points)
    {
        if (_item is not { } grant || Definition is not { } definition) return;
        var group = _sheet.Traits(grant.Category);
        if (Investment is { } investment)
        {
            if (points == 0) group.RemoveCommand.Execute(investment);
            else investment.Rating = points;
        }
        else if (points > 0)
            group.AddEntry(new(grant.Category, definition.Id, definition.Name, points));
    }

    // Variable grants have their own control; increasing the total must not silently spend free predator points.
    public int PredatorPoints
    {
        get => Option?.Points ?? 0;
        set
        {
            if (Option is not { } option || value == option.Points) return;
            var invested = InvestedPoints;
            var maximum = Definition?.Maximum ?? 5;
            var limit = Math.Max(option.Points, maximum - invested);
            _sheet.Host!.AllocatePredator(option, Math.Clamp(value, 0, limit));
            Refresh();
        }
    }
    public int PredatorMaximum => Option is { } option
        ? Math.Min(_sheet.Host!.PredatorRoom(option), Math.Max(option.Points, (Definition?.Maximum ?? 5) - InvestedPoints)) : 0;

    public string Notes
    {
        get => IsPredator && Investment is { Description.Length: > 0 } investment
            ? string.Join(Environment.NewLine, new[] { _item?.Description ?? "", investment.Description }.Where(x => x.Length > 0))
            : _item?.Description ?? _notes;
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
        _error = "";
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
                _category = family.Name;
                OnPropertyChanged(nameof(Category));
                OnPropertyChanged(nameof(IsLoresheetCategory));
                if (_sheet.Host is null) return;
                Release();
                if (_sheet.Host.AddLoresheet(definition, PrerequisiteConfirmed, LimitOverrideApproved, ApprovalNote) is { } refusal) _error = refusal;
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
            group.AddEntry(new(Kind, TraitKeys.Personal(Kind), name, rating, Merit: new(family, notes)));
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
        var lore = item?.Loresheet;
        PrerequisiteConfirmed = lore?.PrerequisiteConfirmed ?? false;
        LimitOverrideApproved = lore?.LimitOverrideApproved ?? false;
        ApprovalNote = lore?.ApprovalNote ?? "";
        OnPropertyChanged(nameof(IsLoresheetCategory));
        OnPropertyChanged(nameof(CanApproveLoresheet));
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
                     nameof(PredatorPoints), nameof(PredatorMaximum), nameof(CanInvest),
                     nameof(NameSuggestions), nameof(Warning), nameof(HasWarning) })
            OnPropertyChanged(property);
        RemoveCommand.NotifyCanExecuteChanged();
    }

    public void Dispose() => Attach(null);
}
