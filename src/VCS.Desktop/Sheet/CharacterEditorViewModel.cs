using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using VCS.Application.Catalogs;
using VCS.Application.Blood;
using VCS.Application.Advantages;
using VCS.Application.Characters;
using VCS.Application.Chronicle;
using VCS.Application.Dice;
using VCS.Application.Humanity;
using VCS.Application.Predators;
using VCS.Content;
using VCS.Content.Advantages;
using VCS.Content.Clans;
using VCS.Content.Disciplines;
using VCS.Content.Predators;
using VCS.Content.Skills;
using VCS.Desktop.Catalogs;
using VCS.Rules.V5.Validation;
using VCS.Sheet;

namespace VCS.Desktop.Sheet;

public partial class CharacterEditorViewModel : ObservableObject, INotifyDataErrorInfo, IDisposable
{
    private readonly CharacterService _service;
    private readonly ILogger<CharacterEditorViewModel> _logger;
    private readonly IAutosaveScheduler _autosave;
    private readonly BloodActions _bloodActions;
    private readonly HumanityActions _humanityActions;
    private readonly DiceActions _diceActions;
    private readonly CharacterSheetActions _characterActions;
    private readonly ChronicleActions _chronicleActions;
    private readonly VCS.Rules.Disciplines.PowerRulesetRegistry _powerRulesets;
    private readonly PredatorActions _predatorActions;
    private readonly LoresheetActions _loresheetActions;
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
        ILogger<CharacterEditorViewModel> logger, IAutosaveScheduler autosave, IDisciplineCatalog catalog,
        BloodActions? bloodActions = null, HumanityActions? humanityActions = null,
        DiceActions? diceActions = null, CharacterSheetActions? characterActions = null,
        PredatorActions? predatorActions = null, LoresheetActions? loresheetActions = null,
        ChronicleActions? chronicleActions = null,
        VCS.Rules.Disciplines.PowerRulesetRegistry? powerRulesets = null)
    {
        _current = character;
        _service = service;
        _logger = logger;
        _autosave = autosave;
        _bloodActions = bloodActions ?? new();
        _humanityActions = humanityActions ?? new();
        _diceActions = diceActions ?? new();
        _characterActions = characterActions ?? new();
        _predatorActions = predatorActions ?? new();
        _loresheetActions = loresheetActions ?? new();
        _chronicleActions = chronicleActions ?? new();
        _powerRulesets = powerRulesets ?? new([new VCS.Rules.V5.Disciplines.V5PowerRuleset()]);
        var d = character.Details;
        _advancement = (d.Advancement ?? new()) with { AutomaticExperience = true, IsCreating = true, HasCreationProfile = true };
        _predatorSelection = d.PredatorSelection;
        stains = d.Stains;
        name = d.Name; mortalName = d.MortalName; clan = d.Clan; generation = d.Generation;
        sire = d.Sire; predator = d.Predator; concept = d.Concept; chronicle = d.Chronicle;
        description = d.Description; notes = d.Notes;
        // PC sheets keep notes as a titled list; older sheets bring their single text in as the first note.
        var noteEntries = d.NoteEntries;
        if (!d.IsNpc && noteEntries.Count == 0 && !string.IsNullOrWhiteSpace(d.Notes))
        {
            noteEntries = [new(character.Id, "Anotações", d.Notes, character.UpdatedUtc)];
            notes = "";
        }
        foreach (var effect in d.PowerEffects) PowerEffects.Add(effect);
        Notebook = new(noteEntries);
        Notebook.Changed += OnNotebookChanged;
        ExceptionRegister = new(d.Exceptions);
        ExceptionRegister.Changed += OnExceptionsChanged;
        hunger = d.Hunger; humanity = d.Humanity; bloodPotency = d.BloodPotency;
        healthSuperficial = d.HealthSuperficial; healthAggravated = d.HealthAggravated;
        willpowerSuperficial = d.WillpowerSuperficial; willpowerAggravated = d.WillpowerAggravated;
        npcPhysical = d.NpcPhysical; npcSocial = d.NpcSocial; npcMental = d.NpcMental;
        npcHealthCapacity = d.NpcHealthCapacity; npcWillpowerCapacity = d.NpcWillpowerCapacity;
        Attributes = new(TraitCategory.Attribute, character.Traits);
        Skills = new(TraitCategory.Skill, character.Traits);
        selectedRollAttribute = Attributes.Items.FirstOrDefault();
        selectedRollSkill = Skills.Items.FirstOrDefault(x => x.Rating > 0) ?? Skills.Items.FirstOrDefault();
        Disciplines = new(TraitCategory.Discipline, character.Traits);
        foreach (var discipline in catalog.Powers.Where(x => !x.IsRitual).Select(x => x.Discipline).Distinct())
            if (!Disciplines.Items.Any(x => CatalogText.Normalize(x.Name) == CatalogText.Normalize(discipline) ||
                x.Key == DisciplineCatalog.KeyFor(discipline)))
                Disciplines.AddEntry(new(TraitCategory.Discipline, DisciplineCatalog.KeyFor(discipline), discipline, 0));
        Advantages = new(TraitCategory.Advantage, character.Traits);
        Flaws = new(TraitCategory.Flaw, character.Traits);
        Loresheets = new(TraitCategory.Loresheet, character.Traits);
        Powers = new(catalog, Disciplines, character.Traits, _characterActions);
        Powers.Clan = clan;
        Powers.Changed += OnPowersChanged;
        PersonalDisciplines = new(Disciplines);
        // The grid shows catalog and predator disciplines; personal ones are edited as lines below it.
        var disciplineView = new System.Windows.Data.ListCollectionView(Disciplines.Items)
        {
            IsLiveSorting = true, Filter = x => !PersonalDisciplinesViewModel.IsPersonal((TraitItemViewModel)x)
        };
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
            new("Resonance", "Ressonancia", d.Resonance), new("Dyscrasia", "Discrasia", d.Dyscrasia, true),
            new("BloodSurge", "Surto de Sangue", d.BloodSurge), new("DamageMend", "Quantidade recuperada", d.DamageMend),
            new("PowerBonus", "Bonus de poder", d.PowerBonus), new("RouseReroll", "Rerrolagem de Sangue", d.RouseReroll),
            new("FeedingPenalty", "Penalidade de alimentacao", d.FeedingPenalty), new("BaneSeverity", "Gravidade da Perdicao", d.BaneSeverity)
        ];
        BloodAnnotationFields = BloodFields.Where(x => x.Key is not ("BloodSurge" or "DamageMend" or "PowerBonus" or "RouseReroll" or "FeedingPenalty" or "BaneSeverity")).ToArray();
        _extraFields = ExtraFields.ToDictionary(x => x.Key);
        InitializeNpcSheet(d);
        foreach (var field in ExtraFields) field.PropertyChanged += OnExtraFieldChanged;
        experienceTotal = d.ExperienceTotal;
        experienceSpent = d.ExperienceSpent;
        campaignExperienceTotal = d.Advancement?.AutomaticExperience == true ? d.CampaignExperienceTotal :
            Math.Max(d.CampaignExperienceTotal, d.ExperienceTotal - _characterActions.StartingExperience(_advancement.Age));
        campaignExperienceSpent = d.CampaignExperienceSpent;
        foreach (var group in Groups) group.Changed += OnTraitsChanged;
        SyncAttributeBase();
        SyncDisciplineBase();
        RestorePowerEffects();
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
    public NotesViewModel Notebook { get; }
    public ExceptionRegisterViewModel ExceptionRegister { get; }
    private void OnExceptionsChanged(object? sender, EventArgs e) => MarkChanged();
    private void OnNotebookChanged(object? sender, EventArgs e) => MarkChanged();
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
        SelectedRollSkill ??= Skills.Items.FirstOrDefault(x => x.Rating > 0) ?? Skills.Items.FirstOrDefault();
        RefreshRollPool();
        RefreshCreation();
    }
    [ObservableProperty] private bool isDirty;
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private string error = "";
    [ObservableProperty] private string status = "Salvo localmente";
    [ObservableProperty] private int experienceTotal;
    [ObservableProperty] private int experienceSpent;
    public int ExperienceAvailable => ExperienceTotal - ExperienceSpent;
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
    public SheetFieldViewModel[] BloodAnnotationFields { get; }
    public string BloodSurgeSummary => _bloodActions.BloodSurge(EffectiveBloodPotency);
    public string BloodMendSummary => _bloodActions.DamageMend(EffectiveBloodPotency);
    public string BloodPowerBonusSummary => _bloodActions.PowerBonus(EffectiveBloodPotency);
    public string BloodRouseRerollSummary => _bloodActions.RouseReroll(EffectiveBloodPotency);
    public string BloodFeedingPenaltySummary => _bloodActions.FeedingPenalty(EffectiveBloodPotency);
    public string BloodBaneSeveritySummary => _bloodActions.BaneSeverity(EffectiveBloodPotency);
    public string BloodPotencyRuleWarning
    {
        get
        {
            var warning = _characterActions.CheckBloodPotency(Generation, BloodPotency);
            if (!warning.HasDefinedRange)
                return $"O livro-base não define uma faixa de Potência do Sangue para a {Generation}ª Geração (p. 214); confirme com o Narrador.";
            if (BloodPotency >= warning.Minimum && BloodPotency <= warning.Maximum) return "";

            var message = $"Potência do Sangue {BloodPotency} está fora da faixa da {Generation}ª Geração ({warning.Minimum}–{warning.Maximum}; livro-base, p. 214).";
            if (warning.IsThinBlood && BloodPotency > 0)
                message += " Sangues-Ralos (14ª–16ª) têm Potência 0; só podem aumentá-la se avançarem por Diablerie até a 13ª Geração ou uma geração mais antiga (p. 215).";
            return message;
        }
    }
    public bool HasBloodPotencyRuleWarning => !string.IsNullOrWhiteSpace(BloodPotencyRuleWarning);
    public string GenerationSummary => $"Geracao {Generation}";
    public IReadOnlyList<BloodGenerationDot> GenerationDots => Enumerable.Range(3, 14).Reverse()
        .Select(value => new BloodGenerationDot(value, value == Generation)).ToArray();

    private IEnumerable<SheetFieldViewModel> ExtraFields => IdentityFields.Concat(BackgroundFields).Concat(BloodFields);
    private void OnExtraFieldChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SheetFieldViewModel.Value)) return;
        MarkChanged();
        if (sender is SheetFieldViewModel { Key: "Resonance" or "Dyscrasia" })
        {
            if (!HasActiveResonanceBonus) UseResonanceBonus = false;
            RefreshRollPool();
        }
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
        ? $"Disciplinas do cla: {string.Join(", ", definition.Disciplines.Select(DisciplineCatalog.NameOf))}."
        : "Disciplinas e custos de XP nao definidos para este cla. Nenhum custo sera presumido.";
    public System.ComponentModel.ICollectionView DisciplineItems { get; }
    public PersonalDisciplinesViewModel PersonalDisciplines { get; }
    public string ClanDisciplinesShort => ClanCatalog.Find(Clan) is { } definition
        ? string.Join(" · ", definition.Disciplines.Select(DisciplineCatalog.NameOf)) : "clã sem disciplinas definidas";
    private void RefreshClanDisciplines()
    {
        foreach (var item in Disciplines.Items) item.IsClanDiscipline = _characterActions.IsDisciplineInClan(Clan, item.Name) == true;
    }
    public string ClanHelp => ClanCatalog.Find(Clan) is { } definition
        ? $"Ganha: {string.Join(", ", definition.Disciplines.Select(DisciplineCatalog.NameOf))}.{Environment.NewLine}Perdição: {definition.Bane}{Environment.NewLine}Fonte: {SourceAttribution.Format(definition.Source)}"
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
    public int HealthCapacity => IsNpc ? NpcHealthCapacity : _characterActions.HealthCapacity(Attributes.Items.Concat(Disciplines.Items).Select(x => x.ToValue()).Concat(Powers.SelectedValues()));
    public int WillpowerCapacity => IsNpc ? NpcWillpowerCapacity : _characterActions.WillpowerCapacity(Attributes.Items.Select(x => x.ToValue()));
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
        RefreshRollPool();
        RefreshPowerUse();
        OnPropertyChanged(nameof(PhysicalPowerGuidance));
        OnPropertyChanged(nameof(HealthCapacity));
        OnPropertyChanged(nameof(WillpowerCapacity));
        OnPropertyChanged(nameof(FrenzyTriggerGuidance));
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
    partial void OnGenerationChanged(int value)
    {
        OnPropertyChanged(nameof(GenerationGuidance));
        OnPropertyChanged(nameof(BloodPotencyRuleWarning));
        OnPropertyChanged(nameof(HasBloodPotencyRuleWarning));
        OnPropertyChanged(nameof(GenerationSummary));
        OnPropertyChanged(nameof(GenerationDots));
    }
    partial void OnBloodPotencyChanged(int value)
    {
        OnPropertyChanged(nameof(EffectiveBloodPotency));
        RefreshPowerUse();
        OnPropertyChanged(nameof(BloodSurgeSummary));
        OnPropertyChanged(nameof(BloodMendSummary));
        OnPropertyChanged(nameof(BloodPowerBonusSummary));
        OnPropertyChanged(nameof(BloodRouseRerollSummary));
        OnPropertyChanged(nameof(BloodFeedingPenaltySummary));
        OnPropertyChanged(nameof(BloodBaneSeveritySummary));
        OnPropertyChanged(nameof(FeedingPreview));
        OnPropertyChanged(nameof(BloodPotencyRuleWarning));
        OnPropertyChanged(nameof(HasBloodPotencyRuleWarning));
    }

    private void RefreshAdvancement()
    {
        foreach (var discipline in Disciplines.Items)
            discipline.AdvancementInfo = DisciplineXpText.Describe(
                _characterActions.DescribeDisciplineAdvancement(Clan, discipline.Name, discipline.Rating));
    }

    private void OnPowersChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(PhysicalPowerGuidance));
        OnPropertyChanged(nameof(HealthCapacity));
        MarkChanged();
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
        Notebook.Changed -= OnNotebookChanged;
        ExceptionRegister.Changed -= OnExceptionsChanged;
        foreach (var group in Groups)
        {
            group.Changed -= OnTraitsChanged;
            group.Dispose();
        }
    }
}

public sealed record BloodGenerationDot(int Number, bool IsSelected);
