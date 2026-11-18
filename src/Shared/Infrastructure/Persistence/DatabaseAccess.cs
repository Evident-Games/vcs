using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Game.Infrastructure.Persistence;

public sealed class DatabaseAccess
{
    public static string DefaultPath => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VCS", "Provisorio", "vcs.db");

    public DatabaseAccess(string path)
    {
        Path = System.IO.Path.GetFullPath(path);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
    }

    public string Path { get; }
    public SemaphoreSlim Gate { get; } = new(1, 1);

    public CharacterDbContext CreateContext(string? path = null) => new(
        new DbContextOptionsBuilder<CharacterDbContext>().UseSqlite(ConnectionString(path ?? Path)).Options);

    internal static string ConnectionString(string path, SqliteOpenMode mode = SqliteOpenMode.ReadWriteCreate) =>
        new SqliteConnectionStringBuilder
        {
            DataSource = path, Mode = mode, Pooling = false, ForeignKeys = true, DefaultTimeout = 10
        }.ToString();
}

public sealed class CharacterDbContextFactory : IDesignTimeDbContextFactory<CharacterDbContext>
{
    public CharacterDbContext CreateDbContext(string[] args) =>
        new DatabaseAccess(Environment.GetEnvironmentVariable("VCS_DB_PATH") ?? DatabaseAccess.DefaultPath).CreateContext();
}
