namespace VCS.Sheet;

/// <summary>
/// The stored sheet. It does not know any edition's rules: create and update it through the edition's rules
/// (CharacterRules.Create / Update in VCS.Rules.V5), which validate first.
/// </summary>
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

    public static Character Create(CharacterDetails details, IReadOnlyList<TraitValue> traits)
    {
        var character = new Character { Id = Guid.NewGuid(), CreatedUtc = DateTime.UtcNow };
        character.Update(details, traits);
        return character;
    }

    public void Update(CharacterDetails details, IReadOnlyList<TraitValue> traits)
    {
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
