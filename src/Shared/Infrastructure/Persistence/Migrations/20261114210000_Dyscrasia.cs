using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Game.Infrastructure.Persistence;

#nullable disable

namespace Game.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CharacterDbContext))]
[Migration("20261114210000_Dyscrasia")]
public sealed class Dyscrasia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Dyscrasia",
            table: "Characters",
            type: "TEXT",
            nullable: false,
            defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Dyscrasia", table: "Characters");
    }
}
