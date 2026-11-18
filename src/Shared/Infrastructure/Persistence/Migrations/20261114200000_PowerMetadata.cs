using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VCS.Infrastructure.Persistence;

#nullable disable

namespace VCS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CharacterDbContext))]
[Migration("20261114200000_PowerMetadata")]
public sealed class PowerMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Prerequisite",
            table: "PowerCatalog",
            type: "TEXT",
            maxLength: 500,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Source",
            table: "PowerCatalog",
            type: "TEXT",
            maxLength: 120,
            nullable: false,
            defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Prerequisite", table: "PowerCatalog");
        migrationBuilder.DropColumn(name: "Source", table: "PowerCatalog");
    }
}
