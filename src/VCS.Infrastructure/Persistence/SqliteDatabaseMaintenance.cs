using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VCS.Application.Maintenance;
using VCS.Rules.V5.Validation;

namespace VCS.Infrastructure.Persistence;

public sealed class SqliteDatabaseMaintenance(DatabaseAccess database) : IDatabaseMaintenance
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await database.Gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = database.CreateContext();
            // A new app version may carry schema/catalog fixes; keep a copy of the old database first.
            if ((await db.Database.GetAppliedMigrationsAsync(cancellationToken)).Any() &&
                (await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
                await Task.Run(() => BackupTo(database.Path + ".before-migrate-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".bak"), cancellationToken);
            await MigrateKnownSchemaAsync(db, false, cancellationToken);
        }
        finally { database.Gate.Release(); }
    }

    public async Task BackupAsync(string path, CancellationToken cancellationToken = default)
    {
        RejectLivePath(path);
        await database.Gate.WaitAsync(cancellationToken);
        try { await Task.Run(() => BackupTo(path), cancellationToken); }
        finally { database.Gate.Release(); }
    }

    public async Task<string> RestoreAsync(string path, CancellationToken cancellationToken = default)
    {
        RejectLivePath(path);
        await database.Gate.WaitAsync(cancellationToken);
        var staging = database.Path + ".restore-" + Guid.NewGuid().ToString("N") + ".db";
        try
        {
            await Task.Run(() => CopyDatabase(path, staging), cancellationToken);
            await using (var db = database.CreateContext(staging))
            {
                await db.Database.OpenConnectionAsync(cancellationToken);
                await using var check = db.Database.GetDbConnection().CreateCommand();
                check.CommandText = "PRAGMA integrity_check";
                if (!Equals(await check.ExecuteScalarAsync(cancellationToken), "ok"))
                    throw new InvalidDataException("O backup falhou na verificacao de integridade.");
                await MigrateKnownSchemaAsync(db, true, cancellationToken);
                var characters = await db.Characters.AsNoTracking().Include(x => x.Traits).ToListAsync(cancellationToken);
                foreach (var character in characters)
                {
                    var issues = CharacterRules.Validate(character.Details, character.Traits.Select(x => x.ToValue()).ToArray());
                    if (issues.Count != 0)
                        throw new CharacterValidationException(issues);
                }
                check.CommandText = "PRAGMA foreign_key_check";
                if (await check.ExecuteScalarAsync(cancellationToken) is not null)
                    throw new InvalidDataException("O backup possui relacionamentos inconsistentes.");
            }
            var safetyCopy = database.Path + ".before-restore-" + Guid.NewGuid().ToString("N") + ".bak";
            await Task.Run(() =>
            {
                BackupTo(safetyCopy);
                CopyDatabase(staging, database.Path);
            }, cancellationToken);
            return safetyCopy;
        }
        finally
        {
            try
            {
                foreach (var temporary in new[] { staging, staging + "-wal", staging + "-shm" })
                    if (File.Exists(temporary)) File.Delete(temporary);
            }
            finally { database.Gate.Release(); }
        }
    }

    private void RejectLivePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (new[] { database.Path, database.Path + "-wal", database.Path + "-shm" }
            .Contains(fullPath, StringComparer.OrdinalIgnoreCase))
            throw new IOException("Escolha um arquivo diferente do banco em uso.");
    }

    private static async Task MigrateKnownSchemaAsync(VcsDbContext db, bool requireExisting, CancellationToken cancellationToken)
    {
        var applied = (await db.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
        var known = db.Database.GetMigrations().ToHashSet();
        if ((requireExisting && applied.Length == 0) || applied.Any(x => !known.Contains(x)))
            throw new InvalidDataException("Banco sem esquema VCS conhecido ou de uma versao mais recente.");
        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
        await db.Database.MigrateAsync(cancellationToken);
        if (pending.Intersect(LegacyContentIds.Migrations).Any())
            await LegacyContentIds.UpgradeAsync(db, cancellationToken);
        if (pending.Contains(LegacyContentIds.DisciplineReferencesMigration))
            await LegacyContentIds.UpgradeDisciplineReferencesAsync(db, cancellationToken);
    }

    private void BackupTo(string path)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            CopyDatabase(database.Path, temporary);
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static void CopyDatabase(string sourcePath, string targetPath)
    {
        using var source = new SqliteConnection(DatabaseAccess.ConnectionString(sourcePath, SqliteOpenMode.ReadOnly));
        using var target = new SqliteConnection(DatabaseAccess.ConnectionString(targetPath));
        source.Open();
        target.Open();
        source.BackupDatabase(target);
    }
}
