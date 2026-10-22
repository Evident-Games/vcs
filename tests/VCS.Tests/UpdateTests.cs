using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VCS.Desktop.Services;
using VCS.Infrastructure.Files;
using VCS.Infrastructure.Persistence;

namespace VCS.Tests;

public sealed class UpdateTests
{
    private const string Release = """
        [{"tag_name":"v0.15.0","html_url":"https://github.com/Evident-Games/vcs/releases/tag/v0.15.0",
         "assets":[{"name":"notes.txt","browser_download_url":"https://x/notes.txt"},
                   {"name":"VCS.exe","browser_download_url":"https://x/VCS.exe","digest":"sha256:ABC123"}]}]
        """;

    [Fact]
    public void Newer_release_with_exe_asset_is_offered()
    {
        var update = UpdateService.Parse(Release, new Version(0, 14, 0, 0));
        Assert.NotNull(update);
        Assert.Equal(new Version(0, 15, 0), update.Version);
        Assert.Equal("https://x/VCS.exe", update.DownloadUrl);
        Assert.Equal("ABC123", update.Sha256);
    }

    [Theory]
    [InlineData("0.15.0")]
    [InlineData("0.16.0.0")]
    public void Same_or_older_release_is_ignored(string current) =>
        Assert.Null(UpdateService.Parse(Release, Version.Parse(current)));

    [Fact]
    public void Invalid_tag_or_missing_asset_is_ignored()
    {
        Assert.Null(UpdateService.Parse("""[{"tag_name":"beta","html_url":"u","assets":[]}]""", new Version(0, 1)));
        Assert.Null(UpdateService.Parse("""[{"tag_name":"9.0.0","html_url":"u","assets":[]}]""", new Version(0, 1)));
        Assert.Null(UpdateService.Parse("[]", new Version(0, 1)));
    }

    [Fact]
    public async Task Pending_migration_backs_up_database_first()
    {
        var directory = Path.Combine(Path.GetTempPath(), "VCS-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var database = new DatabaseAccess(Path.Combine(directory, "test.db"));
            await using (var db = database.CreateContext())
            {
                var previous = db.Database.GetMigrations().SkipLast(1).Last();
                await db.GetService<IMigrator>().MigrateAsync(previous);
            }
            await new SqliteDatabaseMaintenance(database).InitializeAsync();
            Assert.Single(Directory.GetFiles(directory, "test.db.before-migrate-*.bak"));

            await new SqliteDatabaseMaintenance(database).InitializeAsync();
            Assert.Single(Directory.GetFiles(directory, "test.db.before-migrate-*.bak"));
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(directory, true);
        }
    }
}
