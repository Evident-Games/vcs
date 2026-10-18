namespace VCS.Domain.Characters;

public enum TraitCategory { Attribute, Skill, Discipline, Advantage, Flaw, DisciplinePower, Loresheet }

public enum LoresheetPayment { AdvantagePoints, InitialXp, CampaignXp }
public sealed record LoresheetInfo(string Collection, bool IsLineage, string Description, LoresheetPayment Payment);
public sealed record TraitValue(TraitCategory Category, string Key, string Name, int Rating, string Specialization = "",
    LoresheetInfo? Loresheet = null);

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
    }

    public Guid Id { get; private set; }
    public TraitCategory Category { get; private set; }
    public string Key { get; private set; } = "";
    public string Name { get; private set; } = "";
    public int Rating { get; private set; }
    public string Specialization { get; private set; } = "";
    public LoresheetInfo? Loresheet { get; private set; }

    public TraitValue ToValue() => new(Category, Key, Name, Rating, Specialization, Loresheet);
}
