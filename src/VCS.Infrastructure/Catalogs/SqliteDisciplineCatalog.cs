using Microsoft.EntityFrameworkCore;
using VCS.Application.Catalogs;
using VCS.Content.Clans;
using VCS.Content.Disciplines;
using VCS.Infrastructure.Persistence;

namespace VCS.Infrastructure.Catalogs;

/// <summary>
/// Built-in disciplines come from <see cref="DisciplineCatalog"/>; the database holds the disciplines the user created.
/// Powers and clans refer to disciplines by id, so a discipline in use cannot be deleted.
/// </summary>
public sealed class SqliteDisciplineCatalog(DatabaseAccess database) : IEditableCatalog<DisciplineDefinition>
{
    public async Task<IReadOnlyList<CatalogEntry<DisciplineDefinition>>> ListEntriesAsync(CancellationToken cancellationToken = default)
    {
        var custom = await LoadAsync(cancellationToken);
        return [.. DisciplineCatalog.Merge(custom)
            .Select(x => new CatalogEntry<DisciplineDefinition>(x.Id, x, DisciplineCatalog.IsBuiltIn(x.Id), false))
            .OrderBy(x => x.Value.Name, StringComparer.CurrentCultureIgnoreCase)];
    }

    public async Task SaveAsync(DisciplineDefinition value, string? replacingId = null, CancellationToken cancellationToken = default)
    {
        CatalogValidation.Text("Nome", value.Name, 80);
        var id = value.Id.Length == 0 ? "discipline-custom-" + Guid.NewGuid().ToString("N") : value.Id;
        if (DisciplineCatalog.IsBuiltIn(id))
            throw new InvalidOperationException("As disciplinas do aplicativo nao podem ser alteradas.");
        if (DisciplineCatalog.All.Any(x => x.Id != id && ClanCatalog.SameName(x.Name, value.Name)))
            throw new InvalidOperationException("Ja existe uma disciplina com esse nome.");
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            var record = new DisciplineCatalogRecord { Id = id, Name = value.Name.Trim() };
            var existing = await db.DisciplineCatalog.FindAsync([id], cancellationToken);
            if (existing is null) db.DisciplineCatalog.Add(record);
            else db.Entry(existing).CurrentValues.SetValues(record);
            await db.SaveChangesAsync(cancellationToken);
        }
        finally { database.Gate.Release(); }
        await RefreshAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (DisciplineCatalog.IsBuiltIn(id)) throw CatalogValidation.BuiltInDelete();
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            var clans = await db.ClanCatalog.AsNoTracking().ToListAsync(cancellationToken);
            if (await db.PowerCatalog.AnyAsync(x => x.DisciplineId == id || x.AmalgamDisciplineId == id, cancellationToken) ||
                SqliteClanCatalog.Definitions(clans).Any(x => x.Disciplines.Contains(id)))
                throw new InvalidOperationException("Esta disciplina e usada por poderes ou clas; altere-os antes de exclui-la.");
            await db.DisciplineCatalog.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken);
        }
        finally { database.Gate.Release(); }
        await RefreshAsync(cancellationToken);
    }

    public Task ResetAsync(string id, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("As disciplinas do aplicativo nao podem ser alteradas.");

    public async Task RefreshAsync(CancellationToken cancellationToken = default) =>
        DisciplineCatalog.Configure(await LoadAsync(cancellationToken));

    private async Task<IReadOnlyList<DisciplineDefinition>> LoadAsync(CancellationToken cancellationToken)
    {
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            return [.. (await db.DisciplineCatalog.AsNoTracking().ToListAsync(cancellationToken)).Select(x => new DisciplineDefinition(x.Id, x.Name))];
        }
        finally { database.Gate.Release(); }
    }
}
