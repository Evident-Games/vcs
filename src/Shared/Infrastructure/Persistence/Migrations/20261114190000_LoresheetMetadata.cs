using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VCS.Infrastructure.Persistence;

#nullable disable

namespace VCS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(VcsDbContext))]
[Migration("20261114190000_LoresheetMetadata")]
public sealed class LoresheetMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Prerequisite",
            table: "AdvantageCatalog",
            type: "TEXT",
            maxLength: 500,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Source",
            table: "AdvantageCatalog",
            type: "TEXT",
            maxLength: 120,
            nullable: false,
            defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Prerequisite", table: "AdvantageCatalog");
        migrationBuilder.DropColumn(name: "Source", table: "AdvantageCatalog");
    }
}
