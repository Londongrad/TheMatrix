using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.CityCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityInitialWeatherProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InitialWeatherManualSeverity",
                table: "Cities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InitialWeatherManualTemperatureC",
                table: "Cities",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InitialWeatherManualType",
                table: "Cities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InitialWeatherMode",
                table: "Cities",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitialWeatherManualSeverity",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "InitialWeatherManualTemperatureC",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "InitialWeatherManualType",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "InitialWeatherMode",
                table: "Cities");
        }
    }
}
