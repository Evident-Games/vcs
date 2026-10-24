using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VCS.Application.Catalogs;
using VCS.Application.Characters;
using VCS.Desktop.Catalogs;
using VCS.Desktop.Layout;
using VCS.Domain.Advancement;
using VCS.Domain.Advantages;
using VCS.Domain.Characters;
using VCS.Domain.Clans;
using VCS.Domain.Predators;
using VCS.Domain.Skills;

namespace VCS.Desktop.Sheet;

public partial class CharacterEditorViewModel : ObservableObject, INotifyDataErrorInfo, IDisposable
{
    private readonly CharacterService _service;
    private readonly ILogger<CharacterEditorViewModel> _logger;
    private readonly IAutosaveScheduler _autosave;
    private readonly SemaphoreSlim _saveGate = new(1, 1);
    private CharacterDto _current;
    private long _changeVersion;
    private bool _ready;
    private bool _disposed;
    private IReadOnlyList<ValidationIssue> _issues = [];
    private readonly Dictionary<string, SheetFieldViewModel> _extraFields;
    private static readonly HashSet<string> EditableProperties =
    [
        nameof(Name), nameof(MortalName), nameof(Clan), nameof(Generation), nameof(Sire),
        nameof(Concept), nameof(Chronicle), nameof(Description), nameof(Notes), nameof(Hunger),
        nameof(Humanity), nameof(Stains), nameof(BloodPotency), nameof(HealthSuperficial), nameof(HealthAggravated),
        nameof(WillpowerSuperficial), nameof(WillpowerAggravated)
    ];

    public CharacterEditorViewModel(CharacterDto character, CharacterService service,
        ILogger<CharacterEditorViewModel> logger, IAutosaveScheduler autosave, IDisciplineCatalog catalog)
    {
        _current = character;
        _service = service;
        _logger = logger;
        _autosave = autosave;
        var d = character.Details;
        _advancement = (d.Advancement ?? new()) with { AutomaticExperience = true, IsCreating = true, HasCreationProfile = true };
        _predatorSelection = d.PredatorSelection;
        stains = d.Stains;
        name = d.Name; mortalName = d.MortalName; clan = d.Clan; generation = d.Generation;
        sire = d.Sire; predator = d.Predator; concept = d.Concept; chronicle = d.Chronicle;
        description = d.Description; notes = d.Notes;
        hunger = d.Hunger; humanity = d.Humanity; bloodPotency = d.BloodPotency;
        healthSuperficial = d.HealthSuperficial; healthAggravated = d.HealthAggravated;
        willpowerSuperficial = d.WillpowerSuperficial; willpowerAggravated = d.WillpowerAggravated;
        npcPhysical = d.NpcPhysical; npcSocial = d.NpcSocial; npcMental = d.NpcMental;
        npcHealthCapacity = d.NpcHealthCapacity; npcWillpowerCapacity = d.NpcWillpowerCapacity;
        Attributes = new(TraitCategory.Attribute, character.Traits);
        Skills = new(TraitCategory.Skill, character.Traits);
        Disciplines = new(TraitCategory.Discipline, character.Traits);
        foreach (var discipline in catalog.Powers.Where(x => !x.IsRitual).Select(x => x.Discipline).Distinct())
            if (!Disciplines.Items.Any(x => CatalogText.Normalize(x.Name) == CatalogText.Normalize(discipline) ||
                x.Key == CatalogText.StableId("disc-", discipline)))
                Disciplines.AddEntry(new(TraitCategory.Discipline, CatalogText.StableId("disc-", discipline), discipline, 0));
        Advantages = new(TraitCategory.Advantage, character.Traits);
        Flaws = new(TraitCategory.Flaw, character.Traits);
        Loresheets = new(TraitCategory.Loresheet, character.Traits);
        Powers = new(catalog, Disciplines, character.Traits);
        Powers.Clan = clan;
        Powers.Changed += OnPowersChanged;
        var disciplineView = new System.Windows.Data.ListCollectionView(Disciplines.Items) { IsLiveSorting = true };
        disciplineView.SortDescriptions.Add(new(nameof(TraitItemViewModel.IsClanDiscipline), System.ComponentModel.ListSortDirection.Descending));
        disciplineView.SortDescriptions.Add(new(nameof(TraitItemViewModel.Name), System.ComponentModel.ListSortDirection.Ascending));
        disciplineView.LiveSortingProperties.Add(nameof(TraitItemViewModel.IsClanDiscipline));
        DisciplineItems = disciplineView;
        Disciplines.Items.CollectionChanged += (_, _) => RefreshClanDisciplines();
        RefreshClanDisciplines();
        if (d.IsNpc) RebuildNpcSlots();
        IdentityFields =
        [
            new("Ambition", "Ambicao", d.Ambition), new("Desire", "Desejo", d.Desire)
        ];
        BackgroundFields =
        [
            new("ChronicleTenets", "Principios da cronica", d.ChronicleTenets, true),
            new("Touchstones", "Pilares", d.Touchstones, true),
            new("Convictions", "Conviccoes", d.Convictions, true),
            new("ClanBane", "Perdicao do cla", d.ClanBane, true),
            new("TrueAge", "Idade verdadeira", d.TrueAge), new("ApparentAge", "Idade aparente", d.ApparentAge),
            new("BirthDate", "Data de nascimento", d.BirthDate), new("DeathDate", "Data da morte / Abraco", d.DeathDate),
            new("Appearance", "Aparencia", d.Appearance, true),
            new("DistinguishingFeatures", "Tracos distintivos", d.DistinguishingFeatures, true),
            new("History", "Historia", d.History, true)
        ];
        BloodFields =
        [
            new("Resonance", "Ressonancia", d.Resonance), new("BloodSurge", "Surto de Sangue", d.BloodSurge),
            new("DamageMend", "Quantidade recuperada", d.DamageMend), new("PowerBonus", "Bonus de poder", d.PowerBonus),
            new("RouseReroll", "Rerrolagem de Sangue", d.RouseReroll), new("FeedingPenalty", "Penalidade de alimentacao", d.FeedingPenalty),
            new("BaneSeverity", "Gravidade da Perdicao", d.BaneSeverity)
        ];
        _extraFields = ExtraFields.ToDictionary(x => x.Key);
        InitializeNpcSheet(d);
        foreach (var field in ExtraFields) field.PropertyChanged += OnExtraFieldChanged;
        experienceTotal = d.ExperienceTotal;
        experienceSpent = d.ExperienceSpent;
        campaignExperienceTotal = d.Advancement?.AutomaticExperience == true ? d.CampaignExperienceTotal :
            Math.Max(d.CampaignExperienceTotal, d.ExperienceTotal - CharacterAdvancement.StartingXp(_advancement.Age));
        campaignExperienceSpent = d.CampaignExperienceSpent;
        foreach (var group in Groups) group.Changed += OnTraitsChanged;
        SyncAttributeBase();
        SyncDisciplineBase();
        _ready = true;
        ConfigureDisplay(Display);
        RefreshAdvancement();
        ValidateDraft();
        RefreshCreation();
        ConfigurePredatorCatalog(PredatorCatalog.All);
        RecalculateExperience();
        ValidateDraft();
    }

    [ObservableProperty] private string name;
    [ObservableProperty] private string mortalName;
    [ObservableProperty] private string clan;
    [ObservableProperty] private int generation;
    [ObservableProperty] private string sire;
    [ObservableProperty] private string predator;
    [ObservableProperty] private string concept;
    [ObservableProperty] private string chronicle;
    [ObservableProperty] private string description;
    [ObservableProperty] private string notes;
    [ObservableProperty] private int hunger;
    [ObservableProperty] private int humanity;
    [ObservableProperty] private int bloodPotency;
    [ObservableProperty] private int healthSuperficial;
    [ObservableProperty] private int healthAggravated;
    [ObservableProperty] private int willpowerSuperficial;
    [ObservableProperty] private int willpowerAggravated;
    [ObservableProperty] private int npcPhysical;
    [ObservableProperty] private int npcSocial;
    [ObservableProperty] private int npcMental;
    [ObservableProperty] private int npcHealthCapacity;
    [ObservableProperty] private int npcWillpowerCapacity;
    public bool IsNpc => _current.Details.IsNpc;
    public string SheetType => IsNpc ? "NPC" : "Personagem";
    public string IdentityHeading => IsNpc ? "FICHA DE NPC" : "FICHA DE PERSONAGEM";
    public string IdentityNameAutomationLabel => IsNpc ? "Nome do NPC" : "Nome do personagem";

    public ObservableCollection<SkillGroupViewModel> SkillGroups { get; } = [];
    public void ConfigureSkillCatalog(IReadOnlyList<SkillDefinition> definitions)
    {
        var wasReady = _ready;
        _ready = false;
        try
        {
            SkillGroups.Clear();
            var catalogItems = new HashSet<TraitItemViewModel>();
            foreach (var group in definitions.GroupBy(x => x.Group).OrderBy(x => x.Key == "Físicas" ? 0 : x.Key == "Sociais" ? 1 : 2))
            {
                var items = new List<TraitItemViewModel>();
                foreach (var definition in group.OrderBy(x => x.Name))
                {
                    var item = Skills.Items.FirstOrDefault(x => x.Key == definition.Id) ??
                        Skills.Items.FirstOrDefault(x => CatalogText.Normalize(x.Name) == CatalogText.Normalize(definition.Name));
                    if (item is null)
                    {
                        Skills.AddEntry(new(TraitCategory.Skill, definition.Id, definition.Name, 0));
                        item = Skills.Items.Last();
                    }
                    catalogItems.Add(item);
                    item.Description = definition.Description;
                    items.Add(item);
                }
                SkillGroups.Add(new(group.Key, items, [], Skills));
            }
            var groups = SkillGroups.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
            var personal = Skills.Items.Where(x => !catalogItems.Contains(x)).ToArray();
            foreach (var item in personal)
            {
                var groupName = item.SkillGroup is { } savedGroup && groups.ContainsKey(savedGroup)
                    ? savedGroup : "Mentais";
                if (groups.TryGetValue(groupName, out var group))
                {
                    item.SkillGroup ??= group.Name;
                    group.PersonalItems.Insert(group.PersonalItems.Count - 1, new(group, item));
                }
            }
        }
        finally { _ready = wasReady; }
        RefreshCreation();
    }
    [ObservableProperty] private bool isDirty;
    [ObservableProperty] private bool isSaving;
    /// <summary>Theme used inside this sheet; null follows the application theme.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EffectiveSheetThemeId))]
    private string? sheetThemeId;
    public string EffectiveSheetThemeId => SheetThemeId ?? Display.ThemeId;
    [ObservableProperty] private string error = "";
    [ObservableProperty] private string status = "Salvo localmente";
    [ObservableProperty] private int experienceTotal;
    [ObservableProperty] private int experienceSpent;
    public int ExperienceAvailable => ExperienceTotal - ExperienceSpent;
    public DisplaySettingsViewModel Display { get; private set; } = new();
    public ObservableCollection<EditorSection> VisibleSections { get; } = [];
    public ObservableCollection<EditorTab> VisibleTabs { get; } = [];
    [ObservableProperty] private EditorTab? selectedTab;
    public bool NoVisibleSections => VisibleSections.Count == 0;
    /// <summary>Section arrangement for this sheet's kind (PC or NPC).</summary>
    public SheetLayout Layout => Display.LayoutFor(IsNpc);
    public void ConfigureDisplay(DisplaySettingsViewModel display)
    {
        Layout.Changed -= OnLayoutChanged;
        Display.PropertyChanged -= OnDisplayPropertyChanged;
        Display = display;
        Layout.Changed += OnLayoutChanged;
        Display.PropertyChanged += OnDisplayPropertyChanged;
        OnPropertyChanged(nameof(Display));
        OnPropertyChanged(nameof(Layout));
        OnPropertyChanged(nameof(EffectiveSheetThemeId));
        RefreshSections();
    }
    private void OnLayoutChanged(object? sender, EventArgs e) => RefreshSections();
    private void OnDisplayPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DisplaySettingsViewModel.ThemeId)) OnPropertyChanged(nameof(EffectiveSheetThemeId));
    }
    /// <summary>
    /// Syncs the shown sections with the layout, reusing existing section objects so their views are not rebuilt.
    /// Titles and bento sizes are bound live to the options, so only order, visibility and tabs need work here.
    /// </summary>
    private void RefreshSections()
    {
        var wanted = Layout.Sections.Where(x => x.IsVisible).OrderBy(x => x.SectionOrder).ToArray();
        var existing = VisibleSections.ToDictionary(x => x.Option);
        for (var i = VisibleSections.Count - 1; i >= 0; i--)
            if (!wanted.Contains(VisibleSections[i].Option)) VisibleSections.RemoveAt(i);
        for (var i = 0; i < wanted.Length; i++)
        {
            if (!existing.TryGetValue(wanted[i], out var section)) VisibleSections.Insert(i, new(wanted[i], this));
            else if (VisibleSections.IndexOf(section) is var from && from != i) VisibleSections.Move(from, i);
        }
        var tabs = VisibleSections.GroupBy(x => SheetLayout.TabKey(x.Option), StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Min(x => x.Option.TabOrder)).Select(g => new EditorTab(g.Key, g.ToArray())).ToArray();
        var sameTabs = tabs.Length == VisibleTabs.Count && tabs.Zip(VisibleTabs).All(x =>
            x.First.Title == x.Second.Title && x.First.Sections.SequenceEqual(x.Second.Sections));
        if (!sameTabs)
        {
            var tabKey = SelectedTab?.Sections.FirstOrDefault()?.Key;
            VisibleTabs.Clear();
            foreach (var tab in tabs) VisibleTabs.Add(tab);
            SelectedTab = VisibleTabs.FirstOrDefault(x => x.Sections.Any(s => s.Key == tabKey)) ?? VisibleTabs.FirstOrDefault();
        }
        OnPropertyChanged(nameof(NoVisibleSections));
    }
    public SheetFieldViewModel[] IdentityFields { get; }
    public AdvantageCatalogViewModel? AdvantageCatalog { get; private set; }
    public void ConfigureAdvantageCatalog(IReadOnlyList<AdvantageDefinition> entries)
    {
        _advantageDefinitions = entries;
        ConfigureLoresheets(entries);
        AdvantageCatalog?.Dispose();
        AdvantageCatalog = new(entries, Advantages, Flaws, this);
        entries = entries.Where(x => x.Category != TraitCategory.Loresheet).ToArray();
        OnPropertyChanged(nameof(AdvantageCatalog));
        Advantages.ConfigureCatalog(entries);
        Flaws.ConfigureCatalog(entries);
        if (IsNpc)
        {
            AdvantageCatalog.PadRows(NpcMeritSlotsPerKind);
            RebuildNpcSlots();
        }
        RefreshCreation();
    }
    public SheetFieldViewModel[] BackgroundFields { get; }
    public SheetFieldViewModel[] BloodFields { get; }
    private IEnumerable<SheetFieldViewModel> ExtraFields => IdentityFields.Concat(BackgroundFields).Concat(BloodFields);
    private void OnExtraFieldChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SheetFieldViewModel.Value)) MarkChanged();
    }
    partial void OnExperienceTotalChanged(int value) => OnPropertyChanged(nameof(ExperienceAvailable));
    partial void OnExperienceSpentChanged(int value) => OnPropertyChanged(nameof(ExperienceAvailable));

    public Guid Id => _current.Id;
    public DateTime CreatedUtc => _current.CreatedUtc;
    public DateTime UpdatedUtc => _current.UpdatedUtc;
    public TraitGroupViewModel Attributes { get; }
    public IReadOnlyList<TraitItemViewModel> PhysicalAttributes => Attributes.Items.Take(3).ToArray();
    public IReadOnlyList<TraitItemViewModel> SocialAttributes => Attributes.Items.Skip(3).Take(3).ToArray();
    public IReadOnlyList<TraitItemViewModel> MentalAttributes => Attributes.Items.Skip(6).Take(3).ToArray();
    public TraitGroupViewModel Skills { get; }
    public TraitGroupViewModel Disciplines { get; }
    public TraitGroupViewModel Advantages { get; }
    public TraitGroupViewModel Flaws { get; }
    public TraitGroupViewModel Loresheets { get; }
    public DisciplineCatalogViewModel Powers { get; }
    private TraitGroupViewModel[] Groups => [Attributes, Skills, Disciplines, Advantages, Flaws, Loresheets];
    public string[] KnownClans { get; set; } = ClanCatalog.All.Select(x => x.Name).ToArray();
    public string ClanDescription => ClanCatalog.Find(Clan)?.Description ?? "Cla personalizado ou nao selecionado.";
    public string ClanDisciplines => ClanCatalog.Find(Clan) is { } definition
        ? $"Disciplinas do cla: {string.Join(", ", definition.Disciplines)}."
        : "Disciplinas e custos de XP nao definidos para este cla. Nenhum custo sera presumido.";
    public System.ComponentModel.ICollectionView DisciplineItems { get; }
    public string ClanDisciplinesShort => ClanCatalog.Find(Clan) is { } definition
        ? string.Join(" · ", definition.Disciplines) : "clã sem disciplinas definidas";
    private void RefreshClanDisciplines()
    {
        foreach (var item in Disciplines.Items) item.IsClanDiscipline = DisciplineAdvancement.IsInClan(Clan, item.Name) == true;
    }
    public string ClanHelp => ClanCatalog.Find(Clan) is { } definition
        ? $"Ganha: {string.Join(", ", definition.Disciplines)}.{Environment.NewLine}Perdição: {definition.Bane}"
        : "Selecione um clã do catálogo para consultar suas disciplinas e perdição.";
    public string GenerationGuidance
    {
        get
        {
            var (minimum, maximum) = AgeIndex switch
            {
                (int)VampireAge.Child => (12, 16),
                (int)VampireAge.Neonate => (12, 13),
                _ => (10, 11)
            };
            var reference = $"Faixa usual: geração {minimum} a {maximum}.";
            return Generation < minimum
                ? $"{reference} Abaixo: diablerie (permitida)."
                : $"{reference} Fora dela continua permitido.";
        }
    }
    public int HealthCapacity => IsNpc ? NpcHealthCapacity : CharacterRules.HealthCapacity(Attributes.Items.Select(x => x.ToValue()));
    public int WillpowerCapacity => IsNpc ? NpcWillpowerCapacity : CharacterRules.WillpowerCapacity(Attributes.Items.Select(x => x.ToValue()));
    partial void OnNpcHealthCapacityChanged(int value) { OnPropertyChanged(nameof(HealthCapacity)); OnPropertyChanged(nameof(NpcHealthText)); }
    partial void OnNpcWillpowerCapacityChanged(int value) { OnPropertyChanged(nameof(WillpowerCapacity)); OnPropertyChanged(nameof(NpcWillpowerText)); }
    public bool HasErrors => _issues.Count != 0;
    public string ValidationSummary => string.Join(Environment.NewLine, _issues.Select(x => x.Message));
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    public event Action<CharacterDto>? Saved;

    // No entity-level errors: WPF would otherwise outline every control bound to the editor itself.
    public IEnumerable GetErrors(string? propertyName) => string.IsNullOrEmpty(propertyName)
        ? Array.Empty<string>()
        : _issues.Where(x => x.Field == propertyName).Select(x => x.Message).ToArray();

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (_ready && e.PropertyName is { } property &&
            (EditableProperties.Contains(property) || property is nameof(ExperienceTotal) or nameof(ExperienceSpent) or nameof(CampaignExperienceTotal)
                or nameof(NpcPhysical) or nameof(NpcSocial) or nameof(NpcMental) or nameof(NpcHealthCapacity) or nameof(NpcWillpowerCapacity)))
            MarkChanged();
    }

    private void OnTraitsChanged(object? sender, EventArgs e)
    {
        if (!_ready) return;
        if (ReferenceEquals(sender, Attributes)) SyncAttributeBase();
        else if (ReferenceEquals(sender, Disciplines)) SyncDisciplineBase();
        RefreshAdvancement();
        OnPropertyChanged(nameof(HealthCapacity));
        OnPropertyChanged(nameof(WillpowerCapacity));
        MarkChanged();
    }

    partial void OnClanChanged(string value)
    {
        if (!_ready) return;
        Powers.Clan = value;
        RefreshAdvancement();
        RefreshPredatorDisciplines();
        OnPropertyChanged(nameof(ClanDescription));
        OnPropertyChanged(nameof(ClanDisciplines));
        OnPropertyChanged(nameof(ClanDisciplinesShort));
        RefreshClanDisciplines();
        OnPropertyChanged(nameof(ClanHelp));
        OnPropertyChanged(nameof(PredatorHelp));
        OnPropertyChanged(nameof(PredatorDisciplineHint));
    }
    partial void OnGenerationChanged(int value) => OnPropertyChanged(nameof(GenerationGuidance));

    private void RefreshAdvancement()
    {
        foreach (var discipline in Disciplines.Items)
            discipline.AdvancementInfo = DisciplineXpText.Describe(Clan, discipline.Name, discipline.Rating);
    }

    private void OnPowersChanged(object? sender, EventArgs e) => MarkChanged();

    private void MarkChanged()
    {
        RecalculateExperience();
        _changeVersion++;
        IsDirty = true;
        Error = "";
        Status = "Alteracoes nao salvas";
        ValidateDraft();
        RefreshCreation();
        _autosave.Schedule(async () => { await FlushAsync(); });
    }

    private CharacterDto Snapshot() => _current with
    {
        Details = new()
        {
            Advancement = _advancement,
            PredatorSelection = _predatorSelection, Stains = Stains,
            IsNpc = IsNpc, NpcPhysical = NpcPhysical, NpcSocial = NpcSocial, NpcMental = NpcMental,
            NpcHealthCapacity = NpcHealthCapacity, NpcWillpowerCapacity = NpcWillpowerCapacity,
            Name = Name, MortalName = MortalName, Clan = Clan, Generation = Generation,
            Sire = Sire, Predator = Predator, Concept = Concept, Chronicle = Chronicle,
            Description = Description, Notes = Notes, Hunger = Hunger, Humanity = Humanity,
            BloodPotency = BloodPotency, HealthSuperficial = HealthSuperficial, HealthAggravated = HealthAggravated,
            WillpowerSuperficial = WillpowerSuperficial, WillpowerAggravated = WillpowerAggravated,
            Ambition = Extra("Ambition"), Desire = Extra("Desire"), Resonance = Extra("Resonance"),
            ChronicleTenets = Extra("ChronicleTenets"), Touchstones = Extra("Touchstones"), Convictions = Extra("Convictions"),
            ClanBane = Extra("ClanBane"), TrueAge = Extra("TrueAge"), ApparentAge = Extra("ApparentAge"),
            BirthDate = Extra("BirthDate"), DeathDate = Extra("DeathDate"), Appearance = Extra("Appearance"),
            DistinguishingFeatures = Extra("DistinguishingFeatures"), History = Extra("History"),
            BloodSurge = Extra("BloodSurge"), DamageMend = Extra("DamageMend"), PowerBonus = Extra("PowerBonus"),
            RouseReroll = Extra("RouseReroll"), FeedingPenalty = Extra("FeedingPenalty"), BaneSeverity = Extra("BaneSeverity"),
            ExperienceTotal = ExperienceTotal, ExperienceSpent = ExperienceSpent,
            CampaignExperienceTotal = CampaignExperienceTotal, CampaignExperienceSpent = CampaignExperienceSpent,
            NpcAttacks = [.. NpcAttacks.Select(x => x.ToValue())]
        },
        Traits = [.. Groups.SelectMany(x => x.Items).Select(x => x.ToValue()), .. Powers.SelectedValues()]
    };
    private string Extra(string key) => _extraFields[key].Value;

    private void ValidateDraft()
    {
        var snapshot = Snapshot();
        var previousFields = _issues.Select(x => x.Field).ToArray();
        _issues = CharacterRules.Validate(snapshot.Details, snapshot.Traits);
        foreach (var field in previousFields.Concat(_issues.Select(x => x.Field)).Distinct())
            ErrorsChanged?.Invoke(this, new(field));
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(ValidationSummary));
    }

    [RelayCommand]
    private async Task SaveAsync() => await FlushAsync();

    public async Task<bool> FlushAsync()
    {
        _autosave.Cancel();
        await _saveGate.WaitAsync();
        try
        {
            if (!IsDirty) return true;
            ValidateDraft();
            if (HasErrors)
            {
                Status = "Corrija a validacao para salvar";
                return false;
            }
            IsSaving = true;
            Error = "";
            Status = "Salvando...";
            var version = _changeVersion;
            var saved = await _service.SaveAsync(Snapshot());
            _current = saved;
            Saved?.Invoke(saved);
            OnPropertyChanged(nameof(UpdatedUtc));
            IsDirty = version != _changeVersion;
            Status = IsDirty ? "Alteracoes nao salvas" : $"Salvo as {DateTime.Now:HH:mm:ss}";
            if (IsDirty && !_disposed) _autosave.Schedule(async () => { await FlushAsync(); });
            return !IsDirty;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Falha ao salvar personagem {CharacterId}", Id);
            Error = exception.Message;
            Status = "Falha ao salvar; alteracoes continuam pendentes";
            return false;
        }
        finally
        {
            IsSaving = false;
            _saveGate.Release();
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _autosave.Dispose();
        Powers.Changed -= OnPowersChanged;
        Powers.Dispose();
        AdvantageCatalog?.Dispose();
        Layout.Changed -= OnLayoutChanged;
        Display.PropertyChanged -= OnDisplayPropertyChanged;
        foreach (var field in ExtraFields) field.PropertyChanged -= OnExtraFieldChanged;
        DetachNpcAttacks();
        foreach (var group in Groups)
        {
            group.Changed -= OnTraitsChanged;
            group.Dispose();
        }
    }
}
