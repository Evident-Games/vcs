using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VCS.Application.Catalogs;
using VCS.Domain.Predators;
using VCS.Infrastructure.Persistence;

namespace VCS.Infrastructure.Catalogs;

public sealed class SqlitePredatorCatalog(DatabaseAccess database) : IPredatorCatalog
{
    public async Task<IReadOnlyList<PredatorDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            var records = await db.PredatorCatalog.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
            return records.Select(x => JsonSerializer.Deserialize<PredatorDefinition>(x.Definition)
                ?? throw new InvalidDataException($"Definicao do predador {x.Id} invalida.")).ToArray();
        }
        finally { database.Gate.Release(); }
    }
}
