using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingWeatherHealthImpacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CityPopulationPendingWeatherImpacts",
                columns: table => new
                {
                    ImpactId = table.Column<Guid>(type: "uuid", nullable: false),
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PreviousType = table.Column<int>(type: "integer", nullable: false),
                    PreviousSeverity = table.Column<int>(type: "integer", nullable: false),
                    PreviousPrecipitationKind = table.Column<int>(type: "integer", nullable: false),
                    PreviousTemperatureC = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    PreviousHumidityPercent = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    PreviousWindSpeedKph = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    PreviousCloudCoveragePercent = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    PreviousPressureHpa = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    CurrentType = table.Column<int>(type: "integer", nullable: false),
                    CurrentSeverity = table.Column<int>(type: "integer", nullable: false),
                    CurrentPrecipitationKind = table.Column<int>(type: "integer", nullable: false),
                    CurrentTemperatureC = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    CurrentHumidityPercent = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    CurrentWindSpeedKph = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    CurrentCloudCoveragePercent = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    CurrentPressureHpa = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    EnvironmentClimateZone = table.Column<int>(type: "integer", nullable: true),
                    EnvironmentHemisphere = table.Column<int>(type: "integer", nullable: true),
                    EnvironmentUtcOffsetMinutes = table.Column<int>(type: "integer", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityPopulationPendingWeatherImpacts", x => x.ImpactId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CityPopulationPendingWeatherImpacts_CityId_OccurredAtUtc",
                table: "CityPopulationPendingWeatherImpacts",
                columns: new[] { "CityId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityPopulationPendingWeatherImpacts");
        }
    }
}
