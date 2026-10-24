using VCS.Domain.Predators;

namespace VCS.Application.Catalogs;

public interface IPredatorCatalog
{
    Task<IReadOnlyList<PredatorDefinition>> ListAsync(CancellationToken cancellationToken = default);
}
