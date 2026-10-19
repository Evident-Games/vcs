namespace VCS.Domain.Characters;

public sealed class Character
{
    private Character() { }
    private readonly List<CharacterTrait> _traits = [];
    public Guid Id { get; private set; }
    public CharacterDetails Details { get; private set; } = new();
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }
    public long Revision { get; private set; }
    public IReadOnlyCollection<CharacterTrait> Traits => _traits.AsReadOnly();
    public int HealthCapacity => Details.IsNpc ? Details.NpcHealthCapacity : CharacterRules.HealthCapacity(_traits.Select(x => x.ToValue()));
    public int WillpowerCapacity => Details.IsNpc ? Details.NpcWillpowerCapacity : CharacterRules.WillpowerCapacity(_traits.Select(x => x.ToValue()));

    public static Character Create(CharacterDetails details, IReadOnlyList<TraitValue>? traits = null)
    {
        var character = new Character { Id = Guid.NewGuid(), CreatedUtc = DateTime.UtcNow };
        character.Update(details, traits ?? CharacterRules.DefaultAttributes);
        return character;
    }

    public void Update(CharacterDetails details, IReadOnlyList<TraitValue> traits)
    {
        var issues = CharacterRules.Validate(details, traits);
        if (issues.Count != 0)
            throw new CharacterValidationException(issues);
        Details = details with
        {
            Name = details.Name.Trim(), Clan = details.Clan.Trim(), Chronicle = details.Chronicle.Trim()
        };
        _traits.Clear();
        _traits.AddRange(traits.Select(x => new CharacterTrait(x)));
        UpdatedUtc = DateTime.UtcNow;
        Revision++;
    }

    public Character Duplicate() => Create(Details with
    {
        Name = $"{Details.Name[..Math.Min(Details.Name.Length, 112)]} (copia)"
    }, _traits.Select(x => x.ToValue()).ToArray());
}
