using VCS.Domain.Characters;

namespace VCS.Application.Abstractions;

public interface ICharacterRepository
{
    Task<IReadOnlyList<Character>> ListAsync(CancellationToken cancellationToken = default);
    Task<Character?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveAsync(Character character, long? expectedRevision, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, long expectedRevision, CancellationToken cancellationToken = default);
}

public sealed class CharacterConflictException()
    : Exception("A ficha foi alterada ou excluida por outra operacao. Reabra a ficha antes de continuar.");
