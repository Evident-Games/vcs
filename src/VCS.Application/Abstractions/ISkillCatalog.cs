using VCS.Domain.Characters;

namespace VCS.Application.Abstractions;

public interface ISkillCatalog
{
    Task<IReadOnlyList<SkillDefinition>> ListAsync(CancellationToken cancellationToken = default);
}
