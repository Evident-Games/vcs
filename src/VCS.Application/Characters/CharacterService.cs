using VCS.Domain.Characters;

namespace VCS.Application.Characters;

public sealed class CharacterService(ICharacterRepository repository)
{
    public async Task<IReadOnlyList<CharacterSummary>> ListAsync(string? search = null, CancellationToken cancellationToken = default)
    {
        var characters = await repository.ListAsync(cancellationToken);
        var term = search?.Trim() ?? "";
        return characters.Where(c => new[] { c.Details.Name, c.Details.Clan, c.Details.Chronicle }
                .Any(value => value.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(c => c.UpdatedUtc)
            .Select(c => new CharacterSummary(c.Id, c.Details.Name, c.Details.Clan, c.Details.Chronicle, c.UpdatedUtc, c.Revision, c.Details.IsNpc))
            .ToArray();
    }

    public Task WarmUpAsync(CancellationToken cancellationToken = default) => repository.WarmUpAsync(cancellationToken);

    public async Task<CharacterDto> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        CharacterDto.From(await repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Personagem nao encontrado."));

    public async Task<CharacterDto> CreateAsync(CancellationToken cancellationToken = default)
    {
        var character = Character.Create(new() { BloodPotency = 0, Advancement = new() });
        await repository.SaveAsync(character, null, cancellationToken);
        return CharacterDto.From(character);
    }

    public async Task<CharacterDto> CreateNpcAsync(CancellationToken cancellationToken = default)
    {
        var character = Character.Create(new() { IsNpc = true, Name = "Novo NPC", BloodPotency = 0 });
        await repository.SaveAsync(character, null, cancellationToken);
        return CharacterDto.From(character);
    }

    public async Task<CharacterDto> SaveAsync(CharacterDto draft, CancellationToken cancellationToken = default)
    {
        var character = await repository.GetAsync(draft.Id, cancellationToken)
            ?? throw new CharacterConflictException();
        if (character.Revision != draft.Revision)
            throw new CharacterConflictException();
        character.Update(draft.Details, draft.Traits);
        await repository.SaveAsync(character, draft.Revision, cancellationToken);
        return CharacterDto.From(character);
    }

    public async Task<CharacterDto> DuplicateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var original = await repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Personagem nao encontrado.");
        var copy = original.Duplicate();
        await repository.SaveAsync(copy, null, cancellationToken);
        return CharacterDto.From(copy);
    }

    public Task DeleteAsync(CharacterSummary character, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(character.Id, character.Revision, cancellationToken);

    public async Task<CharacterDto> ImportAsync(CharacterDto imported, CancellationToken cancellationToken = default)
    {
        var character = Character.Create(imported.Details, imported.Traits);
        await repository.SaveAsync(character, null, cancellationToken);
        return CharacterDto.From(character);
    }
}
