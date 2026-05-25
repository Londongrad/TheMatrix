using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationSystems.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnvironmentalWeatherPressure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WeatherFreezePressure",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WeatherRainPressure",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WeatherSnowPressure",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WeatherStormPressure",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WeatherThawRelief",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeatherFreezePressure",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "WeatherRainPressure",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "WeatherSnowPressure",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "WeatherStormPressure",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "WeatherThawRelief",
                table: "CityEnvironmentalConditions");
        }
    }
}
