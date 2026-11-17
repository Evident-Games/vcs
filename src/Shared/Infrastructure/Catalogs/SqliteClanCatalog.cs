using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VCS.Application.Catalogs;
using VCS.Content.Clans;
using VCS.Content.Disciplines;
using VCS.Infrastructure.Persistence;

namespace VCS.Infrastructure.Catalogs;

/// <summary>Clan edits live in the database; built-in clans stay in <see cref="ClanCatalog"/> and are overridden by id.</summary>
public sealed class SqliteClanCatalog(DatabaseAccess database) : IEditableCatalog<ClanDefinition>
{
    public async Task<IReadOnlyList<CatalogEntry<ClanDefinition>>> ListEntriesAsync(CancellationToken cancellationToken = default)
    {
        var edits = await LoadAsync(cancellationToken);
        var builtIn = ClanCatalog.BuiltIn.Select(x => edits.FirstOrDefault(edit => edit.Id == x.Id) is { } edit
            ? new CatalogEntry<ClanDefinition>(x.Id, edit, true, true)
            : new CatalogEntry<ClanDefinition>(x.Id, x, true, false));
        var custom = edits.Where(x => !ClanCatalog.IsBuiltIn(x.Id)).Select(x => new CatalogEntry<ClanDefinition>(x.Id, x, false, false));
        return [.. builtIn.Concat(custom).OrderBy(x => x.Value.Name, StringComparer.CurrentCultureIgnoreCase)];
    }

    public async Task SaveAsync(ClanDefinition value, string? replacingId = null, CancellationToken cancellationToken = default)
    {
        CatalogValidation.Text("Nome", value.Name, 80);
        CatalogValidation.Text("Descricao", value.Description, 2000, required: false);
        CatalogValidation.Text("Perdicao", value.Bane, 2000, required: false);
        if (value.Disciplines.Count > 6 || value.Disciplines.Distinct().Count() != value.Disciplines.Count ||
            value.Disciplines.Any(id => !DisciplineCatalog.All.Any(x => x.Id == id)))
            throw new InvalidOperationException("Disciplinas: ate 6, escolhidas na lista de disciplinas.");
        if (value.Aliases.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 80))
            throw new InvalidOperationException("Outros nomes: cada um com 1 a 80 caracteres.");
        var id = value.Id.Length == 0 ? "clan-custom-" + Guid.NewGuid().ToString("N") : value.Id;
        if (ClanCatalog.BuiltIn.FirstOrDefault(x => x.Id == id) is { } builtIn && !ClanCatalog.SameName(builtIn.Name, value.Name))
            throw new InvalidOperationException("O nome de um cla do aplicativo nao pode ser alterado.");
        if (ClanCatalog.All.Any(x => x.Id != id && ClanCatalog.SameName(x.Name, value.Name)))
            throw new InvalidOperationException("Ja existe um cla com esse nome.");
        var record = new ClanCatalogRecord
        {
            Id = id, Name = value.Name.Trim(), Description = value.Description, Bane = value.Bane,
            Disciplines = JsonSerializer.Serialize(value.Disciplines), Aliases = JsonSerializer.Serialize(value.Aliases)
        };
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            var existing = await db.ClanCatalog.FindAsync([record.Id], cancellationToken);
            if (existing is null) db.ClanCatalog.Add(record);
            else db.Entry(existing).CurrentValues.SetValues(record);
            await db.SaveChangesAsync(cancellationToken);
        }
        finally { database.Gate.Release(); }
        await RefreshAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (ClanCatalog.IsBuiltIn(id)) throw CatalogValidation.BuiltInDelete();
        await RemoveAsync(id, cancellationToken);
    }

    public Task ResetAsync(string id, CancellationToken cancellationToken = default) =>
        ClanCatalog.IsBuiltIn(id) ? RemoveAsync(id, cancellationToken)
            : throw new InvalidOperationException("Somente itens do aplicativo podem ser restaurados.");

    public async Task RefreshAsync(CancellationToken cancellationToken = default) =>
        ClanCatalog.Configure(await LoadAsync(cancellationToken));

    private async Task RemoveAsync(string id, CancellationToken cancellationToken)
    {
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            await db.ClanCatalog.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken);
        }
        finally { database.Gate.Release(); }
        await RefreshAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ClanDefinition>> LoadAsync(CancellationToken cancellationToken)
    {
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            return Definitions(await db.ClanCatalog.AsNoTracking().ToListAsync(cancellationToken));
        }
        finally { database.Gate.Release(); }
    }

    internal static IReadOnlyList<ClanDefinition> Definitions(IEnumerable<ClanCatalogRecord> records) =>
        [.. records.Select(x => new ClanDefinition(x.Id, x.Name, x.Description,
            JsonSerializer.Deserialize<string[]>(x.Disciplines) ?? [], JsonSerializer.Deserialize<string[]>(x.Aliases) ?? [], x.Bane))];
}
