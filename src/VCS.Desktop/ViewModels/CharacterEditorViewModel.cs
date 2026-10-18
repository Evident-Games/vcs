using System.Collections;
using System.ComponentModel;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VCS.Application.Characters;
using VCS.Application.Disciplines;
using VCS.Desktop.Services;
using VCS.Domain.Characters;

namespace VCS.Desktop.ViewModels;

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
    private static readonly HashSet<string> EditableProperties =
    [
        nameof(Name), nameof(MortalName), nameof(Clan), nameof(Generation), nameof(Sire), nameof(Predator),
        nameof(Concept), nameof(Chronicle), nameof(Description), nameof(Notes), nameof(Hunger),
        nameof(Humanity), nameof(BloodPotency), nameof(HealthSuperficial), nameof(HealthAggravated),
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
        _advancement = d.Advancement;
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
        foreach (var discipline in catalog.Powers.Where(x => !x.IsRitual && !d.IsNpc).Select(x => x.Discipline).Distinct())
            if (!Disciplines.Items.Any(x => CatalogText.Normalize(x.Name) == CatalogText.Normalize(discipline) ||
                x.Key == CatalogText.StableId("disc-", discipline)))
                Disciplines.AddEntry(new(TraitCategory.Discipline, CatalogText.StableId("disc-", discipline), discipline, 0));
        Advantages = new(TraitCategory.Advantage, character.Traits);
        Flaws = new(TraitCategory.Flaw, character.Traits);
        Loresheets = new(TraitCategory.Loresheet, character.Traits);
        Powers = new(catalog, Disciplines, character.Traits);
        Powers.Clan = clan;
        Powers.Changed += OnPowersChanged;
        if (d.IsNpc)
        {
            foreach (var item in Disciplines.Items) NpcDisciplines.Add(new(this, item));
            while (NpcDisciplines.Count < 6) NpcDisciplines.Add(new(this));
        }
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
        foreach (var field in ExtraFields) field.PropertyChanged += OnExtraFieldChanged;
        experienceTotal = d.ExperienceTotal;
        experienceSpent = d.ExperienceSpent;
        campaignExperienceTotal = d.CampaignExperienceTotal;
        campaignExperienceSpent = d.CampaignExperienceSpent;
        foreach (var group in Groups) group.Changed += OnTraitsChanged;
        _ready = true;
        ConfigureDisplay(Display);
        RefreshAdvancement();
        ValidateDraft();
        RefreshCreation();
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
    public ObservableCollection<NpcDisciplineSlotViewModel> NpcDisciplines { get; } = [];
    [RelayCommand] private void AddNpcDiscipline() => NpcDisciplines.Add(new(this));
    public ObservableCollection<SkillGroupViewModel> SkillGroups { get; } = [];
    public void ConfigureSkillCatalog(IReadOnlyList<SkillDefinition> definitions)
    {
        var wasReady = _ready;
        _ready = false;
        try
        {
            SkillGroups.Clear();
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
                    item.Description = definition.Description;
                    items.Add(item);
                }
                SkillGroups.Add(new(group.Key, items));
            }
        }
        finally { _ready = wasReady; }
        RefreshCreation();
    }
    [ObservableProperty] private bool isDirty;
    [ObservableProperty] private bool isSaving;
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
    public void ConfigureDisplay(DisplaySettingsViewModel display)
    {
        Display.LayoutChanged -= OnDisplayChanged;
        Display = display;
        Display.LayoutChanged += OnDisplayChanged;
        OnPropertyChanged(nameof(Display));
        RefreshSections();
    }
    private void OnDisplayChanged(object? sender, EventArgs e) => RefreshSections();
    private void RefreshSections()
    {
        var tabKey = SelectedTab?.Sections.FirstOrDefault()?.Key;
        VisibleSections.Clear();
        foreach (var section in Display.Sections.Where(x => x.IsVisible).OrderBy(x => x.SectionOrder))
            VisibleSections.Add(new(section.Key, string.IsNullOrWhiteSpace(section.Caption) ? section.Title : section.Caption.Trim(), this));
        VisibleTabs.Clear();
        foreach (var group in VisibleSections.GroupBy(x =>
        {
            var name = Display.Sections.Single(s => s.Key == x.Key).TabName.Trim();
            return name.Length == 0 ? x.Title : name;
        }, StringComparer.OrdinalIgnoreCase).OrderBy(g => g.Min(x => Display.Sections.Single(s => s.Key == x.Key).TabOrder)))
            VisibleTabs.Add(new(group.Key, group.ToArray()));
        SelectedTab = VisibleTabs.FirstOrDefault(x => x.Sections.Any(s => s.Key == tabKey)) ?? VisibleTabs.FirstOrDefault();
        OnPropertyChanged(nameof(NoVisibleSections));
    }
    public SheetFieldViewModel[] IdentityFields { get; }
    public AdvantageCatalogViewModel? AdvantageCatalog { get; private set; }
    private IReadOnlyList<AdvantageDefinition> _meritOptions = [];
    public ObservableCollection<NpcMeritSlotViewModel> NpcMerits { get; } = [];
    [RelayCommand] private void AddNpcMerit() => NpcMerits.Add(new(this, _meritOptions));
    public void ConfigureAdvantageCatalog(IReadOnlyList<AdvantageDefinition> entries)
    {
        _advantageDefinitions = entries;
        ConfigureLoresheets(entries);
        entries = entries.Where(x => x.Category != TraitCategory.Loresheet).ToArray();
        if (IsNpc)
        {
            var existing = Advantages.Items.Concat(Flaws.Items).ToArray();
            _meritOptions = entries.Concat(existing.Where(x => !entries.Any(e => e.Id == x.Key && e.Category == x.Category))
                .Select(x => new AdvantageDefinition { Id = x.Key, Name = x.Name, Category = x.Category,
                    Description = "Entrada pessoal.", Minimum = 0, Maximum = 5 })).OrderBy(x => x.Name).ToArray();
            NpcMerits.Clear();
            foreach (var item in existing) NpcMerits.Add(new(this, _meritOptions, item));
            while (NpcMerits.Count < 6) NpcMerits.Add(new(this, _meritOptions));
        }
        AdvantageCatalog = new(entries, Advantages, Flaws);
        OnPropertyChanged(nameof(AdvantageCatalog));
        Advantages.ConfigureCatalog(entries);
        Flaws.ConfigureCatalog(entries);
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
    public int HealthCapacity => IsNpc ? NpcHealthCapacity : CharacterRules.HealthCapacity(Attributes.Items.Select(x => x.ToValue()));
    public int WillpowerCapacity => IsNpc ? NpcWillpowerCapacity : CharacterRules.WillpowerCapacity(Attributes.Items.Select(x => x.ToValue()));
    partial void OnNpcHealthCapacityChanged(int value) => OnPropertyChanged(nameof(HealthCapacity));
    partial void OnNpcWillpowerCapacityChanged(int value) => OnPropertyChanged(nameof(WillpowerCapacity));
    public bool HasErrors => _issues.Count != 0;
    public string ValidationSummary => string.Join(Environment.NewLine, _issues.Select(x => x.Message));
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    public event Action<CharacterDto>? Saved;

    public IEnumerable GetErrors(string? propertyName) =>
        _issues.Where(x => string.IsNullOrEmpty(propertyName) || x.Field == propertyName).Select(x => x.Message).ToArray();

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
        OnPropertyChanged(nameof(ClanDescription));
        OnPropertyChanged(nameof(ClanDisciplines));
    }

    private void RefreshAdvancement()
    {
        foreach (var discipline in Disciplines.Items)
            discipline.AdvancementInfo = DisciplineXpText.Describe(Clan, discipline.Name, discipline.Rating);
    }

    private void OnPowersChanged(object? sender, EventArgs e) => MarkChanged();

    private void MarkChanged()
    {
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
            CampaignExperienceTotal = CampaignExperienceTotal, CampaignExperienceSpent = CampaignExperienceSpent
        },
        Traits = [.. Groups.SelectMany(x => x.Items).Select(x => x.ToValue()), .. Powers.SelectedValues()]
    };
    private string Extra(string key) => ExtraFields.Single(x => x.Key == key).Value;

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
        Display.LayoutChanged -= OnDisplayChanged;
        foreach (var field in ExtraFields) field.PropertyChanged -= OnExtraFieldChanged;
        foreach (var group in Groups)
        {
            group.Changed -= OnTraitsChanged;
            group.Dispose();
        }
    }
}
