using Microsoft.EntityFrameworkCore;
using VCS.Application.Catalogs;
using VCS.Domain.Advantages;
using VCS.Infrastructure.Persistence;

namespace VCS.Infrastructure.Catalogs;

public sealed class SqliteAdvantageCatalog(DatabaseAccess database) : IAdvantageCatalog
{
    public async Task<IReadOnlyList<AdvantageDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            return await db.AdvantageCatalog.AsNoTracking().OrderBy(x => x.Group).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        }
        finally { database.Gate.Release(); }
    }
}
