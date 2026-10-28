using Microsoft.EntityFrameworkCore;
using VCS.Application.Catalogs;
using VCS.Content.Skills;
using VCS.Infrastructure.Persistence;

namespace VCS.Infrastructure.Catalogs;

/// <summary>Built-in skills come from <see cref="SkillCatalog"/>; the database holds only the user's edits (same id) and new skills.</summary>
public sealed class SqliteSkillCatalog(DatabaseAccess database) : ISkillCatalog, IEditableCatalog<SkillDefinition>
{
    public async Task<IReadOnlyList<SkillDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            var rows = await db.SkillCatalog.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
            var builtIn = SkillCatalog.BuiltIn.Select(x => rows.GetValueOrDefault(x.Id) ?? x).ToArray();
            return [.. builtIn.Concat(rows.Values.Where(x => Seed(x.Id) is null)).OrderBy(x => x.Name)];
        }
        finally { database.Gate.Release(); }
    }

    public async Task<IReadOnlyList<CatalogEntry<SkillDefinition>>> ListEntriesAsync(CancellationToken cancellationToken = default) =>
        [.. (await ListAsync(cancellationToken)).Select(x => Seed(x.Id) is { } seed
            ? new CatalogEntry<SkillDefinition>(x.Id, x, true, seed != x)
            : new CatalogEntry<SkillDefinition>(x.Id, x, false, false))];

    public async Task SaveAsync(SkillDefinition value, string? replacingId = null, CancellationToken cancellationToken = default)
    {
        CatalogValidation.Text("Nome", value.Name, 80);
        CatalogValidation.Text("Grupo", value.Group, 80);
        CatalogValidation.Text("Descricao", value.Description, 20000, required: false);
        var skill = value.Id.Length == 0 ? value with { Id = "skill-custom-" + Guid.NewGuid().ToString("N") } : value;
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            var existing = await db.SkillCatalog.FindAsync([skill.Id], cancellationToken);
            // A built-in skill saved as shipped needs no row, so later app versions can still update it.
            if (Seed(skill.Id) == skill) { if (existing is not null) db.SkillCatalog.Remove(existing); }
            else if (existing is null) db.SkillCatalog.Add(skill);
            else db.Entry(existing).CurrentValues.SetValues(skill);
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
            await db.SkillCatalog.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken);
        }
        finally { database.Gate.Release(); }
    }

    public Task ResetAsync(string id, CancellationToken cancellationToken = default) =>
        SaveAsync(Seed(id) ?? throw new InvalidOperationException("Somente itens do aplicativo podem ser restaurados."),
            null, cancellationToken);

    private static SkillDefinition? Seed(string id) => SkillCatalog.BuiltIn.FirstOrDefault(x => x.Id == id);
}
