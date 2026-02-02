using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCityWeatherExposure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CityPopulationWeatherExposureStates",
                columns: table => new
                {
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentType = table.Column<int>(type: "integer", nullable: false),
                    CurrentSeverity = table.Column<int>(type: "integer", nullable: false),
                    CurrentPrecipitationKind = table.Column<int>(type: "integer", nullable: false),
                    CurrentTemperatureC = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CurrentHumidityPercent = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CurrentWindSpeedKph = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CurrentCloudCoveragePercent = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CurrentPressureHpa = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CurrentWeatherEffectiveAtSimTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PreviousType = table.Column<int>(type: "integer", nullable: true),
                    PreviousSeverity = table.Column<int>(type: "integer", nullable: true),
                    PreviousPrecipitationKind = table.Column<int>(type: "integer", nullable: true),
                    PreviousTemperatureC = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    PreviousHumidityPercent = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    PreviousWindSpeedKph = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    PreviousCloudCoveragePercent = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    PreviousPressureHpa = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    PreviousWeatherEffectiveAtSimTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastWeatherOccurredOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastExposureProcessedAtSimTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityPopulationWeatherExposureStates", x => x.CityId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CityPopulationWeatherExposureStates_LastExposureProcessedAt~",
                table: "CityPopulationWeatherExposureStates",
                column: "LastExposureProcessedAtSimTimeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CityPopulationWeatherExposureStates_UpdatedAtUtc",
                table: "CityPopulationWeatherExposureStates",
                column: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityPopulationWeatherExposureStates");
        }
    }
}
