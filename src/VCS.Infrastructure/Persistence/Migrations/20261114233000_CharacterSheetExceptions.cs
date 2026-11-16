using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VCS.Infrastructure.Persistence;

#nullable disable

namespace VCS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(VcsDbContext))]
[Migration("20261114233000_CharacterSheetExceptions")]
public sealed class CharacterSheetExceptions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(
        name: "Details_Exceptions", table: "Characters", type: "TEXT", nullable: false, defaultValue: "[]");

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
        name: "Details_Exceptions", table: "Characters");
}
