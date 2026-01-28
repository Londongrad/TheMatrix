using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.CityCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityWeather : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CityWeathers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClimateZone = table.Column<int>(type: "integer", nullable: false),
                    TemperatureSpringAverage = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    TemperatureSummerAverage = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    TemperatureAutumnAverage = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    TemperatureWinterAverage = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    TemperatureDailySwing = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    PrecipitationSpringHumidity = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PrecipitationSummerHumidity = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PrecipitationAutumnHumidity = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PrecipitationWinterHumidity = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PrecipitationSpringDominantKind = table.Column<int>(type: "integer", nullable: false),
                    PrecipitationSummerDominantKind = table.Column<int>(type: "integer", nullable: false),
                    PrecipitationAutumnDominantKind = table.Column<int>(type: "integer", nullable: false),
                    PrecipitationWinterDominantKind = table.Column<int>(type: "integer", nullable: false),
                    WindSpringAverage = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    WindSummerAverage = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    WindAutumnAverage = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    WindWinterAverage = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    WindGustHeadroom = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    Volatility = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false),
                    ExtremeMaxOverallSeverity = table.Column<int>(type: "integer", nullable: false),
                    ExtremeSupportsThunderstorms = table.Column<bool>(type: "boolean", nullable: false),
                    ExtremeSupportsSnowstorms = table.Column<bool>(type: "boolean", nullable: false),
                    ExtremeSupportsFog = table.Column<bool>(type: "boolean", nullable: false),
                    ExtremeSupportsHeatwaves = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentType = table.Column<int>(type: "integer", nullable: false),
                    CurrentSeverity = table.Column<int>(type: "integer", nullable: false),
                    CurrentPrecipitationKind = table.Column<int>(type: "integer", nullable: false),
                    CurrentTemperatureC = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    CurrentHumidityPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CurrentWindSpeedKph = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    CurrentCloudCoveragePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CurrentPressureHpa = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    CurrentStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CurrentExpectedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastEvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastTransitionAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityWeathers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CityWeathers_Cities_Id",
                        column: x => x.Id,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CityWeatherOverrides",
                columns: table => new
                {
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ForcedType = table.Column<int>(type: "integer", nullable: false),
                    ForcedSeverity = table.Column<int>(type: "integer", nullable: false),
                    ForcedPrecipitationKind = table.Column<int>(type: "integer", nullable: false),
                    ForcedTemperatureC = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    ForcedHumidityPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ForcedWindSpeedKph = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    ForcedCloudCoveragePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ForcedPressureHpa = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    ForcedStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ForcedExpectedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OverrideId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityWeatherOverrides", x => x.CityId);
                    table.ForeignKey(
                        name: "FK_CityWeatherOverrides_CityWeathers_CityId",
                        column: x => x.CityId,
                        principalTable: "CityWeathers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityWeatherOverrides");

            migrationBuilder.DropTable(
                name: "CityWeathers");
        }
    }
}
