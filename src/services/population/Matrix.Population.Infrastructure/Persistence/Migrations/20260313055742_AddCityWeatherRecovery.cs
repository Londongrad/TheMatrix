using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCityWeatherRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RecoverySourceCloudCoveragePercent",
                table: "CityPopulationWeatherExposureStates",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RecoverySourceHumidityPercent",
                table: "CityPopulationWeatherExposureStates",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecoverySourcePrecipitationKind",
                table: "CityPopulationWeatherExposureStates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RecoverySourcePressureHpa",
                table: "CityPopulationWeatherExposureStates",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecoverySourceSeverity",
                table: "CityPopulationWeatherExposureStates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RecoverySourceTemperatureC",
                table: "CityPopulationWeatherExposureStates",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecoverySourceType",
                table: "CityPopulationWeatherExposureStates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RecoverySourceWindSpeedKph",
                table: "CityPopulationWeatherExposureStates",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RecoveryStartedAtSimTimeUtc",
                table: "CityPopulationWeatherExposureStates",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecoverySourceCloudCoveragePercent",
                table: "CityPopulationWeatherExposureStates");

            migrationBuilder.DropColumn(
                name: "RecoverySourceHumidityPercent",
                table: "CityPopulationWeatherExposureStates");

            migrationBuilder.DropColumn(
                name: "RecoverySourcePrecipitationKind",
                table: "CityPopulationWeatherExposureStates");

            migrationBuilder.DropColumn(
                name: "RecoverySourcePressureHpa",
                table: "CityPopulationWeatherExposureStates");

            migrationBuilder.DropColumn(
                name: "RecoverySourceSeverity",
                table: "CityPopulationWeatherExposureStates");

            migrationBuilder.DropColumn(
                name: "RecoverySourceTemperatureC",
                table: "CityPopulationWeatherExposureStates");

            migrationBuilder.DropColumn(
                name: "RecoverySourceType",
                table: "CityPopulationWeatherExposureStates");

            migrationBuilder.DropColumn(
                name: "RecoverySourceWindSpeedKph",
                table: "CityPopulationWeatherExposureStates");

            migrationBuilder.DropColumn(
                name: "RecoveryStartedAtSimTimeUtc",
                table: "CityPopulationWeatherExposureStates");
        }
    }
}
