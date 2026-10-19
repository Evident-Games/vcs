using VCS.Domain.Characters;

namespace VCS.Application.Abstractions;

public interface IPredatorCatalog
{
    Task<IReadOnlyList<PredatorDefinition>> ListAsync(CancellationToken cancellationToken = default);
}
