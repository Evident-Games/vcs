
using Game.Content.Advantages;

namespace Game.Application.Catalogs;

public interface IAdvantageCatalog
{
    Task<IReadOnlyList<AdvantageDefinition>> ListAsync(CancellationToken cancellationToken = default);
}
