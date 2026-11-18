using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Game.Infrastructure.Persistence;

#nullable disable

namespace Game.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CharacterDbContext))]
[Migration("20261114220000_RenameDyscrasiaColumn")]
public sealed class RenameDyscrasiaColumn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "Dyscrasia",
            table: "Characters",
            newName: "Details_Dyscrasia");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "Details_Dyscrasia",
            table: "Characters",
            newName: "Dyscrasia");
    }
}
