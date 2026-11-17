using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteCharacterSheet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Details_Ambition",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Details_ApparentAge",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Details_Appearance",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Details_BaneSeverity",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Details_BirthDate",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Details_BloodSurge",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Details_ChronicleTenets",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Details_ClanBane",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Details_Convictions",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Details_DamageMend",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Details_DeathDate",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Details_Desire",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Details_DistinguishingFeatures",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Details_ExperienceSpent",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Details_ExperienceTotal",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Details_FeedingPenalty",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Details_History",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Details_PowerBonus",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Details_Resonance",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Details_RouseReroll",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Details_Touchstones",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Details_TrueAge",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Details_Ambition",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_ApparentAge",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_Appearance",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_BaneSeverity",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_BirthDate",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_BloodSurge",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_ChronicleTenets",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_ClanBane",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_Convictions",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_DamageMend",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_DeathDate",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_Desire",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_DistinguishingFeatures",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_ExperienceSpent",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_ExperienceTotal",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_FeedingPenalty",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_History",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_PowerBonus",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_Resonance",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_RouseReroll",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_Touchstones",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Details_TrueAge",
                table: "Characters");
        }
    }
}
