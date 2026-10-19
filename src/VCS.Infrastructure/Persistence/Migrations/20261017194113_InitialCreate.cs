using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Details_MortalName = table.Column<string>(type: "TEXT", nullable: false),
                    Clan = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Details_Generation = table.Column<int>(type: "INTEGER", nullable: false),
                    Details_Sire = table.Column<string>(type: "TEXT", nullable: false),
                    Details_Predator = table.Column<string>(type: "TEXT", nullable: false),
                    Details_Concept = table.Column<string>(type: "TEXT", nullable: false),
                    Chronicle = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Details_Description = table.Column<string>(type: "TEXT", nullable: false),
                    Details_Notes = table.Column<string>(type: "TEXT", nullable: false),
                    Details_Hunger = table.Column<int>(type: "INTEGER", nullable: false),
                    Details_Humanity = table.Column<int>(type: "INTEGER", nullable: false),
                    Details_BloodPotency = table.Column<int>(type: "INTEGER", nullable: false),
                    Details_HealthSuperficial = table.Column<int>(type: "INTEGER", nullable: false),
                    Details_HealthAggravated = table.Column<int>(type: "INTEGER", nullable: false),
                    Details_WillpowerSuperficial = table.Column<int>(type: "INTEGER", nullable: false),
                    Details_WillpowerAggravated = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Revision = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterTraits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterTraits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterTraits_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_Name",
                table: "Characters",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterTraits_CharacterId_Category_Key",
                table: "CharacterTraits",
                columns: new[] { "CharacterId", "Category", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterTraits");

            migrationBuilder.DropTable(
                name: "Characters");
        }
    }
}
