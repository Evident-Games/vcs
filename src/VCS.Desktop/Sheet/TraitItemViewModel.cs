using CommunityToolkit.Mvvm.ComponentModel;
using VCS.Content.Predators;
using VCS.Rules.V5.Advancement;
using VCS.Rules.V5.Predators;
using VCS.Sheet;

namespace VCS.Desktop.Sheet;

public partial class TraitItemViewModel : ObservableObject
{
    public TraitItemViewModel(TraitValue value)
    {
        Category = value.Category;
        Key = value.Key;
        name = value.Name;
        rating = value.Rating;
        specialization = value.Specialization;
        Loresheet = value.Loresheet;
        MeritGroup = value.Merit?.Group;
        SkillGroup = value.Skill?.Group;
        description = value.Merit?.Description ?? "";
    }
    public TraitCategory Category { get; }
    public string Key { get; }
    public LoresheetInfo? Loresheet { get; }
    public string? MeritGroup { get; internal set; }
    public string? SkillGroup { get; internal set; }
    public int SpecializationCount => CharacterAdvancement.SpecializationCount(Specialization);
    public bool IsPredatorGrant => PredatorRules.IsGrant(ToValue());
    public bool CanEditRating => !IsPredatorGrant;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditName))]
    [NotifyPropertyChangedFor(nameof(IsNameReadOnly))]
    [NotifyPropertyChangedFor(nameof(Minimum))]
    private bool hasPredatorBonus;
    public bool CanEditName => Category != TraitCategory.Attribute && !IsPredatorGrant && !HasPredatorBonus;
    public bool IsNameReadOnly => !CanEditName;
    public int Minimum => Category == TraitCategory.Discipline && HasPredatorBonus ? 1 : 0;
    public bool IsSkill => Category == TraitCategory.Skill;
    [ObservableProperty] private string name;
    [ObservableProperty] private int rating;
    [ObservableProperty] private string advancementInfo = "";
    [ObservableProperty] private string predatorSpecialization = "";
    /// <summary>The current predator lets this discipline take the +1 or this skill take the free specialization.</summary>
    [ObservableProperty] private bool isPredatorChoice;
    [ObservableProperty] private PredatorSpecialization? predatorOption;
    /// <summary>A free-text predator specialization was chosen here and still needs its text.</summary>
    [ObservableProperty] private bool isPredatorPending;
    [ObservableProperty] private string predatorTagTip = "";
    [ObservableProperty] private bool isClanDiscipline;
    [ObservableProperty] private string description = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpecializationCount))]
    private string specialization;
    public TraitValue ToValue() => new(Category, Key, Name, Rating, Specialization, Loresheet,
        MeritGroup is { } group ? new(group, Description) : null,
        SkillGroup is { } skillGroup ? new(skillGroup) : null);
}
