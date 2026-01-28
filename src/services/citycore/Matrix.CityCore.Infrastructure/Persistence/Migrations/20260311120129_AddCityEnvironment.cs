using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.CityCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityEnvironment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClimateZone",
                table: "Cities",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "Hemisphere",
                table: "Cities",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "UtcOffsetMinutes",
                table: "Cities",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClimateZone",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "Hemisphere",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "UtcOffsetMinutes",
                table: "Cities");
        }
    }
}