using Microsoft.EntityFrameworkCore;
using VCS.Application.Catalogs;
using VCS.Domain.Skills;
using VCS.Infrastructure.Persistence;

namespace VCS.Infrastructure.Catalogs;

public sealed class SqliteSkillCatalog(DatabaseAccess database) : ISkillCatalog
{
    public async Task<IReadOnlyList<SkillDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            return await db.SkillCatalog.AsNoTracking().OrderBy(x => x.Name).ToArrayAsync(cancellationToken);
        }
        finally { database.Gate.Release(); }
    }
}
