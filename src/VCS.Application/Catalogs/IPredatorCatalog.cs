using VCS.Content.Predators;

namespace VCS.Application.Catalogs;

public interface IPredatorCatalog
{
    Task<IReadOnlyList<PredatorDefinition>> ListAsync(CancellationToken cancellationToken = default);
}
