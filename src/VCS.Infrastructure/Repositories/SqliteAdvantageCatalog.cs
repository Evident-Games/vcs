using Microsoft.EntityFrameworkCore;
using VCS.Application.Abstractions;
using VCS.Domain.Characters;
using VCS.Infrastructure.Persistence;

namespace VCS.Infrastructure.Repositories;

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
