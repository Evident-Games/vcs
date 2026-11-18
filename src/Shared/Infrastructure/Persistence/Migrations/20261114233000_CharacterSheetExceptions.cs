using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Game.Infrastructure.Persistence;

#nullable disable

namespace Game.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CharacterDbContext))]
[Migration("20261114233000_CharacterSheetExceptions")]
public sealed class CharacterSheetExceptions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(
        name: "Details_Exceptions", table: "Characters", type: "TEXT", nullable: false, defaultValue: "[]");

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
        name: "Details_Exceptions", table: "Characters");
}
