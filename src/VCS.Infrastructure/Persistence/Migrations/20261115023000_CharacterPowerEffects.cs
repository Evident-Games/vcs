using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(VcsDbContext))]
[Migration("20261115023000_CharacterPowerEffects")]
public sealed class CharacterPowerEffects : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(
        name: "Details_PowerEffects", table: "Characters", type: "TEXT", nullable: false, defaultValue: "[]");

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
        name: "Details_PowerEffects", table: "Characters");
}
