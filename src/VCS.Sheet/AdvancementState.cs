namespace VCS.Sheet;

public enum VampireAge { Child, Neonate, Ancilla }
public enum SkillProfile { Specialist, Balanced, Generalist }

public sealed record ExperiencePurchase(DateTime Utc, TraitCategory? Category, string Key, string Name,
    int From, int To, int Cost, string Specialization = "", string Clan = "", int CampaignCost = 0, LoresheetInfo? Loresheet = null);

/// <summary>Initial rating chosen for an attribute; XP is charged only for levels above it.</summary>
public sealed record AttributeBaseRating(string Key, int Rating);

/// <summary>Initial rating chosen for one of the two chargen clan disciplines; XP is charged only for levels above it.</summary>
public sealed record DisciplineBaseRating(string Key, int Rating);

public sealed record AdvancementState
{
    public bool AutomaticExperience { get; init; }
    public bool IsCreating { get; init; } = true;
    public bool HasCreationProfile { get; init; } = true;
    public VampireAge Age { get; init; }
    public SkillProfile Profile { get; init; }
    public TraitValue[] Baseline { get; init; } = [];
    public int BaselineBloodPotency { get; init; }
    public PredatorSelection? BaselinePredatorSelection { get; init; }
    public int BaselineSpent { get; init; }
    public int BaselineCampaignSpent { get; init; }
    public int BaselineNpcPhysical { get; init; }
    public int BaselineNpcSocial { get; init; }
    public int BaselineNpcMental { get; init; }
    public ExperiencePurchase[] Purchases { get; init; } = [];
    public AttributeBaseRating[] AttributeBase { get; init; } = [];
    public DisciplineBaseRating[] DisciplineBase { get; init; } = [];

    public bool Equals(AdvancementState? other) => other is not null &&
        AutomaticExperience == other.AutomaticExperience && IsCreating == other.IsCreating && HasCreationProfile == other.HasCreationProfile && Age == other.Age && Profile == other.Profile &&
        BaselineBloodPotency == other.BaselineBloodPotency && BaselinePredatorSelection == other.BaselinePredatorSelection &&
        BaselineSpent == other.BaselineSpent && BaselineCampaignSpent == other.BaselineCampaignSpent &&
        BaselineNpcPhysical == other.BaselineNpcPhysical && BaselineNpcSocial == other.BaselineNpcSocial &&
        BaselineNpcMental == other.BaselineNpcMental && Baseline.SequenceEqual(other.Baseline) && Purchases.SequenceEqual(other.Purchases) &&
        AttributeBase.SequenceEqual(other.AttributeBase) && DisciplineBase.SequenceEqual(other.DisciplineBase);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(AutomaticExperience);
        hash.Add(IsCreating); hash.Add(HasCreationProfile); hash.Add(Age); hash.Add(Profile); hash.Add(BaselineBloodPotency); hash.Add(BaselineSpent);
        hash.Add(BaselineCampaignSpent);
        hash.Add(BaselinePredatorSelection);
        hash.Add(BaselineNpcPhysical); hash.Add(BaselineNpcSocial); hash.Add(BaselineNpcMental);
        foreach (var value in Baseline) hash.Add(value);
        foreach (var value in Purchases) hash.Add(value);
        foreach (var value in AttributeBase) hash.Add(value);
        foreach (var value in DisciplineBase) hash.Add(value);
        return hash.ToHashCode();
    }
}
