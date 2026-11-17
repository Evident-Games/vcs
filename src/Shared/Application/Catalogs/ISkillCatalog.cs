using VCS.Content.Skills;

namespace VCS.Application.Catalogs;

public interface ISkillCatalog
{
    Task<IReadOnlyList<SkillDefinition>> ListAsync(CancellationToken cancellationToken = default);
}
