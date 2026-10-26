using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VCS.Application.Catalogs;
using VCS.Content.Clans;
using VCS.Infrastructure.Persistence;

namespace VCS.Infrastructure.Catalogs;

/// <summary>Clan edits live in the database; built-in clans stay in <see cref="ClanCatalog"/> and are overridden by name.</summary>
public sealed class SqliteClanCatalog(DatabaseAccess database) : IEditableCatalog<ClanDefinition>
{
    public async Task<IReadOnlyList<CatalogEntry<ClanDefinition>>> ListEntriesAsync(CancellationToken cancellationToken = default)
    {
        var edits = await LoadAsync(cancellationToken);
        var builtIn = ClanCatalog.BuiltIn.Select(x => edits.FirstOrDefault(edit => ClanCatalog.SameName(edit.Name, x.Name)) is { } edit
            ? new CatalogEntry<ClanDefinition>(x.Name, edit, true, true)
            : new CatalogEntry<ClanDefinition>(x.Name, x, true, false));
        var custom = edits.Where(x => !ClanCatalog.IsBuiltIn(x.Name)).Select(x => new CatalogEntry<ClanDefinition>(x.Name, x, false, false));
        return [.. builtIn.Concat(custom).OrderBy(x => x.Id, StringComparer.CurrentCultureIgnoreCase)];
    }

    public async Task SaveAsync(ClanDefinition value, string? replacingId = null, CancellationToken cancellationToken = default)
    {
        CatalogValidation.Text("Nome", value.Name, 80);
        CatalogValidation.Text("Descricao", value.Description, 2000, required: false);
        CatalogValidation.Text("Perdicao", value.Bane, 2000, required: false);
        if (value.Disciplines.Count > 6 || value.Disciplines.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 80))
            throw new InvalidOperationException("Disciplinas: ate 6 nomes, cada um com 1 a 80 caracteres.");
        if (value.Aliases.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 80))
            throw new InvalidOperationException("Outros nomes: cada um com 1 a 80 caracteres.");
        if (replacingId is not null && !ClanCatalog.SameName(replacingId, value.Name) && ClanCatalog.IsBuiltIn(replacingId))
            throw new InvalidOperationException("O nome de um cla do aplicativo nao pode ser alterado.");
        var record = new ClanCatalogRecord
        {
            Name = value.Name.Trim(), Description = value.Description, Bane = value.Bane,
            Disciplines = JsonSerializer.Serialize(value.Disciplines), Aliases = JsonSerializer.Serialize(value.Aliases)
        };
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            if (replacingId is not null && replacingId != record.Name)
                await db.ClanCatalog.Where(x => x.Name == replacingId).ExecuteDeleteAsync(cancellationToken);
            var existing = await db.ClanCatalog.FindAsync([record.Name], cancellationToken);
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

    private async Task RemoveAsync(string name, CancellationToken cancellationToken)
    {
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            await db.ClanCatalog.Where(x => x.Name == name).ExecuteDeleteAsync(cancellationToken);
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
            var records = await db.ClanCatalog.AsNoTracking().ToListAsync(cancellationToken);
            return [.. records.Select(x => new ClanDefinition(x.Name, x.Description,
                JsonSerializer.Deserialize<string[]>(x.Disciplines) ?? [], JsonSerializer.Deserialize<string[]>(x.Aliases) ?? [], x.Bane))];
        }
        finally { database.Gate.Release(); }
    }
}
