using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DisciplinePowerDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Power",
                table: "CharacterTraits",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Power",
                table: "CharacterTraits");
        }
    }
}
