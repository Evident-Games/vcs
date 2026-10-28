using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCS.Infrastructure.Persistence.Migrations
{
    // Disciplines become catalog entries that powers and clans point to by id; the names already saved are turned
    // into ids by LegacyContentIds.UpgradeDisciplineReferencesAsync.
    /// <inheritdoc />
    public partial class DisciplineCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Discipline",
                table: "PowerCatalog",
                newName: "DisciplineId");

            migrationBuilder.RenameColumn(
                name: "AmalgamDiscipline",
                table: "PowerCatalog",
                newName: "AmalgamDisciplineId");

            migrationBuilder.AddColumn<bool>(
                name: "IsRitual",
                table: "PowerCatalog",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DisciplineCatalog",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisciplineCatalog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DisciplineCatalog_Name",
                table: "DisciplineCatalog",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"PowerCatalog\" SET \"DisciplineId\" = (SELECT \"Name\" FROM \"DisciplineCatalog\" d WHERE d.\"Id\" = \"PowerCatalog\".\"DisciplineId\") WHERE \"DisciplineId\" IN (SELECT \"Id\" FROM \"DisciplineCatalog\");");
            migrationBuilder.Sql("UPDATE \"PowerCatalog\" SET \"DisciplineId\" = 'Rituais' WHERE \"IsRitual\" = 1;");

            migrationBuilder.DropTable(
                name: "DisciplineCatalog");

            migrationBuilder.DropColumn(
                name: "IsRitual",
                table: "PowerCatalog");

            migrationBuilder.RenameColumn(
                name: "DisciplineId",
                table: "PowerCatalog",
                newName: "Discipline");

            migrationBuilder.RenameColumn(
                name: "AmalgamDisciplineId",
                table: "PowerCatalog",
                newName: "AmalgamDiscipline");
        }
    }
}
