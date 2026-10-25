using Microsoft.EntityFrameworkCore;
using VCS.Application.Catalogs;
using VCS.Domain.Advantages;
using VCS.Domain.Characters;
using VCS.Infrastructure.Catalogs.Seed;
using VCS.Infrastructure.Persistence;

namespace VCS.Infrastructure.Catalogs;

public sealed class SqliteAdvantageCatalog(DatabaseAccess database) : IAdvantageCatalog, IEditableCatalog<AdvantageDefinition>
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

    public async Task<IReadOnlyList<CatalogEntry<AdvantageDefinition>>> ListEntriesAsync(CancellationToken cancellationToken = default) =>
        [.. (await ListAsync(cancellationToken)).Select(x => Seed(x.Id) is { } seed
            ? new CatalogEntry<AdvantageDefinition>(x.Id, x, true, !Same(seed, x))
            : new CatalogEntry<AdvantageDefinition>(x.Id, x, false, false))];

    public async Task SaveAsync(AdvantageDefinition value, string? replacingId = null, CancellationToken cancellationToken = default)
    {
        CatalogValidation.Text("Nome", value.Name, 80);
        CatalogValidation.Text("Grupo", value.Group, 80);
        CatalogValidation.Text("Descricao", value.Description, 20000, required: false);
        CatalogValidation.Text("Rotulo de pontos", value.PointsLabel, 80, required: false);
        CatalogValidation.Text("Colecao", value.LoresheetCollection, 80, required: value.Category == TraitCategory.Loresheet);
        if (value.Category is not (TraitCategory.Advantage or TraitCategory.Flaw or TraitCategory.Loresheet))
            throw new InvalidOperationException("Tipo deve ser vantagem, defeito ou loresheet.");
        CatalogValidation.Range("Minimo", value.Minimum, 1, 5);
        CatalogValidation.Range("Maximo", value.Maximum, value.Minimum, 5);
        var entry = Copy(value);
        if (entry.Id.Length == 0) entry.Id = "adv-custom-" + Guid.NewGuid().ToString("N");
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            var existing = await db.AdvantageCatalog.FindAsync([entry.Id], cancellationToken);
            if (existing is null) db.AdvantageCatalog.Add(entry);
            else db.Entry(existing).CurrentValues.SetValues(entry);
            await db.SaveChangesAsync(cancellationToken);
        }
        finally { database.Gate.Release(); }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (Seed(id) is not null) throw CatalogValidation.BuiltInDelete();
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            await db.AdvantageCatalog.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken);
        }
        finally { database.Gate.Release(); }
    }

    public Task ResetAsync(string id, CancellationToken cancellationToken = default) =>
        SaveAsync(Seed(id) ?? throw new InvalidOperationException("Somente itens do aplicativo podem ser restaurados."),
            null, cancellationToken);

    private static AdvantageDefinition? Seed(string id) =>
        ProvidedAdvantages.All.Concat(ProvidedLoresheets.All).FirstOrDefault(x => x.Id == id);

    private static AdvantageDefinition Copy(AdvantageDefinition x) => new()
    {
        Id = x.Id, Group = x.Group, Name = x.Name, Category = x.Category, Minimum = x.Minimum, Maximum = x.Maximum,
        PointsLabel = x.PointsLabel, Description = x.Description, LoresheetCollection = x.LoresheetCollection, IsLineage = x.IsLineage
    };

    private static bool Same(AdvantageDefinition a, AdvantageDefinition b) =>
        a.Group == b.Group && a.Name == b.Name && a.Category == b.Category && a.Minimum == b.Minimum && a.Maximum == b.Maximum &&
        a.PointsLabel == b.PointsLabel && a.Description == b.Description && a.LoresheetCollection == b.LoresheetCollection &&
        a.IsLineage == b.IsLineage;
}
