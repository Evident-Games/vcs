using Microsoft.EntityFrameworkCore;
using VCS.Application.Catalogs;
using VCS.Content.Disciplines;
using VCS.Infrastructure.Persistence;

namespace VCS.Infrastructure.Catalogs;

/// <summary>
/// Power edits live in the database; built-in powers come from the embedded catalog and are overridden by id.
/// A new discipline is created by adding a power with a new discipline name.
/// </summary>
public sealed class SqlitePowerCatalog(DatabaseAccess database, ProvidedDisciplineCatalog catalog) : IEditableCatalog<PowerDefinition>
{
    public async Task<IReadOnlyList<CatalogEntry<PowerDefinition>>> ListEntriesAsync(CancellationToken cancellationToken = default)
    {
        var edits = (await LoadAsync(cancellationToken)).ToDictionary(x => x.Id);
        var builtIn = catalog.BuiltIn.Select(x => edits.TryGetValue(x.Id, out var edit)
            ? new CatalogEntry<PowerDefinition>(x.Id, edit, true, edit != x)
            : new CatalogEntry<PowerDefinition>(x.Id, x, true, false));
        var builtInIds = catalog.BuiltIn.Select(x => x.Id).ToHashSet();
        var custom = edits.Values.Where(x => !builtInIds.Contains(x.Id)).Select(x => new CatalogEntry<PowerDefinition>(x.Id, x, false, false));
        return [.. builtIn.Concat(custom).OrderBy(x => x.Value.Discipline).ThenBy(x => x.Value.Level).ThenBy(x => x.Value.Name)];
    }

    public async Task SaveAsync(PowerDefinition value, string? replacingId = null, CancellationToken cancellationToken = default)
    {
        CatalogValidation.Text("Disciplina", value.Discipline, 80);
        CatalogValidation.Text("Nome", value.Name, 80);
        CatalogValidation.Range("Nivel", value.Level, 1, 5);
        CatalogValidation.Text("Custo", value.Cost, 200);
        CatalogValidation.Text("Parada de dados", value.DicePool, 200);
        CatalogValidation.Text("Resumo", value.Summary, 20000);
        var power = value.Id.Length == 0 ? value with { Id = (value.IsRitual ? "ritual-custom-" : "power-custom-") + Guid.NewGuid().ToString("N") } : value;
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            var record = new PowerCatalogRecord
            {
                Id = power.Id, Discipline = power.Discipline.Trim(), Name = power.Name.Trim(), Level = power.Level, Cost = power.Cost,
                DicePool = power.DicePool, Summary = power.Summary, AmalgamDiscipline = power.AmalgamDiscipline, AmalgamLevel = power.AmalgamLevel
            };
            var existing = await db.PowerCatalog.FindAsync([record.Id], cancellationToken);
            if (existing is null) db.PowerCatalog.Add(record);
            else db.Entry(existing).CurrentValues.SetValues(record);
            await db.SaveChangesAsync(cancellationToken);
        }
        finally { database.Gate.Release(); }
        await RefreshAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (catalog.BuiltIn.Any(x => x.Id == id)) throw CatalogValidation.BuiltInDelete();
        await RemoveAsync(id, cancellationToken);
    }

    public Task ResetAsync(string id, CancellationToken cancellationToken = default) =>
        catalog.BuiltIn.Any(x => x.Id == id) ? RemoveAsync(id, cancellationToken)
            : throw new InvalidOperationException("Somente itens do aplicativo podem ser restaurados.");

    public async Task RefreshAsync(CancellationToken cancellationToken = default) => catalog.Apply(await LoadAsync(cancellationToken));

    private async Task RemoveAsync(string id, CancellationToken cancellationToken)
    {
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            await db.PowerCatalog.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken);
        }
        finally { database.Gate.Release(); }
        await RefreshAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<PowerDefinition>> LoadAsync(CancellationToken cancellationToken)
    {
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            var records = await db.PowerCatalog.AsNoTracking().ToListAsync(cancellationToken);
            return [.. records.Select(x => new PowerDefinition(x.Id, x.Discipline, x.Name, x.Level, x.Cost, x.DicePool, x.Summary,
                x.AmalgamDiscipline, x.AmalgamLevel))];
        }
        finally { database.Gate.Release(); }
    }
}
