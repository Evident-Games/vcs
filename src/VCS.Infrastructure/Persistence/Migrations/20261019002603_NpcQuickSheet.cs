using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NpcQuickSheet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Details_NpcAttacks",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Details_NpcAttacks",
                table: "Characters");
        }
    }
}
