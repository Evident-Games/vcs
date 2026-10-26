using VCS.Sheet;

namespace VCS.Application.Characters;

public sealed record CharacterDto(
    Guid Id, CharacterDetails Details, TraitValue[] Traits,
    DateTime CreatedUtc, DateTime UpdatedUtc, long Revision)
{
    public static CharacterDto From(Character character) => new(
        character.Id, character.Details, character.Traits.Select(x => x.ToValue()).ToArray(),
        character.CreatedUtc, character.UpdatedUtc, character.Revision);
}

public sealed record CharacterSummary(
    Guid Id, string Name, string Clan, string Chronicle, DateTime UpdatedUtc, long Revision, bool IsNpc = false)
{
    public string SheetType => IsNpc ? "NPC" : "Personagem";
}
