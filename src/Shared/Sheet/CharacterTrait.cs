namespace VCS.Sheet;

public sealed class CharacterTrait
{
    private CharacterTrait() { }

    internal CharacterTrait(TraitValue value)
    {
        Id = Guid.NewGuid();
        Category = value.Category;
        Key = value.Key;
        Name = value.Name.Trim();
        Rating = value.Rating;
        Specialization = value.Specialization;
        Loresheet = value.Loresheet;
        Merit = value.Merit;
        Skill = value.Skill;
        Power = value.Power;
    }

    public Guid Id { get; private set; }
    public TraitCategory Category { get; private set; }
    public string Key { get; private set; } = "";
    public string Name { get; private set; } = "";
    public int Rating { get; private set; }
    public string Specialization { get; private set; } = "";
    public LoresheetInfo? Loresheet { get; private set; }
    public MeritInfo? Merit { get; private set; }
    public SkillInfo? Skill { get; private set; }
    public PowerInfo? Power { get; private set; }

    public TraitValue ToValue() => new(Category, Key, Name, Rating, Specialization, Loresheet, Merit, Skill, Power);
}
