using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCS.Infrastructure.Persistence.Migrations
{
    // Clan edits are keyed by an English id instead of the name: built-in clans get clan-brujah and so on, user
    // clans clan-custom-<random>. The table is copied by hand because the id must be filled before it is the key.
    // Sheets get their ClanKey in LegacyContentIds.
    /// <inheritdoc />
    public partial class ContentIdsClans : Migration
    {
        private static readonly (string Name, string Id)[] BuiltIn =
        [
            ("Banu Haqim", "clan-banu-haqim"), ("Brujah", "clan-brujah"), ("Gangrel", "clan-gangrel"),
            ("Malkavianos", "clan-malkavian"), ("O Ministério", "clan-ministry"), ("Nosferatu", "clan-nosferatu"),
            ("Ravnos", "clan-ravnos"), ("Salubri", "clan-salubri"), ("Toreador", "clan-toreador"), ("Tremere", "clan-tremere"),
            ("Tzimisce", "clan-tzimisce"), ("Ventrue", "clan-ventrue"), ("Hecata", "clan-hecata"), ("Lasombra", "clan-lasombra")
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(name: "ClanCatalog", newName: "ClanCatalogByName");
            migrationBuilder.CreateTable(
                name: "ClanCatalog",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Disciplines = table.Column<string>(type: "TEXT", nullable: false),
                    Aliases = table.Column<string>(type: "TEXT", nullable: false),
                    Bane = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanCatalog", x => x.Id);
                });
            var ids = string.Concat(BuiltIn.Select(x => $" WHEN '{x.Name}' THEN '{x.Id}'"));
            migrationBuilder.Sql(
                "INSERT INTO \"ClanCatalog\" (\"Id\", \"Name\", \"Description\", \"Disciplines\", \"Aliases\", \"Bane\") " +
                $"SELECT CASE \"Name\"{ids} ELSE 'clan-custom-' || lower(hex(randomblob(16))) END, " +
                "\"Name\", \"Description\", \"Disciplines\", \"Aliases\", \"Bane\" FROM \"ClanCatalogByName\";");
            migrationBuilder.DropTable(name: "ClanCatalogByName");
            migrationBuilder.CreateIndex(
                name: "IX_ClanCatalog_Name",
                table: "ClanCatalog",
                column: "Name",
                unique: true);

            migrationBuilder.AddColumn<string>(
                name: "ClanKey",
                table: "Characters",
                type: "TEXT",
                maxLength: 80,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClanKey",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_ClanCatalog_Name",
                table: "ClanCatalog");
            migrationBuilder.RenameTable(name: "ClanCatalog", newName: "ClanCatalogById");
            migrationBuilder.CreateTable(
                name: "ClanCatalog",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Disciplines = table.Column<string>(type: "TEXT", nullable: false),
                    Aliases = table.Column<string>(type: "TEXT", nullable: false),
                    Bane = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanCatalog", x => x.Name);
                });
            migrationBuilder.Sql(
                "INSERT INTO \"ClanCatalog\" (\"Name\", \"Description\", \"Disciplines\", \"Aliases\", \"Bane\") " +
                "SELECT \"Name\", \"Description\", \"Disciplines\", \"Aliases\", \"Bane\" FROM \"ClanCatalogById\";");
            migrationBuilder.DropTable(name: "ClanCatalogById");
        }
    }
}
