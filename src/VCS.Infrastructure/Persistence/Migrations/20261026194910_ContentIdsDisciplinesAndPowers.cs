using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCS.Infrastructure.Persistence.Migrations
{
    // Built-in power edits keep their rows: only the id changes from the pt-BR name hash to an English id.
    // Rituals the user created move from power-custom- to ritual-custom-. Sheets follow in LegacyContentIds.
    /// <inheritdoc />
    public partial class ContentIdsDisciplinesAndPowers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (from, to) in LegacyContentIds.Powers)
                migrationBuilder.Sql($"UPDATE \"PowerCatalog\" SET \"Id\" = '{to}' WHERE \"Id\" = '{from}';");
            migrationBuilder.Sql("UPDATE \"PowerCatalog\" SET \"Id\" = 'ritual-custom-' || substr(\"Id\", 14) WHERE \"Id\" LIKE 'power-custom-%' AND \"Discipline\" = 'Rituais';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (from, to) in LegacyContentIds.Powers)
                migrationBuilder.Sql($"UPDATE \"PowerCatalog\" SET \"Id\" = '{from}' WHERE \"Id\" = '{to}';");
            migrationBuilder.Sql("UPDATE \"PowerCatalog\" SET \"Id\" = 'power-custom-' || substr(\"Id\", 15) WHERE \"Id\" LIKE 'ritual-custom-%';");
        }
    }
}
