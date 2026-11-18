using Game.Content.Skills;

namespace Game.Application.Catalogs;

public interface ISkillCatalog
{
    Task<IReadOnlyList<SkillDefinition>> ListAsync(CancellationToken cancellationToken = default);
}
