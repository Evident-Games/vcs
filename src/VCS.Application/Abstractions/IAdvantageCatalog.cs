using VCS.Domain.Characters;

namespace VCS.Application.Abstractions;

public interface IAdvantageCatalog
{
    Task<IReadOnlyList<AdvantageDefinition>> ListAsync(CancellationToken cancellationToken = default);
}
