using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCS.Infrastructure.Persistence.Migrations
{
    // No schema change: marks where saved attribute keys (Strength) become attribute-strength; see LegacyContentIds.
    /// <inheritdoc />
    public partial class ContentIdsAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
