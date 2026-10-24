using VCS.Domain.Advantages;

namespace VCS.Application.Catalogs;

public interface IAdvantageCatalog
{
    Task<IReadOnlyList<AdvantageDefinition>> ListAsync(CancellationToken cancellationToken = default);
}
